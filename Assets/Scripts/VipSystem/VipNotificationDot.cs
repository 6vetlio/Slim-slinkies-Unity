using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows a small notification dot on the menu button whenever a NEW VIP has spawned
/// that the player hasn't acknowledged yet. The dot clears as soon as the player taps
/// the menu button (i.e. opens the menu to look). Put this on the menu/cluster button
/// GameObject; it auto-finds a child named "NotificationDot" and the Button on itself.
/// </summary>
public class VipNotificationDot : MonoBehaviour
{
    [Tooltip("The dot GameObject to show/hide. Auto-found by the child named 'NotificationDot' if left null.")]
    [SerializeField] private GameObject dot;

    private int lastWaitingCount;
    private bool subscribed;

    private void Awake()
    {
        if (dot == null)
        {
            Transform t = transform.Find("NotificationDot");
            if (t != null) dot = t.gameObject;
        }

        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(Clear);
        }

        if (dot != null) dot.SetActive(false);
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Update()
    {
        // GameManager may not exist on the first frame; subscribe as soon as it does.
        if (!subscribed) TrySubscribe();
    }

    private void OnDisable()
    {
        if (subscribed && GameManager.Instance != null)
        {
            GameManager.Instance.VipsChanged -= HandleVipsChanged;
        }
        subscribed = false;
    }

    private void TrySubscribe()
    {
        if (subscribed || GameManager.Instance == null) return;
        GameManager.Instance.VipsChanged += HandleVipsChanged;
        subscribed = true;
        lastWaitingCount = GameManager.Instance.WaitingVips.Count;
        // If VIPs are already waiting when we come online, flag it.
        if (lastWaitingCount > 0 && dot != null) dot.SetActive(true);
    }

    private void HandleVipsChanged()
    {
        if (GameManager.Instance == null) return;
        int count = GameManager.Instance.WaitingVips.Count;
        // Only light up on an INCREASE (a fresh spawn), not on pickups/deliveries.
        if (count > lastWaitingCount && dot != null)
        {
            dot.SetActive(true);
        }
        lastWaitingCount = count;
    }

    private void Clear()
    {
        if (dot != null) dot.SetActive(false);
    }
}
