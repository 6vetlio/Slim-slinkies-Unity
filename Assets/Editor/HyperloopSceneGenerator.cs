using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using System.Collections.Generic;
using System.IO;

public static class HyperloopSceneGenerator
{
    const string ScenePath = "Assets/Scenes/HyperloopDemo.unity";

    [MenuItem("Tools/Generate Hyperloop Scene (Clean)")]
    public static void GenerateScene()
    {
        ConfigureSpriteImportSettings.ConfigureHyperloopSprites();
        AssetDatabase.Refresh();

        if (File.Exists(ScenePath))
            AssetDatabase.DeleteAsset(ScenePath);

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Camera cam = CreateCamera();
        CreateEventSystem();
        GameObject canvas = CreateCanvas(cam);

        CreateAllParallaxLayers(canvas.transform, cam);

        // Trains container added AFTER parallax layers so it renders on top
        GameObject gameMgr = CreateGameManager();
        GameObject normalTrain, hyperloopTrain;
        GameObject[] allTrains = CreateUITrains(canvas.transform, out normalTrain, out hyperloopTrain);

        WireGameManager(gameMgr, normalTrain, hyperloopTrain);
        WireDevTrainSwitcher(gameMgr, allTrains);

        CreateHUD(canvas.transform);
        TransportSwitcher ts = gameMgr.GetComponent<TransportSwitcher>();
        GameObject upgradePanel = CreateUpgradeMenu(canvas.transform, ts);
        CanvasGroup upgradeCG = upgradePanel.AddComponent<CanvasGroup>();

        // VipMarkers container with CanvasGroup to hide when map opens
        GameObject vipMarkersParent = new GameObject("VipMarkers_Container");
        RectTransform vipMrkRt = vipMarkersParent.AddComponent<RectTransform>();
        vipMrkRt.SetParent(canvas.transform, false);
        Stretch(vipMrkRt);
        CanvasGroup vipMarkersCG = vipMarkersParent.AddComponent<CanvasGroup>();

        InstantiateMapFromPrefab(canvas.transform, gameMgr, vipMarkersCG, upgradeCG, vipMrkRt);
        
        EnsureMapOnTop(canvas.transform);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
        Debug.Log("HyperloopDemo scene saved to " + ScenePath);
    }

    static Camera CreateCamera()
    {
        GameObject go = new GameObject("Main Camera");
        Camera cam = go.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.2f, 0.6f, 0.8f);
        cam.orthographic = true;
        cam.orthographicSize = 175f;
        go.transform.position = new Vector3(0f, 0f, -10f);
        go.tag = "MainCamera";
        go.AddComponent<AudioListener>();
        return cam;
    }

    static GameObject CreateGameManager()
    {
        GameObject go = new GameObject("GameManager");
        go.transform.position = new Vector3(0f, 0f, 0f);

        go.AddComponent<TransportSwitcher>();
        go.AddComponent<GameManager>();
        TrainMover mover = go.AddComponent<TrainMover>();
        mover.speed = 2400f;
        mover.acceleration = 3000f;
        mover.normalTrainDuration = 12f;
        mover.hyperloopDuration = 3f;
        mover.moveCameraForParallax = true;
        mover.cameraTravelDistance = 800f;
        mover.cameraZPosition = -10f;

        GameObject pointA = new GameObject("PointA");
        pointA.transform.SetParent(go.transform);
        pointA.transform.localPosition = new Vector3(-500f, 0f, 0f);

        GameObject pointB = new GameObject("PointB");
        pointB.transform.SetParent(go.transform);
        pointB.transform.localPosition = new Vector3(500f, 0f, 0f);

        mover.pointA = pointA.transform;
        mover.pointB = pointB.transform;

        return go;
    }

    // Trains are UI Images inside the Canvas so they stay on screen while the camera moves for parallax.
    // Rail level  y ≈ +83   (matches Rails parallax layer offset)
    // Tube level  y ≈ +8    (matches Tubes parallax layer offset)
    static GameObject[] CreateUITrains(Transform canvasTransform, out GameObject normalTrain, out GameObject hyperloopTrain)
    {
        GameObject container = new GameObject("Trains");
        RectTransform containerRt = container.AddComponent<RectTransform>();
        containerRt.SetParent(canvasTransform, false);
        containerRt.anchorMin = new Vector2(0.5f, 0.5f);
        containerRt.anchorMax = new Vector2(0.5f, 0.5f);
        containerRt.anchoredPosition = Vector2.zero;
        containerRt.sizeDelta = Vector2.zero;

        // name, spriteName, anchoredPos (pivot bottom-center), sizeDelta, active
        (string name, string sprite, Vector2 pos, Vector2 size, bool active)[] defs = {
            ("Train_Arriva",       "ArrivaAnimation-Sheet",      new Vector2(0, 83), new Vector2(220, 80),  true),
            ("Train_BulletTrain",  "BulletTrainAnimation-Sheet", new Vector2(0, 83), new Vector2(240, 80),  false),
            ("Train_Hyperloop",    "HyperloopTrain-Sheet",       new Vector2(0,  8), new Vector2(280, 56),  false),
            ("Train_DarkHyperloop","DarkHyperloopTrain",         new Vector2(0,  8), new Vector2(280, 56),  false),
        };

        var result = new GameObject[defs.Length];
        for (int i = 0; i < defs.Length; i++)
        {
            var d = defs[i];
            Sprite spr = LoadSprite(d.sprite);
            if (spr == null)
            {
                Sprite[] frames = LoadSpritesOrdered($"Assets/CarParallax/Hyperloop/TrainAnimations/{d.sprite}.png");
                if (frames.Length == 0)
                    frames = LoadSpritesOrdered($"Assets/CarParallax/Hyperloop/{d.sprite}.png");
                if (frames.Length > 0) spr = frames[0];
            }

            GameObject go = new GameObject(d.name);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.SetParent(container.transform, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = d.pos;
            rt.sizeDelta = d.size;

            Image img = go.AddComponent<Image>();
            if (spr != null) img.sprite = spr;
            img.preserveAspect = true;

            go.SetActive(d.active);
            result[i] = go;
        }

        normalTrain = result[0];
        hyperloopTrain = result[2];
        return result;
    }

    static void WireDevTrainSwitcher(GameObject gameMgr, GameObject[] allTrains)
    {
        TransportSwitcher switcher = gameMgr.GetComponent<TransportSwitcher>();
        TrainMover trainMover = gameMgr.GetComponent<TrainMover>();
        DevTrainSwitcher dev = gameMgr.AddComponent<DevTrainSwitcher>();
        SerializedObject so = new SerializedObject(dev);
        SerializedProperty arr = so.FindProperty("trainVisuals");
        arr.arraySize = allTrains.Length;
        for (int i = 0; i < allTrains.Length; i++)
            arr.GetArrayElementAtIndex(i).objectReferenceValue = allTrains[i];
        so.FindProperty("transportSwitcher").objectReferenceValue = switcher;
        so.FindProperty("trainMover").objectReferenceValue = trainMover;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void EnsureMapOnTop(Transform canvas)
    {
        Transform canvasMap = canvas.Find("Canvas_Map");
        if (canvasMap != null)
        {
            canvasMap.SetAsLastSibling();
        }
    }

    static void WireGameManager(GameObject gameMgr, GameObject normalTrain, GameObject hyperloopTrain)
    {
        TransportSwitcher switcher = gameMgr.GetComponent<TransportSwitcher>();
        if (switcher != null)
        {
            switcher.trainImage = normalTrain;
            switcher.hyperloopImage = hyperloopTrain;
        }
    }

    static void CreateEventSystem()
    {
        GameObject go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
    }

    static GameObject CreateCanvas(Camera cam)
    {
        GameObject go = new GameObject("Canvas");
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;

        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1170f, 540f);
        scaler.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    static void CreateAllParallaxLayers(Transform canvas, Camera cam)
    {
        GameObject layers = new GameObject("ParallaxLayers");
        RectTransform rt = layers.AddComponent<RectTransform>();
        rt.SetParent(canvas, false);
        Stretch(rt);

        BakeLayer(layers.transform, cam, "Sky", "Sky", 0f, 80f, 50, new Vector2(0, -33));
        BakeLayer(layers.transform, cam, "Buildings2", "Skyline", 0.13f, 250f, 30, new Vector2(0, -4));
        BakeLayer(layers.transform, cam, "SpaceNeedle", "SpaceNeedle", 0.14f, 1500f, 6, new Vector2(500, -24));
        BakeLayer(layers.transform, cam, "Buildings", "Skyline", 0.15f, 250f, 30, new Vector2(0, 6));
        BakeLayer(layers.transform, cam, "BackHills", "GrassLayer3", 0.2f, 584f, 20, new Vector2(0, 17));
        BakeLayer(layers.transform, cam, "MiddleHills", "GrassLayer2", 0.3f, 608f, 20, new Vector2(0, 18));
        BakeLayer(layers.transform, cam, "FrontHills", "GrassLayer1", 0.55f, 608f, 20, new Vector2(0, 19));
        BakeLayer(layers.transform, cam, "GrassBack", "GrassBack", 0.7f, 909f, 15, new Vector2(0, 35.5f));
        BakeTubes(layers.transform, cam);
        BakeLayer(layers.transform, cam, "Grass", "Grass", 0.85f, 909f, 15, new Vector2(0, 48));
        BakeLayer(layers.transform, cam, "Rails", "Rails", 1f, 303f, 30, new Vector2(0, 83.5f));
    }

    static void BakeLayer(Transform parent, Camera cam, string name, string spriteName, float scroll, float tileW, int copies, Vector2 offset)
    {
        Sprite spr = LoadSprite(spriteName);
        if (spr == null) return;

        GameObject layer = new GameObject(name);
        RectTransform layerRt = layer.AddComponent<RectTransform>();
        layerRt.SetParent(parent, false);
        layerRt.anchoredPosition = Vector2.zero;

        UIParallax parallax = layer.AddComponent<UIParallax>();
        parallax.cameraToFollow = cam;
        parallax.parallaxStrengthX = scroll;
        parallax.scrollMultiplier = 4f;
        parallax.wrapHorizontally = true;
        parallax.tileWidth = tileW * copies;

        float w = spr.rect.width;
        float h = spr.rect.height;
        float startX = -tileW * (copies / 2);

        for (int i = 0; i < copies; i++)
        {
            GameObject tile = new GameObject($"Tile_{i:D2}");
            RectTransform tileRt = tile.AddComponent<RectTransform>();
            tileRt.SetParent(layer.transform, false);
            tileRt.anchoredPosition = new Vector2(startX + i * tileW + offset.x, offset.y);
            tileRt.sizeDelta = new Vector2(w, h);

            Image img = tile.AddComponent<Image>();
            img.sprite = spr;
        }
    }

    static void BakeTubes(Transform parent, Camera cam)
    {
        Sprite back = LoadSprite("Tubes");
        Sprite front = LoadSprite("TubeFrontClear");

        GameObject layer = new GameObject("Tubes");
        RectTransform layerRt = layer.AddComponent<RectTransform>();
        layerRt.SetParent(parent, false);
        layerRt.anchoredPosition = Vector2.zero;

        UIParallax parallax = layer.AddComponent<UIParallax>();
        parallax.cameraToFollow = cam;
        parallax.parallaxStrengthX = 0.7f;
        parallax.scrollMultiplier = 4f;
        parallax.wrapHorizontally = true;

        int copies = 50;
        float tileW = 160f;
        parallax.tileWidth = tileW * copies;

        float startX = -tileW * (copies / 2);
        for (int i = 0; i < copies; i++)
        {
            GameObject tile = new GameObject($"Tile_{i:D2}");
            RectTransform tileRt = tile.AddComponent<RectTransform>();
            tileRt.SetParent(layer.transform, false);
            tileRt.anchoredPosition = new Vector2(startX + i * tileW, 3.5f);
            tileRt.sizeDelta = new Vector2(tileW, 64f);

            if (back != null)
            {
                GameObject b = new GameObject("Back");
                RectTransform bRt = b.AddComponent<RectTransform>();
                bRt.SetParent(tileRt, false);
                bRt.anchoredPosition = Vector2.zero;
                bRt.sizeDelta = new Vector2(back.rect.width, back.rect.height);
                b.AddComponent<Image>().sprite = back;
            }
            if (front != null)
            {
                GameObject f = new GameObject("Front");
                RectTransform fRt = f.AddComponent<RectTransform>();
                fRt.SetParent(tileRt, false);
                fRt.anchoredPosition = Vector2.zero;
                fRt.sizeDelta = new Vector2(front.rect.width, front.rect.height);
                f.AddComponent<Image>().sprite = front;
            }
        }
    }

    static void CreateHUD(Transform canvas)
    {
        GameObject hud = new GameObject("Canvas_HUD");
        RectTransform rt = hud.AddComponent<RectTransform>();
        rt.SetParent(canvas, false);
        Stretch(rt);

        // Info panel – top left
        GameObject infoPanel = new GameObject("InfoPanel");
        RectTransform panelRt = infoPanel.AddComponent<RectTransform>();
        panelRt.SetParent(hud.transform, false);
        panelRt.anchorMin = new Vector2(0, 1);
        panelRt.anchorMax = new Vector2(0, 1);
        panelRt.pivot = new Vector2(0, 1);
        panelRt.anchoredPosition = new Vector2(10, -10);
        panelRt.sizeDelta = new Vector2(310, 170);

        Image bg = infoPanel.AddComponent<Image>();
        bg.color = new Color(0.06f, 0.12f, 0.18f, 0.92f);

        TextMeshProUGUI moneyText  = CreateHUDLabel(infoPanel.transform, "MoneyCounter",     new Vector2(10, -12),  22, "Money: EUR 100",       TextAlignmentOptions.Left);
        TextMeshProUGUI passText   = CreateHUDLabel(infoPanel.transform, "PassengerCounter", new Vector2(10, -44),  16, "Passengers: 25",        TextAlignmentOptions.Left);
        TextMeshProUGUI incomeText = CreateHUDLabel(infoPanel.transform, "IncomeCounter",    new Vector2(10, -70),  14, "Income/s: EUR 12.5",    TextAlignmentOptions.Left);
        TextMeshProUGUI vipText    = CreateHUDLabel(infoPanel.transform, "VipOnboardInfo",   new Vector2(10, -95),  13, "Onboard (0/3): empty",  TextAlignmentOptions.Left);

        // Money popup parent – invisible, sits over the info panel
        GameObject popupParent = new GameObject("MoneyPopups");
        RectTransform popupRt = popupParent.AddComponent<RectTransform>();
        popupRt.SetParent(hud.transform, false);
        popupRt.anchorMin = new Vector2(0, 1);
        popupRt.anchorMax = new Vector2(0, 1);
        popupRt.pivot = new Vector2(0, 1);
        popupRt.anchoredPosition = new Vector2(10, -10);
        popupRt.sizeDelta = new Vector2(310, 170);

        // Popup template text (inactive by default, HudController instantiates it)
        GameObject popupTemplate = new GameObject("PopupTemplate");
        RectTransform popupTplRt = popupTemplate.AddComponent<RectTransform>();
        popupTplRt.SetParent(popupParent.transform, false);
        popupTplRt.anchoredPosition = Vector2.zero;
        popupTplRt.sizeDelta = new Vector2(200, 30);
        TextMeshProUGUI popupTmp = popupTemplate.AddComponent<TextMeshProUGUI>();
        popupTmp.text = "+EUR 0";
        popupTmp.fontSize = 18;
        popupTmp.color = Color.green;
        popupTmp.alignment = TextAlignmentOptions.Left;
        popupTemplate.SetActive(false);

        HudController hudCtrl = hud.AddComponent<HudController>();
        SerializedObject so = new SerializedObject(hudCtrl);
        so.FindProperty("moneyText").objectReferenceValue = moneyText;
        so.FindProperty("passengersText").objectReferenceValue = passText;
        so.FindProperty("incomeText").objectReferenceValue = incomeText;
        so.FindProperty("onboardVipText").objectReferenceValue = vipText;
        so.FindProperty("moneyPopupPrefab").objectReferenceValue = popupTmp;
        so.FindProperty("moneyPopupParent").objectReferenceValue = popupRt;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static GameObject CreateUpgradeMenu(Transform canvas, TransportSwitcher switcher)
    {
        GameObject panel = new GameObject("UpgradePanel");
        RectTransform panelRt = panel.AddComponent<RectTransform>();
        panelRt.SetParent(canvas, false);
        panelRt.anchorMin = new Vector2(0.5f, 0f);
        panelRt.anchorMax = new Vector2(0.5f, 0f);
        panelRt.pivot = new Vector2(0.5f, 0f);
        panelRt.anchoredPosition = new Vector2(0, 20);
        panelRt.sizeDelta = new Vector2(320, 110);

        Image panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0.08f, 0.20f, 0.32f, 0.95f);

        // Title label
        GameObject titleGo = new GameObject("UpgradeTitle");
        RectTransform titleRt = titleGo.AddComponent<RectTransform>();
        titleRt.SetParent(panel.transform, false);
        titleRt.anchorMin = new Vector2(0, 1); titleRt.anchorMax = new Vector2(1, 1);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0, -10);
        titleRt.sizeDelta = new Vector2(0, 28);
        TextMeshProUGUI titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "UPGRADE TO HYPERLOOP";
        titleTmp.fontSize = 16;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.color = Color.white;
        titleTmp.alignment = TextAlignmentOptions.Center;

        // Upgrade button
        GameObject btnGo = new GameObject("UpgradeButton");
        RectTransform btnRt = btnGo.AddComponent<RectTransform>();
        btnRt.SetParent(panel.transform, false);
        btnRt.anchorMin = new Vector2(0.5f, 0.5f); btnRt.anchorMax = new Vector2(0.5f, 0.5f);
        btnRt.pivot = new Vector2(0.5f, 0.5f);
        btnRt.anchoredPosition = new Vector2(0, -15);
        btnRt.sizeDelta = new Vector2(260, 48);

        Image btnImg = btnGo.AddComponent<Image>();
        btnImg.color = new Color(0.15f, 0.55f, 0.30f, 1f);
        Button btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = btnImg;

        GameObject btnTxtGo = new GameObject("Text");
        RectTransform btnTxtRt = btnTxtGo.AddComponent<RectTransform>();
        btnTxtRt.SetParent(btnGo.transform, false);
        Stretch(btnTxtRt);
        TextMeshProUGUI btnTmp = btnTxtGo.AddComponent<TextMeshProUGUI>();
        btnTmp.text = "Upgrade\nEUR 8000";
        btnTmp.fontSize = 15;
        btnTmp.color = Color.white;
        btnTmp.alignment = TextAlignmentOptions.Center;

        UpgradeButtonController ctrl = panel.AddComponent<UpgradeButtonController>();
        SerializedObject so = new SerializedObject(ctrl);
        so.FindProperty("upgradeButton").objectReferenceValue = btnGo;
        so.FindProperty("button").objectReferenceValue = btn;
        so.FindProperty("transportSwitcher").objectReferenceValue = switcher;
        so.ApplyModifiedPropertiesWithoutUndo();

        return panel;
    }

    static TextMeshProUGUI CreateHUDLabel(Transform parent, string name, Vector2 pos, int size, string text, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(-20, 30);

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = Color.white;
        tmp.alignment = align;
        return tmp;
    }

    static void InstantiateMapFromPrefab(Transform canvas, GameObject gameMgr, CanvasGroup vipMarkersCG, CanvasGroup upgradeCG, RectTransform vipMarkersParentRt)
    {
        GameObject canvasMapPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CanvasMap.prefab");
        GameObject mapSystemsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MapSystems.prefab");
        
        Button openMapBtn = CreateOpenMapButton(canvas);
        
        GameObject canvasMap = null;
        if (canvasMapPrefab != null)
        {
            canvasMap = (GameObject)PrefabUtility.InstantiatePrefab(canvasMapPrefab, canvas);
            canvasMap.name = "Canvas_Map";
            
            RectTransform mapRt = canvasMap.GetComponent<RectTransform>();
            if (mapRt != null)
            {
                mapRt.SetParent(canvas, false);
                Stretch(mapRt);
                mapRt.SetAsLastSibling();
            }
            
            CanvasGroup mapCG = canvasMap.GetComponent<CanvasGroup>();
            if (mapCG == null)
                mapCG = canvasMap.AddComponent<CanvasGroup>();
            mapCG.alpha = 0f;
            mapCG.interactable = false;
            mapCG.blocksRaycasts = false;
            
            Canvas mapCanvas = canvasMap.GetComponent<Canvas>();
            if (mapCanvas == null)
                mapCanvas = canvasMap.AddComponent<Canvas>();
            mapCanvas.overrideSorting = true;
            mapCanvas.sortingOrder = 200;
            canvasMap.AddComponent<GraphicRaycaster>();
        }
        else
        {
            canvasMap = CreateFallbackMap(canvas);
        }
        
        GameObject mapSystems = null;
        if (mapSystemsPrefab != null)
        {
            mapSystems = (GameObject)PrefabUtility.InstantiatePrefab(mapSystemsPrefab);
            mapSystems.name = "MapSystems";
            mapSystems.transform.SetParent(canvas.root);
        }
        else
        {
            mapSystems = new GameObject("MapSystems");
            mapSystems.transform.SetParent(canvas.root);
            mapSystems.AddComponent<MapStopController>();
            mapSystems.AddComponent<MapVisualizer>();
            mapSystems.AddComponent<VipSpawnManager>();
        }
        
        TrainMover trainMover = gameMgr.GetComponent<TrainMover>();
        TrainVipHandler vipHandler = gameMgr.GetComponent<TrainVipHandler>();
        if (vipHandler == null)
            vipHandler = gameMgr.AddComponent<TrainVipHandler>();
        
        MapStopController mapCtrl = mapSystems.GetComponent<MapStopController>();
        MapVisualizer mapVis = mapSystems.GetComponent<MapVisualizer>();
        VipSpawnManager vipSpawn = mapSystems.GetComponent<VipSpawnManager>();
        
        Station[] stations = canvasMap.GetComponentsInChildren<Station>(true);

        var initialUnlockStates = new System.Collections.Generic.Dictionary<string, bool>
        {
            { "station_groningen", true },
            { "station_amsterdam", true },
            { "station_brussels", false },
            { "station_paris", false },
            { "station_berlin", false },
            { "station_hamburg", false },
            { "station_hannover", false },
        };
        Station firstUnlocked = null;
        foreach (Station s in stations)
        {
            if (s == null) continue;
            if (initialUnlockStates.TryGetValue(s.StationId, out bool unlocked))
            {
                SerializedObject stSo = new SerializedObject(s);
                stSo.FindProperty("isUnlocked").boolValue = unlocked;
                stSo.FindProperty("unlockCost").intValue = unlocked ? 0 : s.UnlockCost;
                stSo.ApplyModifiedPropertiesWithoutUndo();
                if (unlocked && firstUnlocked == null) firstUnlocked = s;
            }
        }
        Station startStation = firstUnlocked ?? (stations.Length > 0 ? stations[0] : null);
        
        if (mapCtrl != null)
        {
            SerializedObject so = new SerializedObject(mapCtrl);
            so.FindProperty("trainMover").objectReferenceValue = trainMover;
            so.FindProperty("trainVipHandler").objectReferenceValue = vipHandler;
            so.FindProperty("mapRoot").objectReferenceValue = canvasMap;
            so.FindProperty("openMapButton").objectReferenceValue = openMapBtn;
            so.FindProperty("vipSpawnManager").objectReferenceValue = vipSpawn;
            so.FindProperty("mapVisualizer").objectReferenceValue = mapVis;
            if (startStation != null)
                so.FindProperty("startingStation").objectReferenceValue = startStation;

            // Wire elements to hide when map opens (VIP markers + upgrade panel, NOT trains)
            SerializedProperty hideArr = so.FindProperty("gameplayElementsToHide");
            int count = 0;
            if (vipMarkersCG != null) count++;
            if (upgradeCG != null) count++;
            hideArr.arraySize = count;
            int idx = 0;
            if (vipMarkersCG != null)
                hideArr.GetArrayElementAtIndex(idx++).objectReferenceValue = vipMarkersCG;
            if (upgradeCG != null)
                hideArr.GetArrayElementAtIndex(idx++).objectReferenceValue = upgradeCG;

            so.ApplyModifiedPropertiesWithoutUndo();
        }
        
        if (vipHandler != null && startStation != null)
        {
            SerializedObject vipSO = new SerializedObject(vipHandler);
            vipSO.FindProperty("trainTransform").objectReferenceValue = trainMover.transform;
            vipSO.FindProperty("startingStation").objectReferenceValue = startStation;
            vipSO.ApplyModifiedPropertiesWithoutUndo();
        }
        
        VipMarkerUI markerPrefabAsset = AssetDatabase.LoadAssetAtPath<VipMarkerUI>("Assets/Prefabs/VipMarkerUI.prefab");

        if (vipSpawn != null)
        {
            SerializedObject spawnSO = new SerializedObject(vipSpawn);
            SerializedProperty stationsProp = spawnSO.FindProperty("stations");
            if (stationsProp != null && stations.Length > 0)
            {
                stationsProp.arraySize = stations.Length;
                for (int i = 0; i < stations.Length; i++)
                    stationsProp.GetArrayElementAtIndex(i).objectReferenceValue = stations[i];
            }
            if (markerPrefabAsset != null)
                spawnSO.FindProperty("markerPrefab").objectReferenceValue = markerPrefabAsset;
            spawnSO.FindProperty("markerParent").objectReferenceValue = vipMarkersParentRt;

            if (markerPrefabAsset != null)
            {
                int poolSize = 6;
                VipMarkerUI[] pool = new VipMarkerUI[poolSize];
                for (int i = 0; i < poolSize; i++)
                {
                    VipMarkerUI marker = (VipMarkerUI)PrefabUtility.InstantiatePrefab(markerPrefabAsset, vipMarkersParentRt);
                    marker.gameObject.SetActive(false);
                    pool[i] = marker;
                }
                SerializedProperty poolProp = spawnSO.FindProperty("markerPool");
                poolProp.arraySize = poolSize;
                for (int i = 0; i < poolSize; i++)
                    poolProp.GetArrayElementAtIndex(i).objectReferenceValue = pool[i];
            }

            spawnSO.ApplyModifiedPropertiesWithoutUndo();
        }

        Transform stationsContainer = canvasMap.transform.Find("Stations");
        if (mapVis != null)
        {
            SerializedObject visSO = new SerializedObject(mapVis);
            if (stationsContainer != null)
                visSO.FindProperty("stationsContainer").objectReferenceValue = stationsContainer.gameObject;
            visSO.FindProperty("trainMover").objectReferenceValue = trainMover;
            visSO.FindProperty("trainVipHandler").objectReferenceValue = vipHandler;
            visSO.FindProperty("vipSpawnManager").objectReferenceValue = vipSpawn;
            visSO.ApplyModifiedPropertiesWithoutUndo();
        }

        if (mapVis != null && stationsContainer != null)
        {
            GameObject trainDot = new GameObject("TrainDot");
            RectTransform dotRt = trainDot.AddComponent<RectTransform>();
            dotRt.SetParent(stationsContainer, false);
            dotRt.sizeDelta = new Vector2(16f, 16f);
            Image dotImg = trainDot.AddComponent<Image>();
            dotImg.color = Color.yellow;
            trainDot.transform.SetAsLastSibling();
            trainDot.SetActive(false);

            SerializedObject visSO2 = new SerializedObject(mapVis);
            visSO2.FindProperty("trainDotObject").objectReferenceValue = trainDot;
            visSO2.ApplyModifiedPropertiesWithoutUndo();
        }

        Debug.Log("Map setup complete with " + stations.Length + " stations!");
    }

    static Button CreateOpenMapButton(Transform canvas)
    {
        GameObject btn = new GameObject("OpenMapButton");
        RectTransform rt = btn.AddComponent<RectTransform>();
        rt.SetParent(canvas, false);
        rt.anchorMin = rt.anchorMax = new Vector2(1, 0);
        rt.pivot = new Vector2(1, 0);
        rt.anchoredPosition = new Vector2(-20, 20);
        rt.sizeDelta = new Vector2(120, 40);

        Image img = btn.AddComponent<Image>();
        img.color = new Color(0.2f, 0.6f, 0.7f, 1f);
        Button btnComp = btn.AddComponent<Button>();
        btnComp.targetGraphic = img;

        GameObject txt = new GameObject("Text");
        RectTransform txtRt = txt.AddComponent<RectTransform>();
        txtRt.SetParent(btn.transform, false);
        Stretch(txtRt);

        TextMeshProUGUI tmp = txt.AddComponent<TextMeshProUGUI>();
        tmp.text = "Open Map";
        tmp.fontSize = 18;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        
        return btnComp;
    }

    static GameObject CreateFallbackMap(Transform canvas)
    {
        GameObject mapRoot = new GameObject("Canvas_Map");
        RectTransform mapRt = mapRoot.AddComponent<RectTransform>();
        mapRt.SetParent(canvas, false);
        Stretch(mapRt);
        mapRt.SetAsLastSibling();
        
        Image mapBg = mapRoot.AddComponent<Image>();
        mapBg.color = new Color(0.1f, 0.2f, 0.3f, 0.95f);
        CanvasGroup mapCG = mapRoot.AddComponent<CanvasGroup>();
        mapCG.alpha = 0f;
        mapCG.interactable = false;
        mapCG.blocksRaycasts = false;
        
        Canvas mapCanvas = mapRoot.AddComponent<Canvas>();
        mapCanvas.overrideSorting = true;
        mapCanvas.sortingOrder = 200;
        mapRoot.AddComponent<GraphicRaycaster>();

        GameObject mapTitle = new GameObject("MapTitle");
        RectTransform titleRt = mapTitle.AddComponent<RectTransform>();
        titleRt.SetParent(mapRoot.transform, false);
        titleRt.anchorMin = new Vector2(0.5f, 1f);
        titleRt.anchorMax = new Vector2(0.5f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0, -20);
        titleRt.sizeDelta = new Vector2(400, 50);
        
        TextMeshProUGUI titleTmp = mapTitle.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "SELECT DESTINATION";
        titleTmp.fontSize = 32;
        titleTmp.color = Color.white;
        titleTmp.alignment = TextAlignmentOptions.Center;

        GameObject stationsContainer = new GameObject("Stations");
        RectTransform stationsRt = stationsContainer.AddComponent<RectTransform>();
        stationsRt.SetParent(mapRoot.transform, false);
        stationsRt.anchoredPosition = Vector2.zero;
        stationsRt.sizeDelta = new Vector2(800, 400);
        
        CreateFallbackStation(stationsContainer.transform, "Groningen", "station_groningen", new Vector2(-30, 120), true, 0);
        CreateFallbackStation(stationsContainer.transform, "Amsterdam", "station_amsterdam", new Vector2(-90, 80), true, 0);
        CreateFallbackStation(stationsContainer.transform, "Brussels", "station_brussels", new Vector2(-190, 0), false, 500);
        CreateFallbackStation(stationsContainer.transform, "Paris", "station_paris", new Vector2(-250, -90), false, 1000);
        CreateFallbackStation(stationsContainer.transform, "Berlin", "station_berlin", new Vector2(100, 60), false, 1500);
        CreateFallbackStation(stationsContainer.transform, "Hamburg", "station_hamburg", new Vector2(150, 100), false, 2000);
        CreateFallbackStation(stationsContainer.transform, "Hannover", "station_hannover", new Vector2(120, 0), false, 2500);
        
        return mapRoot;
    }

    static void CreateFallbackStation(Transform parent, string displayName, string stationId, Vector2 pos, bool unlocked, int cost)
    {
        GameObject station = new GameObject($"Station_{displayName}");
        RectTransform rt = station.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(120, 44);
        
        Image img = station.AddComponent<Image>();
        img.color = unlocked ? new Color(0.1f, 0.45f, 0.6f, 1f) : new Color(0.3f, 0.3f, 0.3f, 1f);
        
        Button btn = station.AddComponent<Button>();
        btn.targetGraphic = img;
        
        Station stationComp = station.AddComponent<Station>();
        SerializedObject so = new SerializedObject(stationComp);
        so.FindProperty("stationId").stringValue = stationId;
        so.FindProperty("displayName").stringValue = displayName;
        so.FindProperty("isUnlocked").boolValue = unlocked;
        so.FindProperty("unlockCost").intValue = cost;
        so.ApplyModifiedPropertiesWithoutUndo();
        
        GameObject txt = new GameObject("StationNameText");
        RectTransform txtRt = txt.AddComponent<RectTransform>();
        txtRt.SetParent(station.transform, false);
        Stretch(txtRt);
        
        TextMeshProUGUI tmp = txt.AddComponent<TextMeshProUGUI>();
        tmp.text = displayName;
        tmp.fontSize = 14;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static Sprite LoadSprite(string name)
    {
        string[] paths = {
            $"Assets/CarParallax/Hyperloop/{name}.png",
            $"Assets/CarParallax/Hyperloop/UI/{name}.png",
            $"Assets/CarParallax/Hyperloop/TrainAnimations/{name}.png"
        };
        foreach (string path in paths)
        {
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s != null) return s;
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(path))
                if (o is Sprite sp) return sp;
        }
        return null;
    }

    static Sprite[] LoadSpritesOrdered(string path)
    {
        List<Sprite> list = new List<Sprite>();
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(path))
            if (o is Sprite sp) list.Add(sp);
        list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return list.ToArray();
    }
}
