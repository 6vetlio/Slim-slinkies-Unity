using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VipMarkerUI : MonoBehaviour
{
    [Header("Display Mode")]
    [SerializeField] private bool useCompactIconMode = true;
    [SerializeField] private float compactIconSize = 36f;
    
    [Header("UI Elements")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text destinationText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private Image portraitImage;
    [SerializeField] private Image npcIconImage;
    [SerializeField] private Sprite maleNpcSprite;
    [SerializeField] private Sprite femaleNpcSprite;
    [SerializeField] private Button travelButton;
    
    [Header("Progress Ring")]
    [SerializeField] private Image progressRing;
    [SerializeField] private Color waitingColor = Color.yellow;
    [SerializeField] private Color urgentColor = Color.red;
    [SerializeField] private Color onboardColor = Color.green;
    [SerializeField] private Color defaultColor = Color.white;
    
    [Header("Positioning")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.5f, 0f);
    [SerializeField] private bool followStationInWorld = true;
    [SerializeField] private bool followStationOnMapCanvas = true;
    [SerializeField] private Vector2 mapCanvasOffset = new Vector2(52f, 18f);

    private VipPassenger vip;
    private Station originStation;
    private RectTransform rectTransform;

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

        if (nameText == null)
        {
            nameText = newNameText;
        }
        if (destinationText == null)
        {
            destinationText = newDestinationText;
        }
        if (timerText == null)
        {
            timerText = newTimerText;
        }
        if (portraitImage == null)
        {
            portraitImage = newPortraitImage;
        }
        if (travelButton == null)
        {
            travelButton = newTravelButton;
        }
        followStationInWorld = newFollowStationInWorld;

        if (travelButton != null)
        {
            travelButton.onClick.AddListener(HandleTravelClicked);
        }
    }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
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

        if (followStationOnMapCanvas && originStation != null && rectTransform != null)
        {
            RectTransform stationRt = originStation.GetComponent<RectTransform>();
            if (stationRt != null)
            {
                rectTransform.position = stationRt.position + new Vector3(mapCanvasOffset.x, mapCanvasOffset.y, 0f);
            }
        }
        else if (followStationInWorld && originStation != null)
        {
            transform.position = originStation.MarkerWorldPosition + worldOffset;
        }

        if (timerText != null)
        {
            timerText.text = Mathf.CeilToInt(vip.PickupTimeRemaining) + "s";
        }
        
        // Update progress ring
        if (progressRing != null && vip != null)
        {
            float timeRemaining = vip.IsWaiting ? vip.PickupTimeRemaining : vip.DeliveryTimeRemaining;
            
            // Estimate progress based on typical VIP timers (60s for pickup, 90s for delivery)
            float estimatedTotal = vip.IsWaiting ? 60f : 90f;
            float progress = estimatedTotal > 0 ? Mathf.Clamp01(timeRemaining / estimatedTotal) : 0f;
            
            progressRing.fillAmount = progress;
            
            // Color coding based on VIP state
            if (vip.IsOnboard)
            {
                progressRing.color = onboardColor;
            }
            else if (vip.IsWaiting)
            {
                if (timeRemaining < 10f)
                {
                    progressRing.color = urgentColor;
                }
                else if (timeRemaining < 30f)
                {
                    progressRing.color = waitingColor;
                }
                else
                {
                    progressRing.color = defaultColor;
                }
            }
            else
            {
                progressRing.color = defaultColor;
            }
        }
    }

    public void Bind(VipPassenger passenger, Station origin)
    {
        vip = passenger;
        originStation = origin;

        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        if (useCompactIconMode)
        {
            rectTransform.sizeDelta = new Vector2(compactIconSize, compactIconSize);
            
            if (nameText != null)
            {
                nameText.gameObject.SetActive(true);
                nameText.text = vip.PassengerName;
                nameText.fontSize = 10f;
                nameText.color = Color.white;
                nameText.alignment = TextAlignmentOptions.Center;
            }
            if (destinationText != null) destinationText.gameObject.SetActive(false);
            if (timerText != null)
            {
                timerText.gameObject.SetActive(true);
                timerText.fontSize = 14f;
                timerText.alignment = TextAlignmentOptions.Center;
            }
        }
        else
        {
            if (nameText != null)
            {
                nameText.gameObject.SetActive(true);
                nameText.text = vip.PassengerName;
                nameText.fontSize = 12f;
            }

            if (destinationText != null)
            {
                destinationText.gameObject.SetActive(true);
                destinationText.text = vip.DestinationStationName;
                destinationText.fontSize = 11f;
            }
        }

        bool usePortrait = vip.Portrait != null && portraitImage != null;
        if (usePortrait)
        {
            portraitImage.sprite = vip.Portrait;
            portraitImage.enabled = true;
            if (npcIconImage != null)
            {
                npcIconImage.enabled = false;
            }
        }
        else if (npcIconImage != null)
        {
            npcIconImage.sprite = vip.IsMale ? maleNpcSprite : femaleNpcSprite;
            npcIconImage.enabled = npcIconImage.sprite != null;
            if (portraitImage != null)
            {
                portraitImage.enabled = false;
            }
        }

        if (followStationOnMapCanvas && originStation != null && rectTransform != null)
        {
            RectTransform stationRt = originStation.GetComponent<RectTransform>();
            if (stationRt != null)
            {
                rectTransform.position = stationRt.position + new Vector3(mapCanvasOffset.x, mapCanvasOffset.y, 0f);
            }
        }
        else if (followStationInWorld && originStation != null)
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
