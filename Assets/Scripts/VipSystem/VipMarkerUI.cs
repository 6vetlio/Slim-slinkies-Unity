using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VipMarkerUI : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text destinationText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private Image portraitImage;
    [SerializeField] private Button travelButton;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.5f, 0f);
    [SerializeField] private bool followStationInWorld = true;

    private VipPassenger vip;
    private Station originStation;

    public void ConfigureReferences(
        TMP_Text newNameText,
        TMP_Text newDestinationText,
        TMP_Text newTimerText,
        Image newPortraitImage = null,
        Button newTravelButton = null,
        bool newFollowStationInWorld = true)
    {
        if (travelButton != null)
        {
            travelButton.onClick.RemoveListener(HandleTravelClicked);
        }

        nameText = newNameText;
        destinationText = newDestinationText;
        timerText = newTimerText;
        portraitImage = newPortraitImage;
        travelButton = newTravelButton;
        followStationInWorld = newFollowStationInWorld;

        if (travelButton != null)
        {
            travelButton.onClick.AddListener(HandleTravelClicked);
        }
    }

    private void Awake()
    {
        if (travelButton != null)
        {
            travelButton.onClick.AddListener(HandleTravelClicked);
        }
    }

    private void OnDestroy()
    {
        if (travelButton != null)
        {
            travelButton.onClick.RemoveListener(HandleTravelClicked);
        }
    }

    private void LateUpdate()
    {
        if (vip == null)
        {
            return;
        }

        if (followStationInWorld && originStation != null)
        {
            transform.position = originStation.MarkerWorldPosition + worldOffset;
        }

        if (timerText != null)
        {
            timerText.text = Mathf.CeilToInt(vip.PickupTimeRemaining) + "s";
        }
    }

    public void Bind(VipPassenger passenger, Station origin)
    {
        vip = passenger;
        originStation = origin;

        if (nameText != null)
        {
            nameText.text = vip.PassengerName;
        }

        if (destinationText != null)
        {
            destinationText.text = "To " + vip.DestinationStationName;
        }

        if (portraitImage != null)
        {
            portraitImage.sprite = vip.Portrait;
            portraitImage.enabled = vip.Portrait != null;
        }

        if (originStation != null)
        {
            transform.position = originStation.MarkerWorldPosition + worldOffset;
        }
    }

    private void HandleTravelClicked()
    {
        if (originStation != null)
        {
            originStation.SelectStation();
        }
    }
}
