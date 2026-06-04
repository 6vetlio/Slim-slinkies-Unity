using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Drives an Image (Image Type: Filled / Horizontal) + an optional TMP label
/// to show how full the train is — onboard VIPs out of the current max.
///
/// Setup:
///  - Place on a panel that owns a horizontal-fill Image and (optionally) a
///    TMP label like "3 / 3".
///  - Wire fillImage and label in the Inspector.
///  - Refreshes whenever VipsChanged or TrainTierChanged fires.
/// </summary>
public class CapacityBar : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text label;
    [SerializeField] private string format = "{0} / {1}";

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
            GameManager.Instance.VipsChanged -= Refresh;
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
            GameManager.Instance.VipsChanged += Refresh;
            GameManager.Instance.TrainTierChanged += HandleTierChanged;
            subscribed = true;
        }
        Refresh();
    }

    private void HandleTierChanged(int _)
    {
        Refresh();
    }

    public void Refresh()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        int onboard = GameManager.Instance.OnboardVips.Count;
        int max = Mathf.Max(1, GameManager.Instance.MaxOnboardVips);

        if (fillImage != null)
        {
            fillImage.fillAmount = Mathf.Clamp01((float)onboard / max);
        }

        if (label != null)
        {
            label.text = string.Format(format, onboard, max);
        }
    }
}
