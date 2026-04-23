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

    [Header("Economy")]
    [SerializeField] private float startingMoney = 0f;
    [SerializeField] private int regularPassengers = 50;
    [SerializeField] private float revenuePerPassengerPerSecond = 1f;

    [Header("Train State")]
    [SerializeField] private string currentStationId = "";

    public float Money { get; private set; }
    public int RegularPassengers => regularPassengers;
    public float PassiveIncomePerSecond => regularPassengers * revenuePerPassengerPerSecond;
    public string CurrentStationId => currentStationId;
    public VipPassenger CurrentOnboardVip { get; private set; }
    public IReadOnlyList<VipPassenger> WaitingVips => waitingVips;
    public bool VipTimersPaused { get; private set; }

    private readonly List<VipPassenger> waitingVips = new List<VipPassenger>();
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

        if (CurrentOnboardVip != null)
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
        CurrentOnboardVip = vip;
        pickedUpVip = vip;
        VipsChanged?.Invoke();
        return true;
    }

    public bool TryDeliverOnboardVipAtStation(string stationId, out VipPassenger deliveredVip)
    {
        deliveredVip = null;

        if (CurrentOnboardVip == null || CurrentOnboardVip.DestinationStationId != stationId)
        {
            return false;
        }

        deliveredVip = CurrentOnboardVip;
        deliveredVip.MarkDelivered();
        CurrentOnboardVip = null;
        AddMoney(deliveredVip.DeliveryReward);
        VipRewarded?.Invoke(deliveredVip, deliveredVip.DeliveryReward);
        VipsChanged?.Invoke();
        return true;
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

        if (CurrentOnboardVip != null)
        {
            CurrentOnboardVip.Tick(deltaTime);

            if (CurrentOnboardVip.DeliveryTimeRemaining <= 0f)
            {
                VipPassenger failedVip = CurrentOnboardVip;
                CurrentOnboardVip = null;
                failedVip.MarkExpired();
                ApplyPenalty(failedVip, failedVip.MissedDeliveryPenalty);
                VipPenalized?.Invoke(failedVip, failedVip.MissedDeliveryPenalty);
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
