using System;
using UnityEngine;

[Serializable]
public class TrainTierDefinition
{
    [Tooltip("Display name shown on the upgrade button (e.g. \"Bullet Train\").")]
    public string displayName = "Train Tier";

    [Tooltip("Small icon/preview shown on the buy button.")]
    public Sprite icon;

    [Tooltip("Cost in EUR to purchase this tier. Tier 0 (the base train) should be 0.")]
    public float cost = 0f;

    [Tooltip("Travel duration in seconds for this tier. Lower = faster.")]
    public float travelDuration = 25f;

    [Tooltip("Passenger bonus added to base passenger count when this tier is active.")]
    public int passengerBonus = 0;

    [Tooltip("Multiplier applied to passive income while this tier is active. 1 = no change.")]
    public float incomeMultiplier = 1f;

    [Tooltip("Optional GameObject visual to enable when this tier is active. Leave empty to keep current placeholder visuals.")]
    public GameObject visualPrefab;

    [Tooltip("Top speed in km/h shown on the speedometer when this tier is active.")]
    public float topSpeedKmh = 80f;

    [Tooltip("Short subtitle shown under the tier name on the upgrade panel header (e.g. \"National Net\", \"High Speed\").")]
    public string subtitle = "";
}
