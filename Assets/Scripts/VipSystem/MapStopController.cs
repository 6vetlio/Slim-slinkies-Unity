using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MapStopController : MonoBehaviour
{
    [Header("Train")]
    [SerializeField] private TrainMover trainMover;
    [SerializeField] private TrainVipHandler trainVipHandler;
    [SerializeField] private Station startingStation;

    [Header("Map")]
    [SerializeField] private GameObject mapRoot;
    [SerializeField] private Button openMapButton;
    [SerializeField] private VipSpawnManager vipSpawnManager;
    [SerializeField] private MapVisualizer mapVisualizer;
    [SerializeField] private bool spawnVipWhenMapOpens = true;

    [Header("UI")]
    [SerializeField] private GameObject popupText;
    [SerializeField] private string travelingBlockMessage = "You can't change destination while driving — this train goes 70 km/h, not 1100!";
    [SerializeField] private bool debugMapLogs = false;

    [Header("Hide When Map Open")]
    [SerializeField] private CanvasGroup[] gameplayElementsToHide;

    private Station pendingArrivalStation;
    private bool mapOpen;
    private TrainMover subscribedTrainMover;
    private bool subscribedToStationClicks;
    private CanvasGroup mapCanvasContentGroup;

    public void Configure(
        TrainMover newTrainMover,
        TrainVipHandler newTrainVipHandler,
        Station newStartingStation,
        GameObject newMapRoot,
        VipSpawnManager newVipSpawnManager,
        Button newOpenMapButton = null,
        MapVisualizer newMapVisualizer = null)
    {
        UnsubscribeFromTrainMover();

        if (trainMover == null)
        {
            trainMover = newTrainMover;
        }
        if (trainVipHandler == null)
        {
            trainVipHandler = newTrainVipHandler;
        }
        if (startingStation == null)
        {
            startingStation = newStartingStation;
        }
        if (mapRoot == null)
        {
            mapRoot = newMapRoot;
        }
        if (vipSpawnManager == null)
        {
            vipSpawnManager = newVipSpawnManager;
        }
        if (openMapButton == null)
        {
            openMapButton = newOpenMapButton;
        }
        if (mapVisualizer == null)
        {
            mapVisualizer = newMapVisualizer;
        }

        if (isActiveAndEnabled && trainMover != null)
        {
            SubscribeToTrainMover();
        }

        EnsureMapCanvasGroup();
        ApplyMapPanelVisibility(false);

        ConfigureMapButton();
    }

    private void Awake()
    {
        EnsureMapCanvasGroup();
        ApplyMapPanelVisibility(false);

        if (vipSpawnManager != null)
        {
            vipSpawnManager.enabled = true;
        }
    }

    private void OnEnable()
    {
        SubscribeToTrainMover();
        SubscribeToStationClicks();
        ConfigureMapButton();
    }

    private void OnDisable()
    {
        UnsubscribeFromTrainMover();
        UnsubscribeFromStationClicks();

        if (openMapButton != null)
        {
            openMapButton.onClick.RemoveListener(HandleOpenMapButtonClicked);
        }
    }

    private void Start()
    {
        if (startingStation != null && trainVipHandler != null)
        {
            trainVipHandler.ArriveAtStation(startingStation);
            Debug.Log("Starting station: " + startingStation.DisplayName);
        }

        RefreshDockedState();
        StartCoroutine(SyncMapRoutesDeferred());
    }

    private IEnumerator SyncMapRoutesDeferred()
    {
        yield return null;
        if (mapVisualizer != null)
            mapVisualizer.SyncRoutesNow();
    }

    private void EnsureMapCanvasGroup()
    {
        if (mapRoot == null)
        {
            return;
        }

        mapRoot.SetActive(true);

        mapCanvasContentGroup = mapRoot.GetComponent<CanvasGroup>();
        if (mapCanvasContentGroup == null)
        {
            mapCanvasContentGroup = mapRoot.AddComponent<CanvasGroup>();
        }
    }

    private void ApplyMapPanelVisibility(bool visible)
    {
        if (mapCanvasContentGroup != null)
        {
            mapCanvasContentGroup.alpha = visible ? 1f : 0f;
            mapCanvasContentGroup.interactable = visible;
            mapCanvasContentGroup.blocksRaycasts = visible;
        }
        else if (mapRoot != null)
        {
            mapRoot.SetActive(visible);
        }

        if (gameplayElementsToHide != null)
        {
            foreach (var cg in gameplayElementsToHide)
            {
                if (cg == null) continue;
                cg.alpha = visible ? 1f : 0f;
                cg.interactable = visible;
                cg.blocksRaycasts = visible;
            }
        }
    }

    private void SubscribeToTrainMover()
    {
        if (trainMover == null || subscribedTrainMover == trainMover)
        {
            return;
        }

        if (subscribedTrainMover != null)
        {
            subscribedTrainMover.ReachedStationStop -= HandleTrainReachedStop;
        }

        trainMover.ReachedStationStop -= HandleTrainReachedStop;
        trainMover.ReachedStationStop += HandleTrainReachedStop;
        subscribedTrainMover = trainMover;

        if (debugMapLogs)
        {
            Debug.Log("MapStopController: subscribed to TrainMover arrival event | trainMover=" + trainMover.name);
        }
    }

    private void UnsubscribeFromTrainMover()
    {
        if (subscribedTrainMover == null)
        {
            return;
        }

        subscribedTrainMover.ReachedStationStop -= HandleTrainReachedStop;
        subscribedTrainMover = null;
    }

    private void SubscribeToStationClicks()
    {
        if (subscribedToStationClicks)
        {
            return;
        }

        Station.Clicked -= HandleStationClicked;
        Station.Clicked += HandleStationClicked;
        subscribedToStationClicks = true;
    }

    private void UnsubscribeFromStationClicks()
    {
        if (!subscribedToStationClicks)
        {
            return;
        }

        Station.Clicked -= HandleStationClicked;
        subscribedToStationClicks = false;
    }

    public void CloseMapWithoutDeparting()
    {
        SetMapOpen(false);
    }

    private void HandleTrainReachedStop()
    {
        if (debugMapLogs)
        {
            Debug.Log("MapStopController: train reached stop event | pendingArrivalStation=" + (pendingArrivalStation != null ? pendingArrivalStation.DisplayName : "none") + " | moverStatus=" + (trainMover != null ? trainMover.MovementStatus.ToString() : "missing"));
        }

        if (trainMover != null)
        {
            trainMover.ForceStationary();
        }

        if (pendingArrivalStation != null && trainVipHandler != null)
        {
            trainVipHandler.ArriveAtStation(pendingArrivalStation);
            pendingArrivalStation = null;
        }

        RefreshDockedState();
    }

    private void HandleStationClicked(Station station)
    {
        if (!mapOpen || station == null || trainMover == null)
        {
            if (debugMapLogs)
            {
                Debug.Log("MapStopController: station click ignored | mapOpen=" + mapOpen + " | station=" + (station != null ? station.DisplayName : "null") + " | trainMover=" + (trainMover != null ? "assigned" : "null"));
            }
            return;
        }

        if (debugMapLogs)
        {
            Debug.Log("MapStopController: station clicked | station=" + station.DisplayName + " | currentStation=" + (trainVipHandler != null && trainVipHandler.CurrentStation != null ? trainVipHandler.CurrentStation.DisplayName : "none") + " | moverStatus=" + trainMover.MovementStatus + " | isStopped=" + trainMover.IsStoppedAtStation);
        }

        if (!station.IsUnlocked)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TryUnlockStation(station);
            }
            return;
        }

        if (IsCurrentStation(station))
        {
            if (debugMapLogs)
            {
                Debug.Log("MapStopController: clicked current station, closing map without travel | station=" + station.DisplayName + " | stationId=" + station.StationId + " | gameManagerCurrentStationId=" + (GameManager.Instance != null ? GameManager.Instance.CurrentStationId : "missing"));
            }
            pendingArrivalStation = null;
            SetMapOpen(false);
            return;
        }

        if (trainMover.MovementStatus == TrainMovementStatus.Travelling)
        {
            if (debugMapLogs)
            {
                Debug.LogWarning("MapStopController: travel blocked because train is travelling | station=" + station.DisplayName + " | moverStatus=" + trainMover.MovementStatus);
            }
            ShowPopup(travelingBlockMessage);
            return;
        }

        pendingArrivalStation = station;
        trainMover.TravelTo(station.transform);

        if (trainMover.MovementStatus == TrainMovementStatus.Travelling)
        {
            if (debugMapLogs)
            {
                Debug.Log("MapStopController: travel accepted, closing map | destination=" + station.DisplayName + " | moverStatus=" + trainMover.MovementStatus);
            }
            SetMapOpen(false);
        }
        else
        {
            if (debugMapLogs)
            {
                Debug.LogWarning("MapStopController: travel did not start, keeping map open | destination=" + station.DisplayName + " | moverStatus=" + trainMover.MovementStatus);
            }
            pendingArrivalStation = null;
        }

        RefreshDockedState();
    }

    private bool IsCurrentStation(Station station)
    {
        if (station == null)
        {
            return false;
        }

        if (trainVipHandler != null && trainVipHandler.CurrentStation != null)
        {
            if (trainVipHandler.CurrentStation == station)
            {
                return true;
            }

            if (!string.IsNullOrEmpty(trainVipHandler.CurrentStation.StationId) && trainVipHandler.CurrentStation.StationId == station.StationId)
            {
                return true;
            }
        }

        if (GameManager.Instance != null && !string.IsNullOrEmpty(GameManager.Instance.CurrentStationId) && GameManager.Instance.CurrentStationId == station.StationId)
        {
            return true;
        }

        return false;
    }

    private void ShowPopup(string message)
    {
        Debug.Log(message);
        
        if (popupText != null)
        {
            CancelInvoke(nameof(HidePopup));
            var tmp = popupText.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = message;
            }
            popupText.SetActive(true);
            Invoke(nameof(HidePopup), 2f);
        }
    }

    private void HidePopup()
    {
        if (popupText != null)
        {
            popupText.SetActive(false);
        }
    }

    private void OpenMap()
    {
        SetMapOpen(true);

        if (vipSpawnManager != null)
        {
            vipSpawnManager.enabled = true;

            if (spawnVipWhenMapOpens && GameManager.Instance != null && GameManager.Instance.WaitingVips.Count == 0)
            {
                vipSpawnManager.SpawnVip();
            }
        }
    }

    private void SetMapOpen(bool open)
    {
        mapOpen = open;

        if (debugMapLogs)
        {
            Debug.Log("MapStopController: map " + (open ? "opened" : "closed") + " | moverStatus=" + (trainMover != null ? trainMover.MovementStatus.ToString() : "missing"));
        }

        ApplyMapPanelVisibility(open);

        if (mapVisualizer != null)
        {
            if (open)
                mapVisualizer.ShowMapElements();
            else
                mapVisualizer.HideMapElements();
        }

        RefreshDockedState();
    }

    private void HandleOpenMapButtonClicked()
    {
        if (mapOpen)
        {
            SetMapOpen(false);
        }
        else
        {
            OpenMap();
        }
    }

    private void RefreshDockedState()
    {
        bool isDocked = trainMover != null && trainMover.IsStoppedAtStation;

        if (debugMapLogs)
        {
            Debug.Log("MapStopController: refresh docked state | isDocked=" + isDocked + " | moverStatus=" + (trainMover != null ? trainMover.MovementStatus.ToString() : "missing"));
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetVipTimersPaused(isDocked);
        }

        if (openMapButton != null)
        {
            openMapButton.interactable = true;
            openMapButton.gameObject.SetActive(true);
        }
    }

    private void ConfigureMapButton()
    {
        if (openMapButton == null)
        {
            return;
        }

        openMapButton.onClick.RemoveListener(HandleOpenMapButtonClicked);
        openMapButton.onClick.AddListener(HandleOpenMapButtonClicked);
    }
}
