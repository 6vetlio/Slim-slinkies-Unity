using System;
using System.Collections.Generic;
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

    [Tooltip("Seconds the train takes to traverse a single chunk. Travel duration = chunkCount * secondsPerChunk.")]
    public float secondsPerChunk = 2.5f;

    [Tooltip("Fallback travel duration in seconds for legs with no RouteSegment configured. Lower = faster.")]
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

    [Tooltip("Ordered list of minor upgrades the player must purchase one-by-one before this tier's next-tier CTA unlocks.")]
    public List<MinorUpgradeDefinition> minorUpgrades = new List<MinorUpgradeDefinition>();
}
