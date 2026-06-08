using UnityEngine;
using System;

public enum TrainMovementStatus
{
    Stationary,
    Travelling,
    Arrived
}

public class TrainMover : MonoBehaviour
{
    public Transform pointA;
    public Transform pointB;
    public Transform pointC;
    public Transform pointD;
    public float speed = 900f;
    public float acceleration = 1200f;
    public bool stopAtPointB = true;
    public float normalTrainDuration = 25f;
    public float hyperloopDuration = 5f;
    public float referenceWorldDistance = 2500f;
    public float minTravelDurationFactor = 0.45f;
    public float maxTravelDurationFactor = 3.25f;
    public float minimumTravelDistance = 1400f;
    public bool moveCameraForParallax = false;
    public float cameraTravelDistance = 500f;
    public float cameraZPosition = -10f;
    public bool debugMovementLogs = true;
    public float visualTravelScreenDistance = 0f;
    [Tooltip("Vertical bob amount during travel. Keep at 0 to prevent the train from floating off the track.")]
    public float visualTravelVerticalBob = 0f;

    [Header("2D Lock")]
    public bool lockZPosition = true;
    public float zPosition = 0f;

    [Header("Animation")]
    [Tooltip("Animator on the train visual. Must have a bool parameter named 'IsMoving'.")]
    [SerializeField] private Animator trainAnimator;
    [Tooltip("Animator on the hyperloop visual. Must have a bool parameter named 'IsMoving'.")]
    [SerializeField] private Animator hyperloopAnimator;
    [SerializeField] private string isMovingAnimatorParam = "IsMoving";

    [SerializeField] private TrainMovementStatus movementStatus = TrainMovementStatus.Stationary;

    public event Action ReachedStationStop;

    private Transform target;
    private bool initialized = false;
    private Vector3 travelStartPosition;
    private Vector3 targetPosition;
    private Vector3 movingObjectStartPosition;
    private Vector3 movingObjectTargetPosition;
    private float travelElapsed;
    private float activeTravelDuration;
    private Transform movingObject;
    private float lastProgressLog;
    private TransportSwitcher transportSwitcher;
    private GameObject activeTransportVisual;
    private RectTransform activeTransportRectTransform;
    private Transform activeTransportTransform;
    private Vector2 activeTransportBaseAnchoredPosition;
    private Vector3 activeTransportBaseLocalPosition;
    private float activeTravelDirection = 1f;

    public TrainMovementStatus MovementStatus => movementStatus;
    public bool IsStoppedAtStation => movementStatus != TrainMovementStatus.Travelling;
    public float TravelProgress => (activeTravelDuration > 0f && target != null) ? Mathf.Clamp01(travelElapsed / activeTravelDuration) : 0f;
    public Transform TravelTargetTransform => target;

    void Start()
    {
        InitializeTrain();
    }

    void Update()
    {
        if (target == null) return;

        // Train stays put in world space — only the parallax layers convey motion.
        // Per-tier animations on the train sprite handle the "alive" look. We just
        // tick the timer and fire the arrival event when it elapses.
        travelElapsed += Time.deltaTime;
        float duration = Mathf.Max(0.01f, activeTravelDuration);
        float progress = Mathf.Clamp01(travelElapsed / duration);

        if (debugMovementLogs && progress - lastProgressLog >= 0.25f)
        {
            lastProgressLog = progress;
            Debug.Log("TrainMover: travelling progress " + Mathf.RoundToInt(progress * 100f) + "% | duration=" + duration);
        }

        if (lockZPosition)
        {
            Vector3 position = transform.position;
            position.z = zPosition;
            transform.position = position;
        }

        if (progress >= 1f)
        {
            CompleteTravel();
        }
        else if (travelElapsed > duration + 1f)
        {
            Debug.LogWarning("TrainMover: travel watchdog forced completion | elapsed=" + travelElapsed + " | duration=" + duration + " | status=" + movementStatus);
            CompleteTravel();
        }
    }
    
    private void InitializeTrain()
    {
        // Auto-find PointA and PointB if not set
        if (pointA == null)
        {
            pointA = transform.Find("PointA");
        }
        if (pointB == null)
        {
            pointB = transform.Find("PointB");
        }
        if (pointC == null)
        {
            pointC = transform.Find("PointC");
        }
        if (pointD == null)
        {
            pointD = transform.Find("PointD");
        }
        
        if (pointA == null || pointB == null)
        {
            Debug.LogError("TrainMover: PointA and PointB must be assigned or exist as child objects");
            return;
        }

        moveCameraForParallax = false;
        movingObject = transform;
        
        target = null;
        movementStatus = TrainMovementStatus.Stationary;
        initialized = true;
        CacheActiveTransportVisual();
        ResetTransportVisualOffset();

        if (debugMovementLogs)
        {
            Debug.Log("TrainMover: initialized | status=" + movementStatus + " | trainPosition=" + transform.position + " | movingObject=" + movingObject.name);
        }
    }

    public void DepartFromStation()
    {
        TravelTo(pointB);
    }

    public void TravelTo(Transform destination)
    {
        if (destination == null)
        {
            if (debugMovementLogs)
            {
                Debug.LogWarning("TrainMover: TravelTo rejected because destination is null | status=" + movementStatus);
            }
            return;
        }

        if (movementStatus == TrainMovementStatus.Travelling)
        {
            if (debugMovementLogs)
            {
                Debug.LogWarning("TrainMover: TravelTo rejected because train is already travelling | destination=" + destination.name + " | status=" + movementStatus);
            }
            return;
        }

        if (!initialized)
        {
            InitializeTrain();
        }

        if (!initialized)
        {
            if (debugMovementLogs)
            {
                Debug.LogWarning("TrainMover: TravelTo rejected because initialization failed | destination=" + destination.name + " | status=" + movementStatus);
            }
            return;
        }

        moveCameraForParallax = false;
        movingObject = transform;

        CacheActiveTransportVisual();
        ResetTransportVisualOffset();

        // Chunk-based travel. Duration = segment.chunkCount * tier.secondsPerChunk.
        // World distance the train slides = segment.chunkCount * segment.worldUnitsPerChunk.
        // No more drifting "always +1400 rightward" — same leg, same time, every time.
        string fromId = GameManager.Instance != null ? GameManager.Instance.CurrentStationId : null;
        Station destStation = destination.GetComponent<Station>();
        if (destStation == null)
        {
            destStation = destination.GetComponentInParent<Station>();
        }
        string toId = destStation != null ? destStation.StationId : null;

        RouteSegment segment = GameManager.Instance != null ? GameManager.Instance.GetSegment(fromId, toId) : null;
        int chunkCount;
        float worldUnitsPerChunk;
        if (segment != null)
        {
            chunkCount = Mathf.Max(1, segment.chunkCount);
            worldUnitsPerChunk = Mathf.Max(1f, segment.worldUnitsPerChunk);
        }
        else
        {
            // Fallback: use the hardcoded station-order to estimate distance, so
            // groningen→berlin takes 6× as long as groningen→amsterdam without
            // brutus wiring up RouteSegments. The numbers self-correct once he
            // populates the Inspector list.
            chunkCount = GameManager.Instance != null
                ? GameManager.Instance.GetFallbackChunkCount(fromId, toId)
                : 1;
            worldUnitsPerChunk = Mathf.Max(1f, minimumTravelDistance);
            if (debugMovementLogs)
            {
                Debug.Log("TrainMover: no RouteSegment for " + fromId + " → " + toId + ", using station-order fallback chunks=" + chunkCount);
            }
        }

        float secondsPerChunk = GameManager.Instance != null
            ? GameManager.Instance.CurrentTrainSecondsPerChunk
            : Mathf.Max(0.05f, normalTrainDuration / Mathf.Max(1, chunkCount));

        movementStatus = TrainMovementStatus.Travelling;
        SetMovingAnimation(true);
        target = destination;
        activeTravelDirection = 1f;
        travelStartPosition = transform.position;
        targetPosition = transform.position; // train stays put; field kept for legacy callers
        travelElapsed = 0f;
        activeTravelDuration = Mathf.Max(0.85f, chunkCount * secondsPerChunk);
        lastProgressLog = 0f;

        // Hand off to the world-scroller. Preferred path: slide worldcontainer so
        // the destination station's region card parks centered under the static
        // train (via WorldRouteStrip). Falls back to the relative `movementDistance`
        // when the station isn't mapped yet. Train stays put; the world slides.
        float movementDistance = chunkCount * worldUnitsPerChunk;
        TrainMovement chunkMovementSystem = FindFirstObjectByType<TrainMovement>();
        if (chunkMovementSystem != null)
        {
            chunkMovementSystem.InitializeChunkTravelTo(toId, movementDistance);
        }

        if (debugMovementLogs)
        {
            Debug.Log("TrainMover: travel started | from=" + fromId + " | to=" + toId + " | chunks=" + chunkCount + " | secondsPerChunk=" + secondsPerChunk + " | duration=" + activeTravelDuration + " | worldScroll=" + movementDistance);
        }
    }

    public void SkipToDestination()
    {
        if (target == null)
        {
            return;
        }

        CompleteTravel();
    }

    public void ForceStationary()
    {
        movementStatus = TrainMovementStatus.Stationary;
        SetMovingAnimation(false);
        target = null;
        travelElapsed = 0f;
        ResetTransportVisualOffset();

        if (debugMovementLogs)
        {
            Debug.Log("TrainMover: forced stationary | status=" + movementStatus);
        }
    }

    private void CacheActiveTransportVisual()
    {
        if (transportSwitcher == null)
        {
            transportSwitcher = FindFirstObjectByType<TransportSwitcher>();
        }

        GameObject nextTransportVisual = transportSwitcher != null ? transportSwitcher.GetActiveTransportVisual() : gameObject;
        if (nextTransportVisual == null)
        {
            nextTransportVisual = gameObject;
        }

        if (activeTransportVisual == nextTransportVisual)
        {
            return;
        }

        ResetTransportVisualOffset();

        activeTransportVisual = nextTransportVisual;
        activeTransportRectTransform = activeTransportVisual.GetComponent<RectTransform>();
        activeTransportTransform = activeTransportVisual.transform;

        if (activeTransportRectTransform != null)
        {
            activeTransportBaseAnchoredPosition = activeTransportRectTransform.anchoredPosition;
        }

        if (activeTransportTransform != null)
        {
            activeTransportBaseLocalPosition = activeTransportTransform.localPosition;
        }
    }

    private void ApplyTransportVisualOffset(float progress)
    {
        if (activeTransportVisual == null)
        {
            return;
        }

        if (visualTravelScreenDistance <= 0.001f && visualTravelVerticalBob <= 0.001f)
        {
            return;
        }

        float outwardBlend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.02f, 0.3f, progress));
        float settleBlend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.78f, 1f, progress));
        float horizontalOffset = activeTravelDirection * visualTravelScreenDistance * outwardBlend * (1f - settleBlend);
        float bobWeight = Mathf.Sin(progress * Mathf.PI);
        float verticalOffset = Mathf.Sin(progress * Mathf.PI * 4f) * visualTravelVerticalBob * bobWeight;

        if (activeTransportRectTransform != null)
        {
            activeTransportRectTransform.anchoredPosition = activeTransportBaseAnchoredPosition + new Vector2(horizontalOffset, verticalOffset);
            return;
        }

        if (activeTransportTransform != null)
        {
            activeTransportTransform.localPosition = activeTransportBaseLocalPosition + new Vector3(horizontalOffset, verticalOffset, 0f);
        }
    }

    private void ResetTransportVisualOffset()
    {
        if (activeTransportRectTransform != null)
        {
            activeTransportRectTransform.anchoredPosition = activeTransportBaseAnchoredPosition;
            return;
        }

        if (activeTransportTransform != null)
        {
            activeTransportTransform.localPosition = activeTransportBaseLocalPosition;
        }
    }

    private void CompleteTravel()
    {
        if (target == null)
        {
            return;
        }

        // Train stays put — no position snap. Just transition status and fire arrival.
        if (lockZPosition)
        {
            Vector3 position = transform.position;
            position.z = zPosition;
            transform.position = position;
        }

        movementStatus = TrainMovementStatus.Arrived;
        SetMovingAnimation(false);
        target = null;
        travelElapsed = 0f;
        ResetTransportVisualOffset();

        if (debugMovementLogs)
        {
            Debug.Log("TrainMover: travel complete | status=" + movementStatus + " | movingObjectPosition=" + movingObject.position);
        }
        ReachedStationStop?.Invoke();
    }

    private void SetMovingAnimation(bool isMoving)
    {
        if (string.IsNullOrEmpty(isMovingAnimatorParam))
        {
            return;
        }

        if (trainAnimator != null)
        {
            trainAnimator.SetBool(isMovingAnimatorParam, isMoving);
        }

        if (hyperloopAnimator != null)
        {
            hyperloopAnimator.SetBool(isMovingAnimatorParam, isMoving);
        }
    }
}
