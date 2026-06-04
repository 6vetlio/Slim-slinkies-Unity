using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Visual gauge that reflects the current train tier's top speed. The needle
/// rotates from -90° (left) to +90° (right) as the train tier increases, the
/// optional arc-fill Image fills proportionally, and a TMP label shows the
/// km/h number. Updates whenever GameManager.TrainTierChanged fires.
///
/// Setup:
///  - Place on a panel containing a needle Image (pivot at the bottom),
///    optionally an arc-fill Image (Image Type: Filled / Radial 180 / Bottom
///    origin), and a TMP label.
///  - Wire each in the Inspector.
///  - Adjust maxNeedleSpeedKmh to match the fastest tier so Hyperloop sits at
///    the full-right tick.
/// </summary>
public class SpeedometerWidget : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("Needle Image with its pivot at the bottom so a Z rotation swings it left/right.")]
    [SerializeField] private RectTransform needle;
    [Tooltip("Optional. Image with Image Type: Filled. Fill amount is set to currentSpeed / maxNeedleSpeedKmh.")]
    [SerializeField] private Image arcFill;
    [Tooltip("Optional. TMP label that reads the current top speed.")]
    [SerializeField] private TMP_Text speedLabel;
    [Tooltip("Optional. TMP label showing a short suffix (e.g. \"MAX SPEED\").")]
    [SerializeField] private TMP_Text captionLabel;

    [Header("Tuning")]
    [Tooltip("Speed (km/h) at which the needle reaches the full-right tick. Should match or exceed the fastest tier.")]
    [SerializeField] private float maxNeedleSpeedKmh = 800f;
    [Tooltip("Needle Z rotation (degrees) at zero km/h. Positive = pointing left.")]
    [SerializeField] private float needleZeroAngle = 90f;
    [Tooltip("Needle Z rotation (degrees) at maxNeedleSpeedKmh. Negative = pointing right.")]
    [SerializeField] private float needleMaxAngle = -90f;
    [Tooltip("Format string for the speed label. {0} = km/h number.")]
    [SerializeField] private string speedFormat = "{0:0} km/h";

    private bool subscribed;

    private void OnEnable()
    {
        TrySubscribeAndRefresh();
    }

    private void Start()
    {
        TrySubscribeAndRefresh();
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null && subscribed)
        {
            GameManager.Instance.TrainTierChanged -= HandleTierChanged;
        }
        subscribed = false;
    }

    private void TrySubscribeAndRefresh()
    {
        if (GameManager.Instance == null)
        {
            return;
        }
        if (!subscribed)
        {
            GameManager.Instance.TrainTierChanged += HandleTierChanged;
            subscribed = true;
        }
        Refresh();
    }

    private void HandleTierChanged(int newTier)
    {
        Refresh();
    }

    public void Refresh()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        float topSpeed = GameManager.Instance.CurrentTrainTopSpeedKmh;
        float normalized = Mathf.Clamp01(topSpeed / Mathf.Max(1f, maxNeedleSpeedKmh));

        if (needle != null)
        {
            float angle = Mathf.Lerp(needleZeroAngle, needleMaxAngle, normalized);
            needle.localEulerAngles = new Vector3(0f, 0f, angle);
        }

        if (arcFill != null)
        {
            arcFill.fillAmount = normalized;
        }

        if (speedLabel != null)
        {
            speedLabel.text = string.Format(speedFormat, topSpeed);
        }

        if (captionLabel != null && string.IsNullOrEmpty(captionLabel.text))
        {
            captionLabel.text = "MAX SPEED";
        }
    }
}
