using System;
using UnityEngine;

[Serializable]
public class MinorUpgradeDefinition
{
    [Tooltip("Display name shown on the upgrade button.")]
    public string displayName = "Minor Upgrade";

    [Tooltip("Short description of what the upgrade does.")]
    [TextArea(1, 3)]
    public string description = "";

    [Tooltip("Cost in EUR to purchase this minor upgrade.")]
    public float cost = 250f;

    [Tooltip("Additional passive income per second granted when this minor upgrade is purchased.")]
    public float passiveIncomeBonusPerSecond = 0.5f;

    [Tooltip("Optional sprite/icon shown on the upgrade button.")]
    public Sprite icon;
}
