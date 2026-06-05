using System;
using UnityEngine;

/// <summary>
/// A directional-but-matched-both-ways route between two adjacent stations.
/// Travel duration = chunkCount * currentTier.secondsPerChunk.
/// World distance the train slides = chunkCount * worldUnitsPerChunk, so the
/// parallax has a consistent per-chunk pace regardless of tier.
/// </summary>
[Serializable]
public class RouteSegment
{
    [Tooltip("Station id at one end of this segment.")]
    public string fromStationId = "";

    [Tooltip("Station id at the other end. Lookup is symmetric — order does not matter.")]
    public string toStationId = "";

    [Tooltip("Number of chunks between the two stations. Travel time = chunks * tier.secondsPerChunk.")]
    public int chunkCount = 10;

    [Tooltip("World units the train slides per chunk. Keeps the visual pace consistent across tiers; only the duration shrinks with faster trains.")]
    public float worldUnitsPerChunk = 140f;

    public bool Matches(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
        return (fromStationId == a && toStationId == b)
            || (fromStationId == b && toStationId == a);
    }
}
