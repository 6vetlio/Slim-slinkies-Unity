using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class Station : MonoBehaviour, IPointerClickHandler
{
    public static event Action<Station> Clicked;

    [SerializeField] private string stationId = "station_id";
    [SerializeField] private string displayName = "Station";
    [SerializeField] private Transform markerAnchor;
    [SerializeField] private TrainVipHandler trainHandlerToNotify;

    [Header("Unlock")]
    [SerializeField] private bool isUnlocked = true;
    [SerializeField] private int unlockCost = 0;

    public string StationId => stationId;
    public string DisplayName => displayName;
    public bool IsUnlocked => isUnlocked;
    public int UnlockCost => unlockCost;
    public Vector3 MarkerWorldPosition => markerAnchor != null ? markerAnchor.position : transform.position;

    public void Configure(string newStationId, string newDisplayName, TrainVipHandler handlerToNotify = null, Transform newMarkerAnchor = null)
    {
        stationId = newStationId;
        displayName = newDisplayName;
        trainHandlerToNotify = handlerToNotify;
        markerAnchor = newMarkerAnchor;
    }

    public void SetUnlocked(bool unlocked)
    {
        isUnlocked = unlocked;
    }

    public void SetUnlockCost(int cost)
    {
        unlockCost = cost;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        SelectStation();
    }

    private void OnMouseDown()
    {
        SelectStation();
    }

    public void SelectStation()
    {
        Clicked?.Invoke(this);

        if (trainHandlerToNotify != null)
        {
            trainHandlerToNotify.RequestTravelTo(this);
        }
    }
}
