using UnityEngine;
using System.Collections.Generic;

public class ChunkManager : MonoBehaviour
{
    [SerializeField] private Transform trainTransform;
    [SerializeField] private BackgroundChunk[] landscapeChunkPrefabs;
    [SerializeField] private BackgroundChunk stationChunkPrefab;
    [SerializeField] private Transform worldContainer;
    [SerializeField] private float spawnDistance = 1000f;
    [SerializeField] private float despawnDistance = 200f;

    [Tooltip("How many landscape chunks make up one leg of travel before a station appears. " +
             "A leg = N landscape chunks (all the SAME biome) followed by one StationChunk, so the " +
             "biome only ever changes ACROSS a station, never mid-travel. The train scrolls exactly " +
             "one leg (N+1 chunk widths) per trip, so it always parks on the next station.")]
    [SerializeField] private int chunksPerLeg = 3;

    [Tooltip("Width of one chunk in worldcontainer units. Must match BackgroundChunk.chunkWidth (640). " +
             "Drives the per-leg scroll distance so a station lands centred under the static train.")]
    [SerializeField] private float referenceChunkWidth = 640f;

    [Tooltip("Auto-place the strip so the first StationChunk lands under the train exactly one leg " +
             "after departure. Because the station cadence equals the leg scroll, every later arrival " +
             "also lands on a station. Leave on; use stationAlignNudge for fine pixel tuning.")]
    [SerializeField] private bool autoAlignStationToTrain = true;

    [Tooltip("Fine offset (world units) added to the auto-aligned strip so the station sits dead-centre " +
             "under the train. Tune live while watching an arrival. Ignored if auto-align is off.")]
    [SerializeField] private float stationAlignNudge = 150f;

    [Tooltip("Manual strip offset, used only when auto-align is OFF.")]
    [SerializeField] private float startPhaseX = 0f;

    [Tooltip("Local Y/Z spawned chunks are placed at, in worldContainer space. Must match the hand-placed seed chunks (they sit at 0,0) so spawned chunks line up with the track instead of floating above it.")]
    [SerializeField] private float spawnLocalY = 0f;
    [SerializeField] private float spawnLocalZ = 0f;

    private List<BackgroundChunk> activeChunks = new List<BackgroundChunk>();
    private float nextChunkLocalX = 0f; // Local position relative to worldContainer
    private int chunkCount = 0;

    // Biomes grouped from landscapeChunkPrefabs (by name: hill* / acre* / city*),
    // kept in first-appearance order so legs cycle through them in a stable order.
    private List<List<BackgroundChunk>> biomeGroups = new List<List<BackgroundChunk>>();
    private int legBiomeIndex = 0;  // which biome group the current leg uses
    private int chunkInLeg = 0;     // 0..chunksPerLeg-1 landscape, then the station
    private int memberIndex = 0;    // walks members inside the current biome group

    // The exact world distance the train must scroll for ONE leg: N landscape
    // chunks + 1 station = (N+1) chunk widths. TrainMovement reads this so every
    // trip advances exactly one station-spacing and parks on the next station.
    public float LegScrollDistance => (Mathf.Max(1, chunksPerLeg) + 1) * Mathf.Max(1f, referenceChunkWidth);

    // True only when this runtime-spawner is actually configured. When the
    // worldContainer ref or the landscape prefab list is missing, the component
    // no-ops instead of throwing an UnassignedReferenceException every frame.
    private bool IsConfigured =>
        worldContainer != null
        && trainTransform != null
        && landscapeChunkPrefabs != null
        && landscapeChunkPrefabs.Length > 0;

    private void Start()
    {
        if (!IsConfigured)
        {
            Debug.LogWarning("[ChunkManager] Disabled — no worldContainer/landscape prefabs assigned.");
            enabled = false;
            return;
        }

        BuildBiomeGroups();

        if (autoAlignStationToTrain)
        {
            // The first station is the (chunksPerLeg+1)-th chunk, at local
            //   X_station0 = startPhaseX + chunksPerLeg * W.
            // One leg later the world has scrolled LegScrollDistance = (chunksPerLeg+1)*W,
            // so that chunk's centre reaches the train when:
            //   startPhaseX = trainLocalX + LegScrollDistance - chunksPerLeg*W - W/2
            //               = trainLocalX + W - W/2  (the +1 leg minus the N chunks),
            // i.e. one extra chunk minus half a chunk to centre the 640-wide station.
            // trainLocalX is the train's position in the container's own local space.
            float trainLocalX = TrainLocalX();
            float W = Mathf.Max(1f, referenceChunkWidth);
            startPhaseX = trainLocalX + W - (W * 0.5f) + stationAlignNudge;
            Debug.Log($"[ChunkManager] auto-align: trainLocalX={trainLocalX:0.0} startPhaseX={startPhaseX:0.0} leg={LegScrollDistance:0} nudge={stationAlignNudge:0}");
        }

        nextChunkLocalX = startPhaseX;

        for (int i = 0; i < 3; i++)
        {
            SpawnNextChunk();
        }
    }

    private void Update()
    {
        if (!IsConfigured) return;

        // Work entirely in worldContainer-LOCAL space. The container is a UI child of a
        // CanvasScaler-driven Canvas, so its lossyScale is ~1.07 and RESOLUTION-DEPENDENT.
        // Mixing chunk local coords (the grid) with world positions made the grid advance
        // ~1.07x faster than the scroll — a ~170px drift per leg that compounded into
        // off-centre stations and blue gaps after a few stops. TrainMovement now scrolls
        // worldContainer.localPosition by the same local units, so everything stays exact.
        float trainLocalX = TrainLocalX();

        float W = Mathf.Max(1f, referenceChunkWidth);
        float effectiveSpawn = Mathf.Max(spawnDistance, W * 2f);
        float effectiveDespawn = Mathf.Max(despawnDistance, W * 3f);

        // Keep spawning until the strip is filled past the spawn horizon. A single 'if'
        // could lag behind during fast multi-leg travel or a frame hitch, leaving a blue
        // hole; the while loop guarantees the world ahead of the train is always populated.
        int guard = 0;
        while ((nextChunkLocalX - trainLocalX) < effectiveSpawn && guard++ < 64)
        {
            SpawnNextChunk();
        }

        // Despawn chunks far behind (left edge well past the train, off-screen left).
        for (int i = activeChunks.Count - 1; i >= 0; i--)
        {
            float distanceBehind = trainLocalX - activeChunks[i].transform.localPosition.x;

            if (distanceBehind > effectiveDespawn)
            {
                Destroy(activeChunks[i].gameObject);
                activeChunks.RemoveAt(i);
            }
        }
    }

    // The train's X expressed in worldContainer-local coordinates: the local X at which a
    // chunk sits directly under the static train. InverseTransformPoint folds in the
    // container's position AND its (scaler-dependent) scale, so callers never mix spaces.
    private float TrainLocalX()
    {
        return worldContainer.InverseTransformPoint(trainTransform.position).x;
    }

    private void SpawnNextChunk()
    {
        BackgroundChunk chunkToSpawn;

        if (stationChunkPrefab != null && chunkInLeg >= Mathf.Max(1, chunksPerLeg))
        {
            // End of the leg → the station. Biome advances AFTER it, so the next
            // leg is a different biome — i.e. the scenery only ever changes here.
            chunkToSpawn = Instantiate(stationChunkPrefab);
            chunkToSpawn.chunkType = BackgroundChunk.ChunkType.Station;
            Debug.Log($"[ChunkManager] STATION spawned localX={nextChunkLocalX:0} | trainLocalX={TrainLocalX():0} | centresUnderTrainAfter={(nextChunkLocalX - TrainLocalX()):0} local units of scroll");
            chunkInLeg = 0;
            if (biomeGroups.Count > 0)
            {
                legBiomeIndex = (legBiomeIndex + 1) % biomeGroups.Count;
            }
            memberIndex = 0;
        }
        else
        {
            // A landscape chunk from the CURRENT leg's biome. Members of the group
            // cycle in order so a leg reads as a designed sequence, not random jumps,
            // and adjacent same-biome chunks tile seamlessly (uniform chunkWidth).
            BackgroundChunk prefab = NextLandscapePrefab();
            chunkToSpawn = Instantiate(prefab);
            chunkToSpawn.chunkType = BackgroundChunk.ChunkType.Landscape;
            chunkInLeg++;
        }

        // Parent WITHOUT keeping the world transform (worldPositionStays = false), then
        // pin the local transform explicitly. The default SetParent(worldContainer)
        // rescales the clone to preserve its world scale under the (non-unit-scaled)
        // worldContainer — every chunk came out at localScale ~0.937, so its 640px art
        // rendered ~600px wide and a bit short, leaving a ~40px BLUE GAP at every seam
        // (and blue above) during travel. localScale 1 matches the hand-placed seed /
        // station chunks, so spawned chunks tile flush with no gaps.
        chunkToSpawn.transform.SetParent(worldContainer, false);
        chunkToSpawn.transform.localScale = Vector3.one;
        float placeX = nextChunkLocalX;
        if (chunkToSpawn.chunkType == BackgroundChunk.ChunkType.Station)
            placeX += 150f;
        chunkToSpawn.transform.localPosition = new Vector3(placeX, spawnLocalY, spawnLocalZ);

        activeChunks.Add(chunkToSpawn);
        nextChunkLocalX += chunkToSpawn.ChunkWidth;
        chunkCount++;
    }

    private BackgroundChunk NextLandscapePrefab()
    {
        if (biomeGroups.Count == 0)
        {
            // Defensive: no grouping (shouldn't happen once configured) — fall back
            // to the flat list so the world still populates.
            return landscapeChunkPrefabs[chunkCount % landscapeChunkPrefabs.Length];
        }

        List<BackgroundChunk> group = biomeGroups[legBiomeIndex % biomeGroups.Count];
        BackgroundChunk prefab = group[memberIndex % group.Count];
        memberIndex++;
        return prefab;
    }

    // Group the flat prefab list into biomes by name (hill* / acre* / city*),
    // preserving the order each biome first appears so legs cycle predictably.
    private void BuildBiomeGroups()
    {
        biomeGroups.Clear();
        Dictionary<string, List<BackgroundChunk>> byKey = new Dictionary<string, List<BackgroundChunk>>();

        for (int i = 0; i < landscapeChunkPrefabs.Length; i++)
        {
            BackgroundChunk prefab = landscapeChunkPrefabs[i];
            if (prefab == null) continue;

            string key = BiomeKey(prefab.name);
            if (!byKey.TryGetValue(key, out List<BackgroundChunk> group))
            {
                group = new List<BackgroundChunk>();
                byKey[key] = group;
                biomeGroups.Add(group); // first-appearance order = cycle order
            }
            group.Add(prefab);
        }
    }

    private static string BiomeKey(string prefabName)
    {
        string n = prefabName != null ? prefabName.ToLowerInvariant() : "";
        // "city*" first — cityendhill/citystartacre contain "hill"/"acre" too.
        if (n.StartsWith("city")) return "city";
        if (n.Contains("hill")) return "hills";
        if (n.Contains("acre")) return "acre";
        return "other";
    }
}
