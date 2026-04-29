using System.Collections.Generic;
using UnityEngine;

public class VipSpawnManager : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private Station[] stations;
    [SerializeField] private VipMarkerUI markerPrefab;
    [SerializeField] private Transform markerParent;

    [Header("VIP Profiles")]
    [SerializeField] private VipProfile[] vipProfiles;
    [SerializeField] private string[] fallbackNames =
    {
        "Ari Chen",
        "Maya Vale",
        "Noah Singh",
        "Lena Brooks",
        "Jonas Reed"
    };

    [Header("Spawn Pacing")]
    [SerializeField] private float firstSpawnDelay = 1.5f;
    [SerializeField] private float minSpawnInterval = 8f;
    [SerializeField] private float maxSpawnInterval = 14f;
    [SerializeField] private int normalTargetVipCount = 2;
    [SerializeField] private float chanceToAllowThirdVip = 0.15f;

    [Header("VIP Rules")]
    [SerializeField] private float pickupSeconds = 60f;
    [SerializeField] private float deliverySeconds = 90f;
    [SerializeField] private int deliveryReward = 350;
    [SerializeField] private int missedPickupPenalty = 100;
    [SerializeField] private int missedDeliveryPenalty = 250;

    private readonly Dictionary<string, VipMarkerUI> markersByVipId = new Dictionary<string, VipMarkerUI>();
    private float spawnTimer;
    private bool subscribedToGameManager;

    public void Configure(Station[] newStations, VipMarkerUI newMarkerPrefab, Transform newMarkerParent)
    {
        if (stations == null || stations.Length == 0)
        {
            stations = newStations;
        }
        if (markerPrefab == null)
        {
            markerPrefab = newMarkerPrefab;
        }
        if (markerParent == null)
        {
            markerParent = newMarkerParent;
        }
        TrySubscribeToGameManager();
        RebuildMarkersForWaitingVips();
    }

    private void Start()
    {
        TrySubscribeToGameManager();
        RefreshStationsIfNeeded();
        RebuildMarkersForWaitingVips();
        spawnTimer = firstSpawnDelay;
    }

    private void OnEnable()
    {
        TrySubscribeToGameManager();
        if (spawnTimer <= 0f)
        {
            spawnTimer = firstSpawnDelay;
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null && subscribedToGameManager)
        {
            GameManager.Instance.VipsChanged -= HandleVipsChanged;
        }

        subscribedToGameManager = false;
    }

    private void Update()
    {
        TrySubscribeToGameManager();

        if (GameManager.Instance == null || stations == null || stations.Length < 2)
        {
            return;
        }

        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f)
        {
            return;
        }

        spawnTimer = Random.Range(minSpawnInterval, maxSpawnInterval);

        if (GameManager.Instance.WaitingVips.Count < GetTargetVipCount())
        {
            SpawnVip();
        }
    }

    public void SpawnVip()
    {
        RefreshStationsIfNeeded();

        if (GameManager.Instance == null || stations.Length < 2)
        {
            return;
        }

        List<Station> originCandidates = GetOriginCandidates();
        if (originCandidates.Count == 0)
        {
            return;
        }

        Station origin = originCandidates[Random.Range(0, originCandidates.Count)];
        Station destination = GetDifferentStation(origin);
        VipProfile profile = GetRandomProfile();

        string passengerName = profile != null ? profile.passengerName : GetFallbackName();
        Sprite portrait = profile != null ? profile.portrait : null;

        VipPassenger vip = new VipPassenger(
            passengerName,
            portrait,
            origin,
            destination,
            pickupSeconds,
            deliverySeconds,
            deliveryReward,
            missedPickupPenalty,
            missedDeliveryPenalty);

        GameManager.Instance.AddWaitingVip(vip);
        CreateMarker(vip, origin);
    }

    private int GetTargetVipCount()
    {
        if (Random.value < chanceToAllowThirdVip)
        {
            return Mathf.Max(3, normalTargetVipCount);
        }

        return normalTargetVipCount;
    }

    private List<Station> GetOriginCandidates()
    {
        List<Station> candidates = new List<Station>();
        string currentStationId = GameManager.Instance.CurrentStationId;

        for (int i = 0; i < stations.Length; i++)
        {
            Station station = stations[i];
            if (station == null)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(currentStationId) && station.StationId == currentStationId)
            {
                continue;
            }

            if (GameManager.Instance.HasWaitingVipAtStation(station.StationId))
            {
                continue;
            }

            candidates.Add(station);
        }

        return candidates;
    }

    private Station GetDifferentStation(Station origin)
    {
        List<Station> destinationCandidates = new List<Station>();

        for (int i = 0; i < stations.Length; i++)
        {
            if (stations[i] != null && stations[i] != origin)
            {
                destinationCandidates.Add(stations[i]);
            }
        }

        return destinationCandidates[Random.Range(0, destinationCandidates.Count)];
    }

    private VipProfile GetRandomProfile()
    {
        if (vipProfiles == null || vipProfiles.Length == 0)
        {
            return null;
        }

        return vipProfiles[Random.Range(0, vipProfiles.Length)];
    }

    private string GetFallbackName()
    {
        if (fallbackNames == null || fallbackNames.Length == 0)
        {
            return "VIP Passenger";
        }

        return fallbackNames[Random.Range(0, fallbackNames.Length)];
    }

    private void HandleVipsChanged()
    {
        RebuildMarkersForWaitingVips();
    }

    private void RebuildMarkersForWaitingVips()
    {
        if (GameManager.Instance == null || markerPrefab == null)
        {
            return;
        }

        List<string> staleIds = new List<string>(markersByVipId.Keys);

        foreach (VipPassenger vip in GameManager.Instance.WaitingVips)
        {
            staleIds.Remove(vip.Id);

            if (!markersByVipId.ContainsKey(vip.Id))
            {
                Station origin = FindStation(vip.OriginStationId);
                if (origin != null)
                {
                    CreateMarker(vip, origin);
                }
            }
        }

        for (int i = 0; i < staleIds.Count; i++)
        {
            VipMarkerUI marker = markersByVipId[staleIds[i]];
            markersByVipId.Remove(staleIds[i]);

            if (marker != null)
            {
                Destroy(marker.gameObject);
            }
        }
    }

    private void CreateMarker(VipPassenger vip, Station origin)
    {
        if (markerPrefab == null || markersByVipId.ContainsKey(vip.Id))
        {
            return;
        }

        Transform parent = markerParent != null ? markerParent : transform;
        VipMarkerUI marker = Instantiate(markerPrefab, parent);
        marker.gameObject.SetActive(true);
        marker.Bind(vip, origin);
        markersByVipId.Add(vip.Id, marker);
    }

    private Station FindStation(string stationId)
    {
        for (int i = 0; i < stations.Length; i++)
        {
            if (stations[i] != null && stations[i].StationId == stationId)
            {
                return stations[i];
            }
        }

        return null;
    }

    private void RefreshStationsIfNeeded()
    {
        if (stations != null && stations.Length > 0)
        {
            return;
        }

        stations = FindObjectsByType<Station>(FindObjectsSortMode.None);
    }

    private void TrySubscribeToGameManager()
    {
        if (GameManager.Instance == null || subscribedToGameManager)
        {
            return;
        }

        GameManager.Instance.VipsChanged += HandleVipsChanged;
        subscribedToGameManager = true;
    }
}
