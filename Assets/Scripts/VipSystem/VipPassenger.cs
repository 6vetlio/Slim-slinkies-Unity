using System;
using UnityEngine;

public enum VipPassengerState
{
    Waiting,
    Onboard,
    Delivered,
    Expired
}

[Serializable]
public class VipPassenger
{
    public string Id { get; private set; }
    public string PassengerName;
    public Sprite Portrait;
    public string OriginStationId;
    public string OriginStationName;
    public string DestinationStationId;
    public string DestinationStationName;
    public float PickupTimeRemaining;
    public float DeliveryTimeRemaining;
    public int DeliveryReward;
    public int MissedPickupPenalty;
    public int MissedDeliveryPenalty;
    public VipPassengerState State { get; private set; }

    public bool IsWaiting => State == VipPassengerState.Waiting;
    public bool IsOnboard => State == VipPassengerState.Onboard;
    public bool IsFinished => State == VipPassengerState.Delivered || State == VipPassengerState.Expired;

    public VipPassenger(
        string passengerName,
        Sprite portrait,
        Station origin,
        Station destination,
        float pickupSeconds,
        float deliverySeconds,
        int deliveryReward,
        int missedPickupPenalty,
        int missedDeliveryPenalty)
    {
        Id = Guid.NewGuid().ToString("N");
        PassengerName = passengerName;
        Portrait = portrait;
        OriginStationId = origin.StationId;
        OriginStationName = origin.DisplayName;
        DestinationStationId = destination.StationId;
        DestinationStationName = destination.DisplayName;
        PickupTimeRemaining = pickupSeconds;
        DeliveryTimeRemaining = deliverySeconds;
        DeliveryReward = deliveryReward;
        MissedPickupPenalty = missedPickupPenalty;
        MissedDeliveryPenalty = missedDeliveryPenalty;
        State = VipPassengerState.Waiting;
    }

    public void Tick(float deltaTime)
    {
        if (State == VipPassengerState.Waiting)
        {
            PickupTimeRemaining = Mathf.Max(0f, PickupTimeRemaining - deltaTime);
        }
        else if (State == VipPassengerState.Onboard)
        {
            DeliveryTimeRemaining = Mathf.Max(0f, DeliveryTimeRemaining - deltaTime);
        }
    }

    public void MarkPickedUp()
    {
        if (State == VipPassengerState.Waiting)
        {
            State = VipPassengerState.Onboard;
        }
    }

    public void MarkDelivered()
    {
        if (State == VipPassengerState.Onboard)
        {
            State = VipPassengerState.Delivered;
        }
    }

    public void MarkExpired()
    {
        if (!IsFinished)
        {
            State = VipPassengerState.Expired;
        }
    }
}
