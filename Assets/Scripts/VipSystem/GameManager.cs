using System;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public event Action MoneyChanged;
    public event Action VipsChanged;
    public event Action<VipPassenger, int> VipRewarded;
    public event Action<VipPassenger, int> VipPenalized;
    public event Action<Station> StationUnlocked;
    public event Action<string> NotEnoughMoney;
    public event Action<int> MinorUpgradePurchased;
    public event Action<int> TrainTierChanged;

    [Header("Economy")]
    [SerializeField] private float startingMoney = 100f;
    [SerializeField] private int regularPassengers = 25;
    [SerializeField] private float revenuePerPassengerPerSecond = 0.5f;
    [SerializeField] private int passengersGainedPerStation = 10;

    [Header("Upgrade")]
    [SerializeField] private float upgradeCost = 8000f;
    [SerializeField] private int upgradedPassengerCount = 80;
    [SerializeField] private float upgradedRevenueMultiplier = 2.5f;

    [Header("Train Tiers")]
    [Tooltip("Ordered list of train tiers. Index 0 = base train, must be cost=0. Each subsequent tier costs money to unlock.")]
    [SerializeField] private List<TrainTierDefinition> trainTiers = new List<TrainTierDefinition>();
    [SerializeField] private int currentTrainTier = 0;
    [Tooltip("Fallback travel duration if no train tiers are configured.")]
    [SerializeField] private float defaultTravelDuration = 25f;

    [Header("Minor Upgrades")]
    [Tooltip("List of minor upgrades that the player must purchase to unlock the major upgrade (hyperloop). Configure in Inspector.")]
    [SerializeField] private List<MinorUpgradeDefinition> minorUpgrades = new List<MinorUpgradeDefinition>();
    [Tooltip("If true, the major (hyperloop) upgrade is locked until all minor upgrades are purchased.")]
    [SerializeField] private bool gateMajorUpgradeBehindMinorUpgrades = true;
    private List<bool> minorUpgradesPurchased = new List<bool>();

    [Header("Train State")]
    [SerializeField] private string currentStationId = "";

    [Header("VIP Capacity")]
    [SerializeField] private int maxOnboardVips = 3;

    private int trainTierPassengerBonus = 0;

    public float Money { get; private set; }
    public int RegularPassengers => regularPassengers;
    public int EffectivePassengers => regularPassengers + trainTierPassengerBonus;
    public float PassiveIncomePerSecond
    {
        get
        {
            float baseIncome = EffectivePassengers * revenuePerPassengerPerSecond;
            if (HasUpgraded)
            {
                baseIncome *= upgradedRevenueMultiplier;
            }
            baseIncome *= CurrentTrainTierIncomeMultiplier;
            baseIncome += MinorUpgradeIncomeBonus;
            return baseIncome;
        }
    }

    public int CurrentTrainTier => currentTrainTier;
    public int TrainTierCount => trainTiers != null ? trainTiers.Count : 0;

    public TrainTierDefinition GetTrainTier(int index)
    {
        if (trainTiers == null || index < 0 || index >= trainTiers.Count)
        {
            return null;
        }
        return trainTiers[index];
    }

    public TrainTierDefinition CurrentTrainTierDef => GetTrainTier(currentTrainTier);

    public float CurrentTrainTravelDuration
    {
        get
        {
            TrainTierDefinition def = CurrentTrainTierDef;
            return def != null ? Mathf.Max(0.85f, def.travelDuration) : Mathf.Max(0.85f, defaultTravelDuration);
        }
    }

    public float CurrentTrainTierIncomeMultiplier
    {
        get
        {
            TrainTierDefinition def = CurrentTrainTierDef;
            return def != null ? Mathf.Max(0f, def.incomeMultiplier) : 1f;
        }
    }

    // How much faster the world should scroll relative to the base tier.
    // Arriva (25s travel) → 1.0, Bullet (12s) → ~2.08, Hyperloop (5s) → 5.0.
    public float CurrentTierScrollMultiplier
    {
        get
        {
            float current = CurrentTrainTravelDuration;
            return Mathf.Max(0.1f, defaultTravelDuration / Mathf.Max(0.85f, current));
        }
    }

    public float CurrentTrainTopSpeedKmh
    {
        get
        {
            TrainTierDefinition def = CurrentTrainTierDef;
            return def != null ? Mathf.Max(1f, def.topSpeedKmh) : 80f;
        }
    }

    public bool IsTrainTierOwned(int index) => index <= currentTrainTier;
    public bool IsNextTrainTier(int index) => index == currentTrainTier + 1;

    public bool CanBuyTrainTier(int index)
    {
        TrainTierDefinition def = GetTrainTier(index);
        if (def == null) return false;
        if (IsTrainTierOwned(index)) return false;
        if (!IsNextTrainTier(index)) return false; // Must be purchased in order
        return Money >= def.cost;
    }

    public bool TryBuyTrainTier(int index)
    {
        TrainTierDefinition def = GetTrainTier(index);
        if (def == null)
        {
            return false;
        }

        if (IsTrainTierOwned(index))
        {
            return false;
        }

        if (!IsNextTrainTier(index))
        {
            NotEnoughMoney?.Invoke("Buy the previous train tier first");
            return false;
        }

        if (Money < def.cost)
        {
            NotEnoughMoney?.Invoke("Need EUR " + def.cost + " for " + def.displayName);
            return false;
        }

        Money -= def.cost;
        currentTrainTier = index;
        SetTrainTierEconomy(def.passengerBonus);
        Debug.Log("[GameManager] Train tier purchased: " + def.displayName + " | tier=" + index + " | duration=" + def.travelDuration + "s | +" + def.passengerBonus + " passengers | x" + def.incomeMultiplier + " income");
        TrainTierChanged?.Invoke(currentTrainTier);
        MoneyChanged?.Invoke();
        return true;
    }
    public float UpgradeCost => upgradeCost;
    public bool HasUpgraded { get; private set; }
    public bool CanBuyUpgrade => !HasUpgraded && Money >= upgradeCost && (!gateMajorUpgradeBehindMinorUpgrades || AllMinorUpgradesPurchased);

    public int MinorUpgradeCount => minorUpgrades != null ? minorUpgrades.Count : 0;

    public bool AllMinorUpgradesPurchased
    {
        get
        {
            if (minorUpgrades == null || minorUpgrades.Count == 0)
            {
                return true;
            }

            for (int i = 0; i < minorUpgradesPurchased.Count; i++)
            {
                if (!minorUpgradesPurchased[i])
                {
                    return false;
                }
            }

            return minorUpgradesPurchased.Count == minorUpgrades.Count;
        }
    }

    public float MinorUpgradeIncomeBonus
    {
        get
        {
            if (minorUpgrades == null || minorUpgradesPurchased == null)
            {
                return 0f;
            }

            float total = 0f;
            int count = Mathf.Min(minorUpgrades.Count, minorUpgradesPurchased.Count);
            for (int i = 0; i < count; i++)
            {
                if (minorUpgradesPurchased[i] && minorUpgrades[i] != null)
                {
                    total += minorUpgrades[i].passiveIncomeBonusPerSecond;
                }
            }
            return total;
        }
    }
    public string CurrentStationId => currentStationId;
    public int MaxOnboardVips => maxOnboardVips;

    // Legacy single-VIP accessor — returns first onboard VIP if any
    public VipPassenger CurrentOnboardVip => onboardVips.Count > 0 ? onboardVips[0] : null;
    public IReadOnlyList<VipPassenger> OnboardVips => onboardVips;
    public IReadOnlyList<VipPassenger> WaitingVips => waitingVips;
    public bool VipTimersPaused { get; private set; }

    private readonly List<VipPassenger> waitingVips = new List<VipPassenger>();
    private readonly List<VipPassenger> onboardVips = new List<VipPassenger>();
    private float passiveIncomeBank;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Money = startingMoney;

        InitializeMinorUpgradesState();
        InitializeTrainTierState();
    }

    private void InitializeTrainTierState()
    {
        if (trainTiers == null || trainTiers.Count == 0)
        {
            return;
        }

        currentTrainTier = Mathf.Clamp(currentTrainTier, 0, trainTiers.Count - 1);
        TrainTierDefinition def = CurrentTrainTierDef;
        if (def != null)
        {
            SetTrainTierEconomy(def.passengerBonus);
        }
    }

    private void InitializeMinorUpgradesState()
    {
        int count = minorUpgrades != null ? minorUpgrades.Count : 0;
        minorUpgradesPurchased = new List<bool>(count);
        for (int i = 0; i < count; i++)
        {
            minorUpgradesPurchased.Add(false);
        }
    }

    private void Update()
    {
        AddPassiveIncome(Time.deltaTime);
        if (!VipTimersPaused)
        {
            TickVipTimers(Time.deltaTime);
        }
    }

    public void SetVipTimersPaused(bool paused)
    {
        if (HasUpgraded)
        {
            VipTimersPaused = false;
            return;
        }

        VipTimersPaused = paused;
    }

    public void SetCurrentStation(Station station)
    {
        if (station == null)
        {
            return;
        }

        currentStationId = station.StationId;
        VipsChanged?.Invoke();
    }

    public void AddWaitingVip(VipPassenger vip)
    {
        if (vip == null || waitingVips.Contains(vip))
        {
            return;
        }

        waitingVips.Add(vip);
        VipsChanged?.Invoke();
    }

    public bool HasWaitingVipAtStation(string stationId)
    {
        return GetWaitingVipAtStation(stationId) != null;
    }

    public VipPassenger GetWaitingVipAtStation(string stationId)
    {
        for (int i = 0; i < waitingVips.Count; i++)
        {
            if (waitingVips[i].OriginStationId == stationId && waitingVips[i].IsWaiting)
            {
                return waitingVips[i];
            }
        }

        return null;
    }

    public bool TryPickupVipAtStation(string stationId, out VipPassenger pickedUpVip)
    {
        pickedUpVip = null;

        if (onboardVips.Count >= maxOnboardVips)
        {
            return false;
        }

        VipPassenger vip = GetWaitingVipAtStation(stationId);
        if (vip == null)
        {
            return false;
        }

        waitingVips.Remove(vip);
        vip.MarkPickedUp();
        onboardVips.Add(vip);
        pickedUpVip = vip;
        VipsChanged?.Invoke();
        return true;
    }

    public bool TryDeliverOnboardVipAtStation(string stationId, out VipPassenger deliveredVip)
    {
        deliveredVip = null;

        for (int i = 0; i < onboardVips.Count; i++)
        {
            if (onboardVips[i].DestinationStationId == stationId)
            {
                deliveredVip = onboardVips[i];
                onboardVips.RemoveAt(i);
                deliveredVip.MarkDelivered();
                AddMoney(deliveredVip.DeliveryReward);
                VipRewarded?.Invoke(deliveredVip, deliveredVip.DeliveryReward);
                VipsChanged?.Invoke();
                return true;
            }
        }

        return false;
    }

    public void IncreaseRegularPassengers(int amount)
    {
        regularPassengers = Mathf.Max(0, regularPassengers + amount);
        MoneyChanged?.Invoke();
    }

    public void SetPassiveRevenue(float newRevenuePerPassenger)
    {
        revenuePerPassengerPerSecond = Mathf.Max(0f, newRevenuePerPassenger);
        MoneyChanged?.Invoke();
    }

    public void SetTrainTierEconomy(int passengerBonus)
    {
        trainTierPassengerBonus = Mathf.Max(0, passengerBonus);
        MoneyChanged?.Invoke();
    }

    public bool TryUnlockStation(Station station)
    {
        if (station == null || station.IsUnlocked)
        {
            return false;
        }

        if (Money < station.UnlockCost)
        {
            NotEnoughMoney?.Invoke("Need EUR " + station.UnlockCost + " to unlock " + station.DisplayName);
            return false;
        }

        Money -= station.UnlockCost;
        station.SetUnlocked(true);
        regularPassengers += passengersGainedPerStation;
        Debug.Log("[GameManager] Station unlocked: " + station.DisplayName + " | Passengers: " + regularPassengers + " (+" + passengersGainedPerStation + ")");
        MoneyChanged?.Invoke();
        StationUnlocked?.Invoke(station);
        return true;
    }

    public bool TryBuyUpgrade()
    {
        if (HasUpgraded)
        {
            return false;
        }

        if (gateMajorUpgradeBehindMinorUpgrades && !AllMinorUpgradesPurchased)
        {
            NotEnoughMoney?.Invoke("Complete all minor upgrades before the hyperloop upgrade");
            return false;
        }

        if (Money < upgradeCost)
        {
            NotEnoughMoney?.Invoke("Need EUR " + upgradeCost + " for hyperloop upgrade");
            return false;
        }

        Money -= upgradeCost;
        regularPassengers = Mathf.Max(0, upgradedPassengerCount);
        HasUpgraded = true;
        SetVipTimersPaused(false);
        MoneyChanged?.Invoke();
        return true;
    }

    public MinorUpgradeDefinition GetMinorUpgrade(int index)
    {
        if (minorUpgrades == null || index < 0 || index >= minorUpgrades.Count)
        {
            return null;
        }
        return minorUpgrades[index];
    }

    public bool IsMinorUpgradePurchased(int index)
    {
        if (minorUpgradesPurchased == null || index < 0 || index >= minorUpgradesPurchased.Count)
        {
            return false;
        }
        return minorUpgradesPurchased[index];
    }

    public bool CanBuyMinorUpgrade(int index)
    {
        MinorUpgradeDefinition def = GetMinorUpgrade(index);
        if (def == null)
        {
            return false;
        }

        if (IsMinorUpgradePurchased(index))
        {
            return false;
        }

        return Money >= def.cost;
    }

    public bool TryBuyMinorUpgrade(int index)
    {
        MinorUpgradeDefinition def = GetMinorUpgrade(index);
        if (def == null)
        {
            return false;
        }

        if (IsMinorUpgradePurchased(index))
        {
            return false;
        }

        if (Money < def.cost)
        {
            NotEnoughMoney?.Invoke("Need EUR " + def.cost + " for " + def.displayName);
            return false;
        }

        Money -= def.cost;
        minorUpgradesPurchased[index] = true;
        Debug.Log("[GameManager] Minor upgrade purchased: " + def.displayName + " | +" + def.passiveIncomeBonusPerSecond + " EUR/s");
        MinorUpgradePurchased?.Invoke(index);
        MoneyChanged?.Invoke();
        return true;
    }

    private void AddPassiveIncome(float deltaTime)
    {
        passiveIncomeBank += PassiveIncomePerSecond * deltaTime;

        if (passiveIncomeBank >= 1f)
        {
            float wholeMoney = Mathf.Floor(passiveIncomeBank);
            passiveIncomeBank -= wholeMoney;
            Money += wholeMoney;
            MoneyChanged?.Invoke();
        }
    }

    private void TickVipTimers(float deltaTime)
    {
        bool changed = false;

        for (int i = waitingVips.Count - 1; i >= 0; i--)
        {
            VipPassenger vip = waitingVips[i];
            vip.Tick(deltaTime);

            if (vip.PickupTimeRemaining <= 0f)
            {
                waitingVips.RemoveAt(i);
                vip.MarkExpired();
                ApplyPenalty(vip, vip.MissedPickupPenalty);
                VipPenalized?.Invoke(vip, vip.MissedPickupPenalty);
                changed = true;
            }
        }

        for (int i = onboardVips.Count - 1; i >= 0; i--)
        {
            VipPassenger vip = onboardVips[i];
            vip.Tick(deltaTime);

            if (vip.DeliveryTimeRemaining <= 0f)
            {
                onboardVips.RemoveAt(i);
                vip.MarkExpired();
                ApplyPenalty(vip, vip.MissedDeliveryPenalty);
                VipPenalized?.Invoke(vip, vip.MissedDeliveryPenalty);
                changed = true;
            }
        }

        if (changed)
        {
            VipsChanged?.Invoke();
        }
    }

    private void AddMoney(int amount)
    {
        Money += amount;
        MoneyChanged?.Invoke();
    }

    private void ApplyPenalty(VipPassenger vip, int penalty)
    {
        Money = Mathf.Max(0f, Money - penalty);
        MoneyChanged?.Invoke();
    }
    public bool BuyArt(float cost)
    {
        if (Money >= cost)
        {
            Money -= cost;
            return true;
        }
        return false;
    }
}
