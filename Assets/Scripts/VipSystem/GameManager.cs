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

    [Header("Routes")]
    [Tooltip("Adjacent-station route segments. Travel time = chunkCount * currentTier.secondsPerChunk. Symmetric — define each pair once.")]
    [SerializeField] private List<RouteSegment> routeSegments = new List<RouteSegment>();

    [Header("Minor Upgrades (legacy fallback)")]
    [Tooltip("Legacy global upgrade list. Used when the current train tier has no minorUpgrades configured. Migrate entries onto each TrainTierDefinition.minorUpgrades to unlock per-tier sequential lock.")]
    [SerializeField] private List<MinorUpgradeDefinition> minorUpgrades = new List<MinorUpgradeDefinition>();

    // Per-tier purchased flags. Keyed by tier index; each entry is a bool[] aligned to that tier's minorUpgrades list.
    private readonly Dictionary<int, bool[]> minorUpgradesPurchasedByTier = new Dictionary<int, bool[]>();
    // Purchased flags for the legacy global list. Separate from per-tier state so
    // progress doesn't reset when the player switches tiers while still on the
    // legacy list.
    private bool[] minorUpgradesPurchasedLegacy = new bool[0];

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

    [Header("Travel Timing")]
    [Tooltip("How many real seconds a 100 km/h train takes to cross one chunk. Per-tier travel time scales inversely with topSpeedKmh, so 700 km/h finishes 7× faster than 100 km/h on the same route.")]
    [SerializeField] private float referenceSecondsPerChunkAt100Kmh = 8f;

    // Time per chunk for the current tier is derived from its topSpeedKmh, so
    // brutus doesn't have to keep two numbers in sync per tier. Arriva (80 km/h)
    // ≈ 10s/chunk; Bullet (220) ≈ 3.6s; Hyperloop (700) ≈ 1.1s.
    public float CurrentTrainSecondsPerChunk
    {
        get
        {
            TrainTierDefinition def = CurrentTrainTierDef;
            float topSpeedKmh = def != null ? Mathf.Max(1f, def.topSpeedKmh) : 80f;
            return Mathf.Max(0.05f, referenceSecondsPerChunkAt100Kmh * 100f / topSpeedKmh);
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

    // How much faster the world should scroll relative to the base tier, derived from
    // secondsPerChunk so it tracks the chunk-based travel system instead of the legacy
    // per-leg travelDuration. Base tier returns 1.0.
    public float CurrentTierScrollMultiplier
    {
        get
        {
            float baseSeconds = (trainTiers != null && trainTiers.Count > 0 && trainTiers[0] != null)
                ? Mathf.Max(0.05f, trainTiers[0].secondsPerChunk)
                : 2.5f;
            return Mathf.Max(0.1f, baseSeconds / CurrentTrainSecondsPerChunk);
        }
    }

    public RouteSegment GetSegment(string fromId, string toId)
    {
        if (routeSegments == null) return null;
        for (int i = 0; i < routeSegments.Count; i++)
        {
            if (routeSegments[i] != null && routeSegments[i].Matches(fromId, toId))
            {
                return routeSegments[i];
            }
        }
        return null;
    }

    public bool HasRouteBetween(string fromId, string toId) => GetSegment(fromId, toId) != null;

    // True only once brutus configures the routeSegments list in the Inspector.
    // The adjacent-only travel gate keys off this — if no routes exist yet, the
    // gate is inert and every station tap is allowed (legacy behaviour).
    public bool HasAnyRoutes => routeSegments != null && routeSegments.Count > 0;

    // Hardcoded fallback chain mirroring VipSpawnManager.stationOrder. Used to
    // estimate "how many chunks apart" two stations are when no RouteSegment is
    // configured. groningen→amsterdam = 1, groningen→berlin = 6.
    private static readonly string[] defaultStationOrder = {
        "groningen", "amsterdam", "brussels", "hamburg", "paris", "hannover", "berlin"
    };

    public int GetFallbackChunkCount(string fromId, string toId)
    {
        int a = System.Array.IndexOf(defaultStationOrder, fromId);
        int b = System.Array.IndexOf(defaultStationOrder, toId);
        if (a < 0 || b < 0) return 1;
        return Mathf.Max(1, Mathf.Abs(a - b));
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
        if (!AllMinorUpgradesPurchasedForCurrentTier) return false;
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

        if (!AllMinorUpgradesPurchasedForCurrentTier)
        {
            NotEnoughMoney?.Invoke("Finish all minor upgrades for " + (CurrentTrainTierDef != null ? CurrentTrainTierDef.displayName : "this train") + " first");
            return false;
        }

        if (Money < def.cost)
        {
            NotEnoughMoney?.Invoke("Need EUR " + def.cost + " for " + def.displayName);
            return false;
        }

        Money -= def.cost;
        currentTrainTier = index;
        EnsureTierPurchasedArray(currentTrainTier);
        SetTrainTierEconomy(def.passengerBonus);
        Debug.Log("[GameManager] Train tier purchased: " + def.displayName + " | tier=" + index + " | secondsPerChunk=" + def.secondsPerChunk + " | +" + def.passengerBonus + " passengers | x" + def.incomeMultiplier + " income");
        TrainTierChanged?.Invoke(currentTrainTier);
        MoneyChanged?.Invoke();
        return true;
    }
    public float UpgradeCost => upgradeCost;
    public bool HasUpgraded { get; private set; }
    public bool CanBuyUpgrade => !HasUpgraded && Money >= upgradeCost && AllMinorUpgradesPurchasedForCurrentTier;

    // ---------- Minor upgrades (per-tier with legacy fallback) ----------

    // The current tier's own minor-upgrade list takes priority. If empty (e.g. the
    // scene hasn't been migrated yet), fall back to the legacy global list — so
    // the existing scene with 6 root entries keeps working unchanged.
    private List<MinorUpgradeDefinition> CurrentTierMinorUpgrades
    {
        get
        {
            var tierList = CurrentTrainTierDef != null ? CurrentTrainTierDef.minorUpgrades : null;
            if (tierList != null && tierList.Count > 0) return tierList;
            return minorUpgrades;
        }
    }

    // True when the active source is the legacy global list (vs. the per-tier list).
    private bool UsingLegacyMinorUpgrades
    {
        get
        {
            var tierList = CurrentTrainTierDef != null ? CurrentTrainTierDef.minorUpgrades : null;
            return tierList == null || tierList.Count == 0;
        }
    }

    public int MinorUpgradeCount =>
        CurrentTierMinorUpgrades != null ? CurrentTierMinorUpgrades.Count : 0;

    public bool AllMinorUpgradesPurchasedForCurrentTier
    {
        get
        {
            var list = CurrentTierMinorUpgrades;
            if (list == null || list.Count == 0) return true;
            bool[] purchased = GetPurchasedArrayForCurrentSource(list.Count);
            for (int i = 0; i < purchased.Length; i++)
            {
                if (!purchased[i]) return false;
            }
            return true;
        }
    }

    // Legacy alias — older callers and the major-upgrade gate referenced this name.
    public bool AllMinorUpgradesPurchased => AllMinorUpgradesPurchasedForCurrentTier;

    public float MinorUpgradeIncomeBonus
    {
        get
        {
            var list = CurrentTierMinorUpgrades;
            if (list == null || list.Count == 0) return 0f;
            bool[] purchased = GetPurchasedArrayForCurrentSource(list.Count);
            float total = 0f;
            for (int i = 0; i < list.Count; i++)
            {
                if (purchased[i] && list[i] != null)
                {
                    total += list[i].passiveIncomeBonusPerSecond;
                }
            }
            return total;
        }
    }

    // Returns the purchased-flags array matching whichever upgrade list is active.
    // Legacy global list uses minorUpgradesPurchasedLegacy; per-tier lists use the dict.
    private bool[] GetPurchasedArrayForCurrentSource(int requiredLength)
    {
        if (UsingLegacyMinorUpgrades)
        {
            if (minorUpgradesPurchasedLegacy == null || minorUpgradesPurchasedLegacy.Length != requiredLength)
            {
                minorUpgradesPurchasedLegacy = new bool[requiredLength];
            }
            return minorUpgradesPurchasedLegacy;
        }
        return GetTierPurchasedArray(currentTrainTier, requiredLength);
    }

    private bool[] GetTierPurchasedArray(int tierIndex, int requiredLength)
    {
        if (!minorUpgradesPurchasedByTier.TryGetValue(tierIndex, out bool[] arr) || arr.Length != requiredLength)
        {
            arr = new bool[requiredLength];
            minorUpgradesPurchasedByTier[tierIndex] = arr;
        }
        return arr;
    }

    private void EnsureTierPurchasedArray(int tierIndex)
    {
        TrainTierDefinition def = GetTrainTier(tierIndex);
        int len = def != null && def.minorUpgrades != null ? def.minorUpgrades.Count : 0;
        GetTierPurchasedArray(tierIndex, len);
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

        InitializeTrainTierState();
    }

    private void InitializeTrainTierState()
    {
        if (trainTiers == null || trainTiers.Count == 0)
        {
            return;
        }

        currentTrainTier = Mathf.Clamp(currentTrainTier, 0, trainTiers.Count - 1);
        for (int i = 0; i < trainTiers.Count; i++)
        {
            EnsureTierPurchasedArray(i);
        }
        TrainTierDefinition def = CurrentTrainTierDef;
        if (def != null)
        {
            SetTrainTierEconomy(def.passengerBonus);
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

        if (!AllMinorUpgradesPurchasedForCurrentTier)
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
        var list = CurrentTierMinorUpgrades;
        if (list == null || index < 0 || index >= list.Count) return null;
        return list[index];
    }

    public bool IsMinorUpgradePurchased(int index)
    {
        var list = CurrentTierMinorUpgrades;
        if (list == null || index < 0 || index >= list.Count) return false;
        bool[] purchased = GetPurchasedArrayForCurrentSource(list.Count);
        return purchased[index];
    }

    // Sequential unlock: an upgrade is unlocked only after every lower-index upgrade
    // on the current source list has been purchased. Index 0 starts unlocked.
    // While the legacy global list is in use this still applies — but the rule
    // can be made "buy in any order" by giving each tier its own list (no shared
    // ordering across tiers).
    public bool IsMinorUpgradeUnlocked(int index)
    {
        var list = CurrentTierMinorUpgrades;
        if (list == null || index < 0 || index >= list.Count) return false;

        // Legacy global list keeps the "any order" behaviour the original scene
        // shipped with, so flipping to per-tier mode is the only way the
        // sequential lock turns on.
        if (UsingLegacyMinorUpgrades) return true;

        bool[] purchased = GetPurchasedArrayForCurrentSource(list.Count);
        for (int i = 0; i < index; i++)
        {
            if (!purchased[i]) return false;
        }
        return true;
    }

    public bool CanBuyMinorUpgrade(int index)
    {
        MinorUpgradeDefinition def = GetMinorUpgrade(index);
        if (def == null) return false;
        if (IsMinorUpgradePurchased(index)) return false;
        if (!IsMinorUpgradeUnlocked(index)) return false;
        return Money >= def.cost;
    }

    public bool TryBuyMinorUpgrade(int index)
    {
        MinorUpgradeDefinition def = GetMinorUpgrade(index);
        if (def == null) return false;
        if (IsMinorUpgradePurchased(index)) return false;

        if (!IsMinorUpgradeUnlocked(index))
        {
            NotEnoughMoney?.Invoke("Buy the previous upgrade first");
            return false;
        }

        if (Money < def.cost)
        {
            NotEnoughMoney?.Invoke("Need EUR " + def.cost + " for " + def.displayName);
            return false;
        }

        Money -= def.cost;
        var list = CurrentTierMinorUpgrades;
        bool[] purchased = GetPurchasedArrayForCurrentSource(list.Count);
        purchased[index] = true;
        Debug.Log("[GameManager] Minor upgrade purchased: " + def.displayName + " | source=" + (UsingLegacyMinorUpgrades ? "legacy" : ("tier " + currentTrainTier)) + " | +" + def.passiveIncomeBonusPerSecond + " EUR/s");
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
    public bool BuyArt()
    {
        if (Money >= 500f)
        {
            Money -= 500f;
            return true;
        }
        return false;
    }
}
