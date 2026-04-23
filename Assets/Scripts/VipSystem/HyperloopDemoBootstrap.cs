using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[ExecuteAlways]
public class HyperloopDemoBootstrap : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private TrainMover trainMover;
    [SerializeField] private TrainVipHandler trainVipHandler;
    [SerializeField] private MapStopController mapStopController;
    [SerializeField] private VipSpawnManager vipSpawnManager;
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private GameObject mapRoot;
    [SerializeField] private Button openMapButton;

    [Header("VIP References")]
    [SerializeField] private Station[] stations;
    [SerializeField] private Station startingStation;
    [SerializeField] private VipMarkerUI markerPrefab;
    [SerializeField] private Transform markerParent;

    [Header("HUD References")]
    [SerializeField] private HudController hudController;
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private TMP_Text passengersText;
    [SerializeField] private TMP_Text incomeText;
    [SerializeField] private TMP_Text onboardVipText;
    [SerializeField] private TMP_Text moneyPopupTemplate;
    [SerializeField] private Transform moneyPopupParent;

    private void OnEnable()
    {
        if (!Application.isPlaying)
        {
            EnsureSceneSetup();
        }
    }

    private void Start()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        SceneManager.sceneLoaded += HandleSceneLoaded;
        EnsureSceneSetup();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    [ContextMenu("Rebuild Scene UI")]
    private void RebuildSceneUi()
    {
        EnsureSceneSetup();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureSceneSetup();
    }

    private void EnsureSceneSetup()
    {
        EnsureCoreObjects();
        EnsureMapUi();
        EnsureHudUi();

        if (mapRoot != null)
        {
            mapRoot.SetActive(true);
        }

        if (!Application.isPlaying)
        {
            return;
        }

        WireRuntime();
    }

    private void EnsureCoreObjects()
    {
        if (trainMover == null)
        {
            trainMover = FindFirstObjectByType<TrainMover>();
        }

        if (trainMover != null && trainVipHandler == null)
        {
            trainVipHandler = trainMover.GetComponent<TrainVipHandler>();
            if (trainVipHandler == null)
            {
                trainVipHandler = trainMover.gameObject.AddComponent<TrainVipHandler>();
            }
        }

        if (mapStopController == null)
        {
            mapStopController = GetComponent<MapStopController>();
            if (mapStopController == null)
            {
                mapStopController = gameObject.AddComponent<MapStopController>();
            }
        }

        if (vipSpawnManager == null)
        {
            vipSpawnManager = GetComponentInChildren<VipSpawnManager>(true);
            if (vipSpawnManager == null)
            {
                GameObject spawnObject = new GameObject("VipSpawnManager");
                spawnObject.transform.SetParent(transform, false);
                vipSpawnManager = spawnObject.AddComponent<VipSpawnManager>();
            }
        }

        if (targetCanvas == null)
        {
            targetCanvas = FindFirstObjectByType<Canvas>();
            if (targetCanvas == null)
            {
                GameObject canvasObject = new GameObject("Canvas");
                targetCanvas = canvasObject.AddComponent<Canvas>();
                targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObject.AddComponent<CanvasScaler>();
                canvasObject.AddComponent<GraphicRaycaster>();
            }
        }

        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        GameObject gameManagerObject = GameObject.Find("GameManager");
        if (gameManagerObject == null)
        {
            gameManagerObject = new GameObject("GameManager");
        }

        if (gameManagerObject.GetComponent<GameManager>() == null)
        {
            gameManagerObject.AddComponent<GameManager>();
        }
    }

    private void EnsureMapUi()
    {
        if (targetCanvas == null)
        {
            return;
        }

        Transform existingMap = targetCanvas.transform.Find("MapRoot");
        if (mapRoot == null)
        {
            mapRoot = existingMap != null ? existingMap.gameObject : CreatePanel(targetCanvas.transform, "MapRoot", new Vector2(980f, 620f), new Color(0.05f, 0.08f, 0.1f, 0.94f)).gameObject;
        }

        RectTransform mapRect = mapRoot.GetComponent<RectTransform>();
        mapRect.anchorMin = new Vector2(0.5f, 0.5f);
        mapRect.anchorMax = new Vector2(0.5f, 0.5f);
        mapRect.pivot = new Vector2(0.5f, 0.5f);
        mapRect.anchoredPosition = Vector2.zero;

        Transform stationRootTransform = mapRoot.transform.Find("Stations");
        RectTransform stationRoot = stationRootTransform as RectTransform;
        if (stationRoot == null)
        {
            stationRoot = CreatePanel(mapRoot.transform, "Stations", new Vector2(720f, 340f), new Color(1f, 1f, 1f, 0f));
            stationRoot.anchoredPosition = new Vector2(0f, -20f);
        }

        Transform markerRootTransform = mapRoot.transform.Find("VipMarkers");
        markerParent = markerRootTransform;
        if (markerParent == null)
        {
            RectTransform markerRoot = CreatePanel(mapRoot.transform, "VipMarkers", new Vector2(900f, 520f), new Color(1f, 1f, 1f, 0f));
            markerParent = markerRoot;
        }

        EnsureStations(stationRoot);
        EnsureMarkerTemplate();
        EnsureOpenMapButton();
    }

    private void EnsureStations(RectTransform stationRoot)
    {
        stations = stationRoot.GetComponentsInChildren<Station>(true);
        if (stations != null && stations.Length > 0)
        {
            startingStation = stations[0];
            return;
        }

        string[] ids = { "amsterdam", "brussels", "paris", "berlin" };
        string[] names = { "Amsterdam", "Brussels", "Paris", "Berlin" };
        Vector2[] positions =
        {
            new Vector2(-300f, 60f),
            new Vector2(-90f, -80f),
            new Vector2(140f, -20f),
            new Vector2(320f, 120f)
        };

        stations = new Station[ids.Length];
        for (int i = 0; i < ids.Length; i++)
        {
            RectTransform buttonRect = CreatePanel(stationRoot, names[i] + " Button", new Vector2(160f, 54f), new Color(0.1f, 0.45f, 0.6f, 1f));
            buttonRect.anchoredPosition = positions[i];

            Image image = buttonRect.GetComponent<Image>();
            image.raycastTarget = true;

            Button button = buttonRect.gameObject.GetComponent<Button>();
            if (button == null)
            {
                button = buttonRect.gameObject.AddComponent<Button>();
            }
            button.targetGraphic = image;

            TMP_Text text = buttonRect.GetComponentInChildren<TMP_Text>();
            if (text == null)
            {
                text = CreateText(buttonRect, "Label", names[i], 22);
            }

            Station station = buttonRect.gameObject.GetComponent<Station>();
            if (station == null)
            {
                station = buttonRect.gameObject.AddComponent<Station>();
            }
            station.Configure(ids[i], names[i]);

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(station.SelectStation);
            stations[i] = station;
        }

        startingStation = stations[0];
    }

    private void EnsureMarkerTemplate()
    {
        if (markerParent == null)
        {
            return;
        }

        Transform existing = markerParent.Find("VipMarkerTemplate");
        if (existing != null)
        {
            markerPrefab = existing.GetComponent<VipMarkerUI>();
            return;
        }

        RectTransform marker = CreatePanel(markerParent, "VipMarkerTemplate", new Vector2(210f, 86f), new Color(0.92f, 0.84f, 0.24f, 0.96f));
        TMP_Text name = CreateText(marker, "Name", "VIP", 21);
        name.rectTransform.anchoredPosition = new Vector2(0f, 22f);
        name.color = Color.black;

        TMP_Text destination = CreateText(marker, "Destination", "To Station", 17);
        destination.rectTransform.anchoredPosition = new Vector2(0f, -6f);
        destination.color = new Color(0.05f, 0.06f, 0.07f, 1f);

        TMP_Text timer = CreateText(marker, "Timer", "20s", 17);
        timer.rectTransform.anchoredPosition = new Vector2(0f, -32f);
        timer.color = new Color(0.35f, 0.03f, 0.03f, 1f);

        markerPrefab = marker.gameObject.AddComponent<VipMarkerUI>();
        markerPrefab.ConfigureReferences(name, destination, timer, null, null, true);
        marker.gameObject.SetActive(false);
    }

    private void EnsureOpenMapButton()
    {
        if (targetCanvas == null)
        {
            return;
        }

        if (openMapButton == null)
        {
            Transform existing = targetCanvas.transform.Find("OpenMapButton");
            if (existing != null)
            {
                openMapButton = existing.GetComponent<Button>();
            }
        }

        if (openMapButton != null)
        {
            return;
        }

        RectTransform rect = CreatePanel(targetCanvas.transform, "OpenMapButton", new Vector2(160f, 52f), new Color(0.08f, 0.36f, 0.52f, 0.95f));
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-24f, 24f);

        Image image = rect.GetComponent<Image>();
        image.raycastTarget = true;

        openMapButton = rect.gameObject.AddComponent<Button>();
        openMapButton.targetGraphic = image;
        CreateText(rect, "Label", "Open Map", 20);
    }

    private void EnsureHudUi()
    {
        GameObject gameManagerObject = GameObject.Find("GameManager");
        if (gameManagerObject == null || targetCanvas == null)
        {
            return;
        }

        Transform host = gameManagerObject.transform.Find("HudControllerHost");
        if (host == null)
        {
            host = new GameObject("HudControllerHost").transform;
            host.SetParent(gameManagerObject.transform, false);
        }

        hudController = host.GetComponent<HudController>();
        if (hudController == null)
        {
            hudController = host.gameObject.AddComponent<HudController>();
        }

        Transform hudRoot = targetCanvas.transform.Find("HudRoot");
        if (hudRoot == null)
        {
            RectTransform created = CreatePanel(targetCanvas.transform, "HudRoot", new Vector2(520f, 160f), new Color(0.02f, 0.03f, 0.04f, 0.68f));
            created.anchorMin = new Vector2(0f, 1f);
            created.anchorMax = new Vector2(0f, 1f);
            created.pivot = new Vector2(0f, 1f);
            created.anchoredPosition = new Vector2(18f, -18f);
            hudRoot = created;
        }

        moneyText = EnsureHudText(hudRoot, "Money", "Money: EUR 0", new Vector2(18f, -20f));
        passengersText = EnsureHudText(hudRoot, "Passengers", "Passengers: 0", new Vector2(18f, -52f));
        incomeText = EnsureHudText(hudRoot, "Income", "Income/s: EUR 0", new Vector2(18f, -84f));
        onboardVipText = EnsureHudText(hudRoot, "OnboardVip", "Onboard VIP: None", new Vector2(18f, -116f));

        Transform popup = hudRoot.Find("MoneyPopupTemplate");
        if (popup == null)
        {
            moneyPopupTemplate = EnsureHudText(hudRoot, "MoneyPopupTemplate", "+EUR 0", new Vector2(260f, -150f), 26, TextAlignmentOptions.Center);
            moneyPopupTemplate.gameObject.SetActive(false);
        }
        else
        {
            moneyPopupTemplate = popup.GetComponent<TMP_Text>();
        }

        moneyPopupParent = hudRoot;
        hudController.Configure(moneyText, passengersText, incomeText, onboardVipText, moneyPopupTemplate, moneyPopupParent);
    }

    private void WireRuntime()
    {
        if (trainMover == null || trainVipHandler == null || mapStopController == null || vipSpawnManager == null || mapRoot == null)
        {
            Debug.LogWarning("HyperloopDemoBootstrap: Missing runtime references.");
            return;
        }

        if ((stations == null || stations.Length == 0) && mapRoot != null)
        {
            stations = mapRoot.GetComponentsInChildren<Station>(true);
        }

        if (startingStation == null && stations != null && stations.Length > 0)
        {
            startingStation = stations[0];
        }

        trainVipHandler.Configure(trainMover.transform, startingStation, false);
        vipSpawnManager.Configure(stations, markerPrefab, markerParent);
        mapStopController.Configure(trainMover, trainVipHandler, startingStation, mapRoot, vipSpawnManager, openMapButton);
    }

    private RectTransform CreatePanel(Transform parent, string name, Vector2 size, Color color)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        Image image = panel.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    private TMP_Text CreateText(Transform parent, string name, string value, int fontSize, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(300f, 30f);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        return text;
    }

    private TMP_Text EnsureHudText(Transform parent, string name, string value, Vector2 anchoredPosition, int fontSize = 22, TextAlignmentOptions alignment = TextAlignmentOptions.Left)
    {
        Transform existing = parent.Find(name);
        TMP_Text text = existing != null ? existing.GetComponent<TMP_Text>() : CreateText(parent, name, value, fontSize, alignment);
        if (text != null)
        {
            text.text = value;
            text.rectTransform.anchorMin = new Vector2(0f, 1f);
            text.rectTransform.anchorMax = new Vector2(0f, 1f);
            text.rectTransform.pivot = new Vector2(0f, 1f);
            text.rectTransform.sizeDelta = new Vector2(480f, 28f);
            text.rectTransform.anchoredPosition = anchoredPosition;
            text.alignment = alignment;
        }

        return text;
    }
}
