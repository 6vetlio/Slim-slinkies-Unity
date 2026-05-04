using System.Collections.Generic;
using UnityEngine;

public class VipSpawnManager : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private Station[] stations;
    [SerializeField] private VipMarkerUI markerPrefab;
    [SerializeField] private Transform markerParent;
    [SerializeField] private VipMarkerUI[] markerPool;

    [Header("VIP Profiles")]
    [SerializeField] private VipProfile[] vipProfiles;

    [Header("Name Generation")]
    [SerializeField] private string[] maleFirstNames =
    {
        "Noah", "Liam", "Oliver", "James", "Elijah", "Lucas", "Mason", "Logan", "Alexander"
    };
    [SerializeField] private string[] femaleFirstNames =
    {
        "Emma", "Olivia", "Ava", "Isabella", "Sophia", "Mia", "Charlotte", "Amelia", "Harper"
    };
    [SerializeField] private string[] lastNames =
    {
        "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez",
        "Martinez", "Wilson", "Anderson", "Thomas", "Taylor", "Moore", "Jackson", "Lee", "Perez"
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
    [SerializeField] private int baseDeliveryReward = 250;
    [SerializeField] private int rewardPerStationDistance = 100;
    [SerializeField] private int missedPickupPenalty = 100;
    [SerializeField] private int missedDeliveryPenalty = 250;

    private static readonly string[] ExcludedStationIds = { "point_a", "point_b" };

    private readonly Dictionary<string, VipMarkerUI> markersByVipId = new Dictionary<string, VipMarkerUI>();
    private readonly List<VipMarkerUI> availableMarkers = new List<VipMarkerUI>();
    private float spawnTimer;
    private bool subscribedToGameManager;

    public void SetStations(Station[] newStations)
    {
        stations = newStations;
    }

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
        InitializeMarkerPool();
        PopulateMapStationsFromScene();
        TrySubscribeToGameManager();
        RefreshStationsIfNeeded();
        RebuildMarkersForWaitingVips();
        spawnTimer = firstSpawnDelay;
    }

    private void InitializeMarkerPool()
    {
        availableMarkers.Clear();
        if (markerPool == null) return;
        foreach (VipMarkerUI marker in markerPool)
        {
            if (marker == null) continue;
            marker.gameObject.SetActive(false);
            availableMarkers.Add(marker);
        }
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

        bool isMale;
        string passengerName;
        Sprite portrait;

        if (profile != null)
        {
            passengerName = profile.passengerName;
            isMale = profile.isMale;
            portrait = profile.portrait;
        }
        else
        {
            passengerName = GetFallbackName(out isMale);
            portrait = null;
        }

        // Calculate reward based on station distance
        int stationDistance = CalculateStationDistance(origin, destination);
        int scaledReward = baseDeliveryReward + (stationDistance * rewardPerStationDistance);

        VipPassenger vip = new VipPassenger(
            passengerName,
            isMale,
            portrait,
            origin,
            destination,
            pickupSeconds,
            deliverySeconds,
            scaledReward,
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
            if (station == null || !station.IsUnlocked)
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
            if (stations[i] != null && stations[i] != origin && stations[i].IsUnlocked)
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

    private string GetFallbackName(out bool isMale)
    {
        if ((maleFirstNames == null || maleFirstNames.Length == 0) &&
            (femaleFirstNames == null || femaleFirstNames.Length == 0))
        {
            isMale = true;
            return "VIP Passenger";
        }

        isMale = Random.value > 0.5f;
        string[] firstNames = isMale ? maleFirstNames : femaleFirstNames;

        if (firstNames == null || firstNames.Length == 0)
        {
            isMale = !isMale;
            firstNames = isMale ? maleFirstNames : femaleFirstNames;
        }

        string firstName = firstNames[Random.Range(0, firstNames.Length)];

        if (lastNames != null && lastNames.Length > 0)
        {
            string lastName = lastNames[Random.Range(0, lastNames.Length)];
            return firstName + " " + lastName;
        }

        return firstName;
    }

    private void HandleVipsChanged()
    {
        RebuildMarkersForWaitingVips();
    }

    private void RebuildMarkersForWaitingVips()
    {
        if (GameManager.Instance == null)
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
                if (markerPool != null && markerPool.Length > 0)
                {
                    ReturnMarkerToPool(marker);
                }
                else
                {
                    Destroy(marker.gameObject);
                }
            }
        }
    }

    private void CreateMarker(VipPassenger vip, Station origin)
    {
        if (markersByVipId.ContainsKey(vip.Id))
        {
            return;
        }

        VipMarkerUI marker = GetMarkerFromPool();
        if (marker == null && markerPrefab != null)
        {
            Transform parent = markerParent != null ? markerParent : transform;
            marker = Instantiate(markerPrefab, parent);
        }
        if (marker == null)
        {
            return;
        }

        marker.gameObject.SetActive(true);
        marker.Bind(vip, origin);
        markersByVipId.Add(vip.Id, marker);
    }

    private VipMarkerUI GetMarkerFromPool()
    {
        for (int i = availableMarkers.Count - 1; i >= 0; i--)
        {
            VipMarkerUI marker = availableMarkers[i];
            if (marker != null)
            {
                availableMarkers.RemoveAt(i);
                return marker;
            }
            availableMarkers.RemoveAt(i);
        }
        return null;
    }

    private void ReturnMarkerToPool(VipMarkerUI marker)
    {
        if (marker == null) return;
        marker.gameObject.SetActive(false);
        if (!availableMarkers.Contains(marker))
        {
            availableMarkers.Add(marker);
        }
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

    private void PopulateMapStationsFromScene()
    {
        Station[] found = FindObjectsByType<Station>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (found == null || found.Length == 0)
        {
            return;
        }

        List<Station> list = new List<Station>();
        for (int i = 0; i < found.Length; i++)
        {
            Station s = found[i];
            if (s == null)
            {
                continue;
            }

            string id = s.StationId;
            if (string.IsNullOrEmpty(id))
            {
                continue;
            }

            bool skip = false;
            for (int j = 0; j < ExcludedStationIds.Length; j++)
            {
                if (id == ExcludedStationIds[j])
                {
                    skip = true;
                    break;
                }
            }

            if (skip)
            {
                continue;
            }

            list.Add(s);
        }

        if (list.Count >= 2)
        {
            stations = list.ToArray();
        }
    }

    private void RefreshStationsIfNeeded()
    {
        if (stations != null && stations.Length > 0)
        {
            return;
        }

        stations = FindObjectsByType<Station>(FindObjectsInactive.Include, FindObjectsSortMode.None);
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

    private int CalculateStationDistance(Station from, Station to)
    {
        // Simple distance calculation based on station order
        // For now, using a rough estimate. Could be improved with actual route graph.
        string[] stationOrder = { "groningen", "amsterdam", "brussels", "hamburg", "paris", "hannover", "berlin" };
        
        int fromIndex = System.Array.IndexOf(stationOrder, from.StationId.ToLower());
        int toIndex = System.Array.IndexOf(stationOrder, to.StationId.ToLower());
        
        if (fromIndex == -1 || toIndex == -1)
        {
            return 1; // Default distance if station not found
        }
        
        return Mathf.Abs(toIndex - fromIndex);
    }
}
