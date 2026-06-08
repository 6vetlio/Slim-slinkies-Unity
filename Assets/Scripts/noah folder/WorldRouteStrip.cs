using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The pre-built station world. Lives on <c>worldcontainer</c>.
///
/// Instead of spawning chunks at runtime, the whole route is authored once in the
/// scene as a horizontal strip of "region cards" (each card = one biome background
/// + that station's prop), placed in route order as children of worldcontainer.
///
/// This component just maps a stationId to the worldcontainer local X that parks
/// that region under the static train. <see cref="TrainMovement"/> reads it and
/// slides worldcontainer to that absolute target during a trip, so the train always
/// stops exactly at a station — and the station art is already there waiting.
///
/// No <c>Instantiate</c>, no <c>Update</c>. Pure lookup over pre-placed children.
///
/// Setup (6vetlio, live in Unity):
///  - Add this component to the <c>worldcontainer</c> object.
///  - Author one region card per station as a child of worldcontainer, laid out
///    left-to-right in route order.
///  - For each station, add an <see cref="entries"/> row: its stationId + that
///    region card's RectTransform.
///  - <see cref="restLocalX"/> is the worldcontainer.localPosition.x at which a
///    region whose own localPosition.x == 0 sits centered under the train. Leave
///    at 0 if your home/first card is at localX 0 and centered at rest.
/// </summary>
[DisallowMultipleComponent]
public class WorldRouteStrip : MonoBehaviour
{
    [System.Serializable]
    public struct RegionEntry
    {
        [Tooltip("Must match the Station's stationId exactly (e.g. \"groningen\").")]
        public string stationId;

        [Tooltip("The region card for this station — a child of worldcontainer.")]
        public RectTransform regionRoot;
    }

    [Tooltip("One row per station, in route order. stationId -> region card.")]
    [SerializeField] private List<RegionEntry> entries = new List<RegionEntry>();

    [Tooltip("worldcontainer.localPosition.x at which a region with localX 0 is centered under the train. Usually 0.")]
    [SerializeField] private float restLocalX = 0f;

    /// <summary>
    /// The worldcontainer local X that centers the given station's region under the
    /// (static) train. Returns false when the station isn't mapped, so callers can
    /// fall back to the legacy relative-distance scroll.
    /// </summary>
    public bool TryGetTarget(string stationId, out float targetLocalX)
    {
        targetLocalX = 0f;
        if (string.IsNullOrEmpty(stationId) || entries == null)
        {
            return false;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            RegionEntry e = entries[i];
            if (e.regionRoot != null && e.stationId == stationId)
            {
                // Slide so the card's local origin lands at the rest (centered) X.
                targetLocalX = restLocalX - e.regionRoot.localPosition.x;
                return true;
            }
        }

        return false;
    }

    /// <summary>Convenience accessor; returns restLocalX when unmapped.</summary>
    public float GetTargetLocalX(string stationId)
    {
        return TryGetTarget(stationId, out float x) ? x : restLocalX;
    }

    public bool HasAnyEntries => entries != null && entries.Count > 0;
}
