using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapVisualizer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject stationsContainer;
    [SerializeField] private TrainMover trainMover;
    [SerializeField] private TrainVipHandler trainVipHandler;
    [SerializeField] private VipSpawnManager vipSpawnManager;
    [SerializeField] private GameObject trainDotObject;

    [Header("Route Lines")]
    [SerializeField] private RouteLineUI[] routeLineObjects;

    [Header("Colors")]
    [SerializeField] private Color colorUnlocked = new Color(0.10f, 0.45f, 0.60f, 1f);
    [SerializeField] private Color colorLocked   = new Color(0.20f, 0.20f, 0.22f, 1f);
    [SerializeField] private Color colorCurrent  = new Color(0.15f, 0.70f, 0.30f, 1f);
    [SerializeField] private Color colorPassed   = new Color(0.16f, 0.22f, 0.26f, 1f);

    private readonly Dictionary<string, Station>       stationById   = new Dictionary<string, Station>();
    private readonly Dictionary<string, RectTransform> stationRects  = new Dictionary<string, RectTransform>();
    private readonly Dictionary<string, Image>         stationImages = new Dictionary<string, Image>();
    private readonly Dictionary<string, TMP_Text>      costLabels    = new Dictionary<string, TMP_Text>();

    private RectTransform trainDotRect;

    private void Start()
    {
        if (trainMover == null)
            trainMover = FindFirstObjectByType<TrainMover>();

        if (trainVipHandler == null)
            trainVipHandler = FindFirstObjectByType<TrainVipHandler>();

        if (vipSpawnManager == null)
            vipSpawnManager = FindFirstObjectByType<VipSpawnManager>();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.StationUnlocked += OnStationUnlocked;
            GameManager.Instance.VipsChanged += OnCurrentStationChanged;
        }

        ResolveSceneReferences();

        // Position route lines immediately so they don't sit stacked at origin
        if (stationsContainer != null)
            BuildNetwork();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StationUnlocked -= OnStationUnlocked;
            GameManager.Instance.VipsChanged -= OnCurrentStationChanged;
        }
    }

    private void Update()
    {
        if (trainDotRect != null)
            UpdateTrainDot();
    }

    public void ShowMapElements()
    {
        ResolveSceneReferences();
        BuildNetwork();
        if (trainDotObject != null)
        {
            trainDotRect = trainDotObject.GetComponent<RectTransform>();
            trainDotObject.SetActive(true);
            PlaceDotAtCurrentStation();
        }
    }

    public void HideMapElements()
    {
        if (trainDotObject != null)
        {
            trainDotObject.SetActive(false);
            trainDotRect = null;
        }
    }

    private void BuildNetwork()
    {
        ResolveSceneReferences();
        if (stationsContainer == null) return;

        stationById.Clear();
        stationRects.Clear();
        stationImages.Clear();
        costLabels.Clear();

        var allStations = new List<Station>();
        var discovered  = stationsContainer.GetComponentsInChildren<Station>(true);

        foreach (var station in discovered)
        {
            string id = station.StationId;
            stationById[id]   = station;
            stationRects[id]  = station.GetComponent<RectTransform>();
            stationImages[id] = station.GetComponent<Image>() ?? station.GetComponentInChildren<Image>();
            allStations.Add(station);
            EnsureCostLabel(id, station, stationRects[id]);
            ApplyStationVisual(id);
        }

        if (vipSpawnManager != null)
            vipSpawnManager.SetStations(allStations.ToArray());

        UpdateRouteLinePositions();
        MoveConnectionsBehindButtons();
    }

    public void SyncRoutesNow()
    {
        ResolveSceneReferences();
        if (stationsContainer != null)
            BuildNetwork();
    }

    private void ResolveSceneReferences()
    {
        if (stationsContainer == null)
        {
            stationsContainer = transform.Find("Stations")?.gameObject;
        }

        if (stationsContainer == null)
        {
            var mapStop = GetComponent<MapStopController>();
            GameObject root = mapStop != null ? mapStop.MapRoot : null;
            if (root != null)
            {
                Transform stations = root.transform.Find("Stations");
                stationsContainer = stations != null ? stations.gameObject : root;
            }
        }

        if ((routeLineObjects == null || routeLineObjects.Length == 0) && stationsContainer != null)
        {
            routeLineObjects = stationsContainer.GetComponentsInChildren<RouteLineUI>(true);
        }

        if (trainDotObject == null && stationsContainer != null)
        {
            Transform trainDot = stationsContainer.transform.Find("TrainDot");
            if (trainDot != null)
            {
                trainDotObject = trainDot.gameObject;
            }
        }
    }

    private void UpdateRouteLinePositions()
    {
        if (routeLineObjects == null) return;

        foreach (var routeLine in routeLineObjects)
        {
            if (routeLine == null) continue;

            if (!stationRects.ContainsKey(routeLine.FromStationId) ||
                !stationRects.ContainsKey(routeLine.ToStationId)) continue;

            Vector2 posFrom = stationRects[routeLine.FromStationId].anchoredPosition;
            Vector2 posTo   = stationRects[routeLine.ToStationId].anchoredPosition;

            bool isUnlocked = stationById.TryGetValue(routeLine.FromStationId, out var stA) && stA.IsUnlocked &&
                              stationById.TryGetValue(routeLine.ToStationId, out var stB) && stB.IsUnlocked;

            routeLine.UpdateVisuals(posFrom, posTo, isUnlocked);
        }
    }

    private void MoveConnectionsBehindButtons()
    {
        int index = 0;
        foreach (Transform child in stationsContainer.transform)
        {
            if (child.GetComponent<RouteLineUI>() != null || child.name.StartsWith("Route_"))
                child.SetSiblingIndex(index++);
        }
    }

    private void PlaceDotAtCurrentStation()
    {
        if (trainDotRect == null) return;
        string curId = trainVipHandler?.CurrentStation?.StationId;
        if (curId != null && stationRects.ContainsKey(curId))
            trainDotRect.anchoredPosition = stationRects[curId].anchoredPosition;
    }

    private void UpdateTrainDot()
    {
        if (trainMover == null || trainDotRect == null) return;

        string fromId = trainVipHandler?.CurrentStation?.StationId;
        string toId   = GetTravelTargetId();

        if (routeLineObjects != null)
        {
            foreach (var routeLine in routeLineObjects)
            {
                if (routeLine != null)
                    routeLine.ClearFill();
            }
        }

        if (trainMover.MovementStatus != TrainMovementStatus.Travelling || toId == null || fromId == null)
        {
            PlaceDotAtCurrentStation();
            return;
        }

        float progress = trainMover.TravelProgress;

        if (!stationRects.ContainsKey(fromId) || !stationRects.ContainsKey(toId)) return;

        Vector2 posFrom = stationRects[fromId].anchoredPosition;
        Vector2 posTo   = stationRects[toId].anchoredPosition;

        trainDotRect.anchoredPosition = Vector2.Lerp(posFrom, posTo, progress);

        if (routeLineObjects != null)
        {
            foreach (var routeLine in routeLineObjects)
            {
                if (routeLine == null) continue;
                bool matches = (routeLine.FromStationId == fromId && routeLine.ToStationId == toId) ||
                               (routeLine.FromStationId == toId && routeLine.ToStationId == fromId);
                if (!matches) continue;

                bool flip = (routeLine.FromStationId == toId);
                routeLine.SetFillAmount(progress, flip);
                break;
            }
        }
    }

    private string GetTravelTargetId()
    {
        Transform target = trainMover?.TravelTargetTransform;
        if (target == null) return null;

        foreach (var kv in stationById)
        {
            if (kv.Value != null && kv.Value.transform == target)
                return kv.Key;
        }

        return null;
    }

    private void ApplyStationVisual(string id)
    {
        if (!stationById.ContainsKey(id)) return;

        Station station   = stationById[id];
        bool    isCurrent = trainVipHandler?.CurrentStation?.StationId == id;

        if (stationImages.TryGetValue(id, out var img) && img != null)
        {
            if (isCurrent)
                img.color = colorCurrent;
            else if (!station.IsUnlocked)
                img.color = colorLocked;
            else
                img.color = colorUnlocked;
        }

        if (costLabels.TryGetValue(id, out var label) && label != null)
        {
            label.gameObject.SetActive(!station.IsUnlocked);
            if (!station.IsUnlocked)
                label.text = "EUR " + station.UnlockCost;
        }
    }

    private void OnStationUnlocked(Station station)
    {
        ApplyStationVisual(station.StationId);
        UpdateRouteLinePositions();
        if (vipSpawnManager != null)
            vipSpawnManager.SetStations(new List<Station>(stationById.Values).ToArray());
    }

    private void OnCurrentStationChanged()
    {
        // Update all station visuals when current station changes
        foreach (var id in stationById.Keys)
        {
            ApplyStationVisual(id);
        }
    }

    private void EnsureCostLabel(string id, Station station, RectTransform parentRT)
    {
        if (parentRT == null) return;

        TMP_Text existing = null;
        foreach (Transform child in parentRT)
        {
            if (child.name == "CostLabel")
            {
                existing = child.GetComponent<TMP_Text>();
                break;
            }
        }

        costLabels[id] = existing;
        if (existing != null)
        {
            existing.gameObject.SetActive(!station.IsUnlocked);
            if (!station.IsUnlocked)
                existing.text = "EUR " + station.UnlockCost;
        }
    }
}
