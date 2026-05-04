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
    public bool moveCameraForParallax = true;
    public float cameraTravelDistance = 500f;
    public float cameraZPosition = -10f;
    public bool debugMovementLogs = true;
    public float visualTravelScreenDistance = 0f;
    public float visualTravelVerticalBob = 0f;

    [Header("2D Lock")]
    public bool lockZPosition = true;
    public float zPosition = 0f;

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

        travelElapsed += Time.deltaTime;
        float duration = Mathf.Max(0.01f, activeTravelDuration);
        float progress = Mathf.Clamp01(travelElapsed / duration);
        float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
        movingObject.position = Vector3.Lerp(movingObjectStartPosition, movingObjectTargetPosition, easedProgress);

        if (debugMovementLogs && progress - lastProgressLog >= 0.25f)
        {
            lastProgressLog = progress;
            Debug.Log("TrainMover: travelling progress " + Mathf.RoundToInt(progress * 100f) + "% | movingObject=" + movingObject.name + " | position=" + movingObject.position);
        }

        CacheActiveTransportVisual();
        ApplyTransportVisualOffset(progress);

        if (!moveCameraForParallax)
        {
            transform.position = movingObject.position;

            if (lockZPosition)
            {
                Vector3 position = transform.position;
                position.z = zPosition;
                transform.position = position;
            }
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

        movingObject = moveCameraForParallax && Camera.main != null ? Camera.main.transform : transform;
        if (moveCameraForParallax && Camera.main != null)
        {
            Vector3 cameraPosition = movingObject.position;
            cameraPosition.z = cameraZPosition;
            movingObject.position = cameraPosition;
        }
        
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

        movingObject = moveCameraForParallax && Camera.main != null ? Camera.main.transform : transform;
        if (moveCameraForParallax && Camera.main != null)
        {
            Vector3 cameraPosition = movingObject.position;
            cameraPosition.z = cameraZPosition;
            movingObject.position = cameraPosition;
        }

        CacheActiveTransportVisual();
        ResetTransportVisualOffset();

        float distance = Vector3.Distance(transform.position, destination.position);
        Vector3 destinationPosition = destination.position;

        if (distance < minimumTravelDistance)
        {
            float direction = destinationPosition.x >= transform.position.x ? 1f : -1f;
            destinationPosition = transform.position + Vector3.right * direction * minimumTravelDistance;
            destinationPosition.y = destination.position.y;
            destinationPosition.z = destination.position.z;
            distance = minimumTravelDistance;
        }

        movementStatus = TrainMovementStatus.Travelling;
        target = destination;
        targetPosition = destinationPosition;
        travelStartPosition = transform.position;
        activeTravelDirection = Mathf.Sign(destinationPosition.x - travelStartPosition.x);
        if (Mathf.Approximately(activeTravelDirection, 0f))
        {
            activeTravelDirection = 1f;
        }
        movingObjectStartPosition = movingObject.position;
        float movementDistance = moveCameraForParallax ? cameraTravelDistance : distance;
        movingObjectTargetPosition = movingObjectStartPosition + Vector3.right * Mathf.Sign(destinationPosition.x - transform.position.x) * movementDistance;
        movingObjectTargetPosition.y = movingObjectStartPosition.y;
        movingObjectTargetPosition.z = moveCameraForParallax ? cameraZPosition : movingObjectStartPosition.z;
        travelElapsed = 0f;
        bool isHyperloop = transportSwitcher != null && transportSwitcher.IsHyperloopActive();
        float baseLegDuration = Mathf.Max(0.85f, isHyperloop ? hyperloopDuration : normalTrainDuration);
        float legWorldDistance = Vector3.Distance(travelStartPosition, destinationPosition);
        float refDist = Mathf.Max(1f, referenceWorldDistance);
        float distanceFactor = Mathf.Clamp(legWorldDistance / refDist, minTravelDurationFactor, maxTravelDurationFactor);
        activeTravelDuration = Mathf.Max(0.85f, baseLegDuration * distanceFactor);
        speed = Mathf.Max(speed, 300f, movementDistance / activeTravelDuration * 1.2f);
        acceleration = Mathf.Max(acceleration, 500f);
        lastProgressLog = 0f;

        if (debugMovementLogs)
        {
            Debug.Log("TrainMover: travel started | destination=" + destination.name + " | status=" + movementStatus + " | worldDistance=" + distance + " | cameraDistance=" + movementDistance + " | duration=" + activeTravelDuration + " | from=" + movingObjectStartPosition + " | to=" + movingObjectTargetPosition);
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

        movingObject.position = movingObjectTargetPosition;

        if (!moveCameraForParallax)
        {
            transform.position = targetPosition;
        }

        if (!moveCameraForParallax && lockZPosition)
        {
            Vector3 position = transform.position;
            position.z = zPosition;
            transform.position = position;
        }

        movementStatus = TrainMovementStatus.Arrived;
        target = null;
        travelElapsed = 0f;
        ResetTransportVisualOffset();

        if (debugMovementLogs)
        {
            Debug.Log("TrainMover: travel complete | status=" + movementStatus + " | movingObjectPosition=" + movingObject.position);
        }
        ReachedStationStop?.Invoke();
    }
}
