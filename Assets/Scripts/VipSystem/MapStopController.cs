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
    [SerializeField] private bool spawnVipWhenMapOpens = true;

    [Header("UI")]
    [SerializeField] private GameObject popupText;

    private Station pendingArrivalStation;
    private bool mapOpen;

    public void Configure(
        TrainMover newTrainMover,
        TrainVipHandler newTrainVipHandler,
        Station newStartingStation,
        GameObject newMapRoot,
        VipSpawnManager newVipSpawnManager,
        Button newOpenMapButton = null)
    {
        if (trainMover != null)
        {
            trainMover.ReachedStationStop -= HandleTrainReachedStop;
        }

        trainMover = newTrainMover;
        trainVipHandler = newTrainVipHandler;
        startingStation = newStartingStation;
        mapRoot = newMapRoot;
        vipSpawnManager = newVipSpawnManager;
        openMapButton = newOpenMapButton;

        if (isActiveAndEnabled && trainMover != null)
        {
            trainMover.ReachedStationStop += HandleTrainReachedStop;
        }

        if (mapRoot != null)
        {
            mapRoot.SetActive(false);
        }

        ConfigureMapButton();
    }

    private void Awake()
    {
        if (mapRoot != null)
        {
            mapRoot.SetActive(false);
        }

        if (vipSpawnManager != null)
        {
            vipSpawnManager.enabled = true;
        }
    }

    private void OnEnable()
    {
        if (trainMover != null)
        {
            trainMover.ReachedStationStop += HandleTrainReachedStop;
        }

        Station.Clicked += HandleStationClicked;
        ConfigureMapButton();
    }

    private void OnDisable()
    {
        if (trainMover != null)
        {
            trainMover.ReachedStationStop -= HandleTrainReachedStop;
        }

        Station.Clicked -= HandleStationClicked;

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
        }

        RefreshDockedState();
    }

    public void CloseMapWithoutDeparting()
    {
        SetMapOpen(false);
    }

    private void HandleTrainReachedStop()
    {
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
            return;
        }

        // Prevent changing directions while driving
        if (!trainMover.IsStoppedAtStation)
        {
            ShowPopup("Cannot change destination while train is traveling");
            return;
        }

        pendingArrivalStation = station;
        SetMapOpen(false);
        trainMover.DepartFromStation();
        RefreshDockedState();
    }

    private void ShowPopup(string message)
    {
        Debug.Log(message);
        
        if (popupText != null)
        {
            var tmp = popupText.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = message;
            }
            popupText.SetActive(true);
            
            // Hide after 2 seconds
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

        if (mapRoot != null)
        {
            mapRoot.SetActive(open);
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
