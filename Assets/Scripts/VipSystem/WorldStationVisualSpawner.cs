using UnityEngine;

/// <summary>
/// Spawns a station visual (building/platform sprite) in the world at the train's
/// stop position whenever the train arrives at a station. Solves the "stops in
/// the middle of nowhere" feel — the player now has a thing to look at when the
/// train docks.
///
/// Setup:
///  - Place this on any persistent GameObject in the scene (the [GameManagers]
///    object is fine).
///  - Assign `trainMover` (auto-found on Start if left empty).
///  - Assign `stationVisualParent` — the Transform new visuals are parented to.
///    Use a sibling of the parallax layers so the visual scrolls away with the
///    world when the train departs.
///  - Assign `stationVisualPrefab` — placeholder for now; brutus to art-pass.
/// </summary>
public class WorldStationVisualSpawner : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TrainMover trainMover;
    [SerializeField] private Transform stationVisualParent;
    [Tooltip("Prefab spawned at the train's stop position each time a leg completes. TODO: art pass.")]
    [SerializeField] private GameObject stationVisualPrefab;

    [Header("Placement")]
    [Tooltip("Offset from the train's stop position. Tweak so the platform/building lines up nicely behind the train.")]
    [SerializeField] private Vector3 spawnOffset = Vector3.zero;
    [Tooltip("Only keep the most recently spawned station visual — older ones are destroyed when a new one spawns.")]
    [SerializeField] private bool destroyPreviousOnNewArrival = true;

    private GameObject currentVisual;
    private bool subscribed;

    private void Start()
    {
        if (trainMover == null)
        {
            trainMover = FindFirstObjectByType<TrainMover>();
        }
        Subscribe();
    }

    private void OnEnable() => Subscribe();
    private void OnDisable() => Unsubscribe();

    private void Subscribe()
    {
        if (subscribed || trainMover == null) return;
        trainMover.ReachedStationStop += HandleReachedStop;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed || trainMover == null) return;
        trainMover.ReachedStationStop -= HandleReachedStop;
        subscribed = false;
    }

    private void HandleReachedStop()
    {
        if (stationVisualPrefab == null)
        {
            // Skeleton mode — log so brutus can confirm the hook fires before the prefab exists.
            Debug.Log("[WorldStationVisualSpawner] Train arrived but no stationVisualPrefab assigned. (skeleton — art pass pending)");
            return;
        }

        if (destroyPreviousOnNewArrival && currentVisual != null)
        {
            Destroy(currentVisual);
        }

        Vector3 spawnPosition = (trainMover != null ? trainMover.transform.position : transform.position) + spawnOffset;
        Transform parent = stationVisualParent != null ? stationVisualParent : transform;
        currentVisual = Instantiate(stationVisualPrefab, spawnPosition, Quaternion.identity, parent);

        string stationName = GameManager.Instance != null ? GameManager.Instance.CurrentStationId : "(unknown)";
        currentVisual.name = "StationVisual_" + stationName;
    }
}
