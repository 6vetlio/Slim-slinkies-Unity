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

            // Use the VIP's real starting duration so the ring is accurate for any timer value.
            float estimatedTotal = vip.IsWaiting ? vip.PickupSecondsTotal : vip.DeliverySecondsTotal;
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

    // Guarantee a visible name label even if the prefab's "Name" child didn't propagate
    // to a pre-placed pool instance. Finds the authored child first; only builds one as a
    // last resort, reusing the timer's font so it always renders.
    private void EnsureNameLabel()
    {
        if (nameText != null) return;

        Transform existing = transform.Find("Name");
        if (existing != null)
        {
            nameText = existing.GetComponent<TMP_Text>();
            if (nameText != null) return;
        }

        if (timerText == null) return; // need a font to render with

        GameObject go = new GameObject("Name", typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(transform, false);
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 4f);
        rt.sizeDelta = new Vector2(72f, 12f);

        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.font = timerText.font;
        t.fontSize = 10f;
        t.color = Color.white;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;
        nameText = t;
    }

    public void Bind(VipPassenger passenger, Station origin)
    {
        vip = passenger;
        originStation = origin;

        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        EnsureNameLabel();

        if (useCompactIconMode)
        {
            rectTransform.sizeDelta = new Vector2(compactIconSize, compactIconSize);
            
            if (nameText != null)
            {
                nameText.gameObject.SetActive(true);
                nameText.text = vip.PassengerName;
                nameText.fontSize = 7f;
                nameText.enableAutoSizing = false;
                nameText.color = Color.white;
                nameText.alignment = TextAlignmentOptions.Center;
            }
            if (destinationText != null) destinationText.gameObject.SetActive(false);
            if (timerText != null)
            {
                timerText.gameObject.SetActive(true);
                // Small, fixed size so the countdown sits tucked under the pin instead of
                // a giant number floating over the map.
                timerText.fontSize = 7f;
                timerText.enableAutoSizing = false;
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
