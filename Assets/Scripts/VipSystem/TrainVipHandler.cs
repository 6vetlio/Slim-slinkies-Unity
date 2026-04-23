using UnityEngine;

public class TrainVipHandler : MonoBehaviour
{
    [SerializeField] private Transform trainTransform;
    [SerializeField] private float travelSpeed = 8f;
    [SerializeField] private Station startingStation;
    [SerializeField] private bool moveTrainTransform = true;

    public Station CurrentStation { get; private set; }
    public Station TargetStation { get; private set; }
    public bool IsTravelling => TargetStation != null;

    public void Configure(Transform newTrainTransform, Station newStartingStation, bool shouldMoveTrainTransform)
    {
        trainTransform = newTrainTransform;
        startingStation = newStartingStation;
        moveTrainTransform = shouldMoveTrainTransform;

        if (startingStation != null && GameManager.Instance != null)
        {
            ArriveAtStation(startingStation);
        }
    }

    private void Start()
    {
        if (trainTransform == null)
        {
            trainTransform = transform;
        }

        if (startingStation != null)
        {
            ArriveAtStation(startingStation);
            if (moveTrainTransform && trainTransform != null)
            {
                trainTransform.position = startingStation.transform.position;
            }
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
            return;
        }

        TargetStation = station;
    }

    public void ArriveAtStation(Station station)
    {
        if (station == null || GameManager.Instance == null)
        {
            return;
        }

        CurrentStation = station;
        TargetStation = null;
        GameManager.Instance.SetCurrentStation(station);

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
