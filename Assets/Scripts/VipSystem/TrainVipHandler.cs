using UnityEngine;

public class TrainVipHandler : MonoBehaviour
{
    [SerializeField] private Transform trainTransform;
    [SerializeField] private float travelSpeed = 8f;
    [SerializeField] private Station startingStation;
    [SerializeField] private bool moveTrainTransform = false;
    [SerializeField] private bool debugVipLogs = true;

    public Station CurrentStation { get; private set; }
    public Station TargetStation { get; private set; }
    public bool IsTravelling => TargetStation != null;

    public void Configure(Transform newTrainTransform, Station newStartingStation, bool shouldMoveTrainTransform)
    {
        if (trainTransform == null)
        {
            trainTransform = newTrainTransform;
        }
        if (startingStation == null)
        {
            startingStation = newStartingStation;
        }
        moveTrainTransform = shouldMoveTrainTransform;

        if (startingStation != null && GameManager.Instance != null)
        {
            ArriveAtStation(startingStation);
        }
    }

    private void Start()
    {
        moveTrainTransform = false;

        if (trainTransform == null)
        {
            trainTransform = transform;
        }

        if (startingStation != null && GameManager.Instance != null)
        {
            ArriveAtStation(startingStation);
        }
    }

    private void Update()
    {
        if (!moveTrainTransform || TargetStation == null || trainTransform == null)
        {
            return;
        }

        trainTransform.position = Vector3.MoveTowards(
            trainTransform.position,
            TargetStation.transform.position,
            travelSpeed * Time.deltaTime);

        if (Vector3.Distance(trainTransform.position, TargetStation.transform.position) <= 0.05f)
        {
            ArriveAtStation(TargetStation);
        }
    }

    public void RequestTravelTo(Station station)
    {
        if (station == null || station == CurrentStation)
        {
            if (debugVipLogs)
            {
                Debug.Log("TrainVipHandler: RequestTravelTo ignored | requested=" + (station != null ? station.DisplayName : "null") + " | current=" + (CurrentStation != null ? CurrentStation.DisplayName : "none"));
            }
            return;
        }

        TargetStation = station;

        if (debugVipLogs)
        {
            Debug.Log("TrainVipHandler: target station set | target=" + TargetStation.DisplayName + " | current=" + (CurrentStation != null ? CurrentStation.DisplayName : "none"));
        }
    }

    public void ArriveAtStation(Station station)
    {
        if (station == null || GameManager.Instance == null)
        {
            if (debugVipLogs)
            {
                Debug.LogWarning("TrainVipHandler: ArriveAtStation ignored | station=" + (station != null ? station.DisplayName : "null") + " | gameManager=" + (GameManager.Instance != null ? "present" : "missing"));
            }
            return;
        }

        CurrentStation = station;
        TargetStation = null;
        GameManager.Instance.SetCurrentStation(station);

        if (debugVipLogs)
        {
            Debug.Log("TrainVipHandler: arrived at station | current=" + CurrentStation.DisplayName + " | stationId=" + CurrentStation.StationId);
        }

        if (GameManager.Instance.TryDeliverOnboardVipAtStation(station.StationId, out VipPassenger deliveredVip))
        {
            Debug.Log("Delivered VIP " + deliveredVip.PassengerName + " to " + station.DisplayName);
            return;
        }

        if (GameManager.Instance.TryPickupVipAtStation(station.StationId, out VipPassenger pickedUpVip))
        {
            Debug.Log("Picked up VIP " + pickedUpVip.PassengerName + " at " + station.DisplayName);
        }
    }
}
