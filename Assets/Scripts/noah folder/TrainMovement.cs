using UnityEngine;

public class TrainMovement : MonoBehaviour
{
    [SerializeField] private Transform worldContainer;
    [SerializeField] private TrainMover trainMover;
    [Tooltip("The pre-built station strip on worldcontainer. Auto-found if left empty.")]
    [SerializeField] private WorldRouteStrip routeStrip;
    [Tooltip("The runtime chunk spawner. When present, every leg scrolls exactly its " +
             "LegScrollDistance (N landscape chunks + 1 station) so the train always parks " +
             "on the next station and the biome only switches there. Auto-found if empty.")]
    [SerializeField] private ChunkManager chunkManager;

    // --- Legacy relative-distance mode (fallback) ---
    private float chunkLocalStartX;                // worldcontainer.localPosition.x when travel started
    private float chunkTravelDistance;             // Local distance the world must scroll this trip

    // --- Station-target mode (preferred) ---
    private bool useStationTarget;
    private float startLocalX;                     // worldcontainer.localPosition.x at travel start
    private float targetLocalX;                    // worldcontainer.localPosition.x at the destination station

    private void Awake()
    {
        if (routeStrip == null && worldContainer != null)
        {
            routeStrip = worldContainer.GetComponent<WorldRouteStrip>();
        }
        if (chunkManager == null)
        {
            chunkManager = FindFirstObjectByType<ChunkManager>();
        }
    }

    /// <summary>
    /// Preferred entry point: slide the world so the destination station's region
    /// card lands centered under the static train. Falls back to the relative
    /// distance when the station isn't mapped in the strip (or no strip exists).
    /// </summary>
    public void InitializeChunkTravelTo(string destinationStationId, float fallbackDistance)
    {
        if (worldContainer == null)
        {
            return;
        }

        if (routeStrip != null && routeStrip.TryGetTarget(destinationStationId, out float resolvedX))
        {
            useStationTarget = true;
            startLocalX = worldContainer.localPosition.x;
            targetLocalX = resolvedX;
            Debug.Log($"TrainMovement: travel to station '{destinationStationId}' | worldX {startLocalX:0} -> {targetLocalX:0}");
            return;
        }

        // No strip mapping. If the chunk spawner is driving the world, scroll exactly
        // ONE leg (N landscape chunks + 1 station). That advances the strip by exactly
        // one station-spacing, so the train parks on the next station and the biome
        // switches only there — instead of the old route-distance value that scrolled
        // an arbitrary number of chunks and ended mid-biome with no station in view.
        float distance = fallbackDistance;
        if (chunkManager != null && chunkManager.isActiveAndEnabled)
        {
            // Snap the route's intended distance to a WHOLE number of legs (so the train
            // still parks centred on a station), but don't collapse every trip to a single
            // leg — that made long routes crawl: the same 2560 spread over a 60s timer.
            // Travelling N legs keeps the world-scroll SPEED constant (distance grows with
            // the timer, ~140 u/s like the original design) and the train expresses through
            // the intermediate stations, landing centred on the final one.
            float leg = chunkManager.LegScrollDistance;
            int legs = Mathf.Max(1, Mathf.RoundToInt(fallbackDistance / leg));
            distance = legs * leg;
            Debug.Log($"TrainMovement: {legs} leg(s) x {leg:0} = {distance:0} (route asked {fallbackDistance:0})");
        }
        InitializeChunkTravel(distance);
    }

    /// <summary>
    /// Legacy: slide the world LEFT by a relative distance over the trip. Kept as a
    /// fallback for un-mapped stations.
    /// </summary>
    public void InitializeChunkTravel(float travelDistance)
    {
        useStationTarget = false;
        // Scroll in worldContainer-LOCAL units so it matches the chunk grid exactly.
        // (worldContainer is under a CanvasScaler-scaled Canvas, so its world units are
        // ~1.07x and resolution-dependent — scrolling world position drifted the grid.)
        chunkLocalStartX = worldContainer != null ? worldContainer.localPosition.x : 0f;
        chunkTravelDistance = travelDistance;

        Debug.Log($"TrainMovement: (fallback) local chunk scroll for distance {travelDistance}");
    }

    private void Update()
    {
        // Only run while the train is travelling.
        if (trainMover == null || worldContainer == null ||
            trainMover.MovementStatus != TrainMovementStatus.Travelling)
        {
            return;
        }

        float travelProgress = trainMover.TravelProgress;

        if (useStationTarget)
        {
            // Slide to the absolute destination so the train parks on the station.
            float x = Mathf.Lerp(startLocalX, targetLocalX, travelProgress);
            Vector3 local = worldContainer.localPosition;
            local.x = x;
            worldContainer.localPosition = local;
            return;
        }

        // Fallback: scroll the world LEFT by the relative distance, in LOCAL units so the
        // train traverses exactly one chunk-grid leg per (leg's worth of) distance.
        float chunkDistanceMoved = chunkTravelDistance * travelProgress;
        Vector3 lp = worldContainer.localPosition;
        lp.x = chunkLocalStartX - chunkDistanceMoved;
        worldContainer.localPosition = lp;
    }
}
