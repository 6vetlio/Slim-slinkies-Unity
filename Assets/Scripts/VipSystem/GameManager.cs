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

    [Header("Economy")]
    [SerializeField] private float startingMoney = 100f;
    [SerializeField] private int regularPassengers = 25;
    [SerializeField] private float revenuePerPassengerPerSecond = 0.5f;
    [SerializeField] private int passengersGainedPerStation = 10;

    [Header("Upgrade")]
    [SerializeField] private float upgradeCost = 8000f;
    [SerializeField] private int upgradedPassengerCount = 80;
    [SerializeField] private float upgradedRevenueMultiplier = 2.5f;

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
            return baseIncome;
        }
    }
    public float UpgradeCost => upgradeCost;
    public bool HasUpgraded { get; private set; }
    public bool CanBuyUpgrade => !HasUpgraded && Money >= upgradeCost;
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
}
