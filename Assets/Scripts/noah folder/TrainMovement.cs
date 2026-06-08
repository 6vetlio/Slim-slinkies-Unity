using UnityEngine;

public class TrainMovement : MonoBehaviour
{
    [SerializeField] private Transform worldContainer;
    [SerializeField] private TrainMover trainMover;
    [Tooltip("The pre-built station strip on worldcontainer. Auto-found if left empty.")]
    [SerializeField] private WorldRouteStrip routeStrip;

    // --- Legacy relative-distance mode (fallback) ---
    private Vector3 chunkWorldStartPosition;      // Where world was when travel started
    private float chunkTravelDistance;             // Total distance world needs to move

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

        // No mapping — keep the old behaviour so nothing regresses before the
        // strip is fully authored.
        InitializeChunkTravel(fallbackDistance);
    }

    /// <summary>
    /// Legacy: slide the world LEFT by a relative distance over the trip. Kept as a
    /// fallback for un-mapped stations.
    /// </summary>
    public void InitializeChunkTravel(float travelDistance)
    {
        useStationTarget = false;
        chunkWorldStartPosition = worldContainer != null ? worldContainer.position : Vector3.zero;
        chunkTravelDistance = travelDistance;

        Debug.Log($"TrainMovement: (fallback) relative chunk travel for distance {travelDistance}");
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

        // Fallback: move the world LEFT by the relative distance.
        float chunkDistanceMoved = chunkTravelDistance * travelProgress;
        worldContainer.position = chunkWorldStartPosition - Vector3.right * chunkDistanceMoved;
    }
}
