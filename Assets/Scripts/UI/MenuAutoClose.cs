using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to a menu panel. After ANY child Button is clicked, the panel hides
/// itself — so a menu that opens features (map, upgrades, shop) collapses as
/// soon as the player picks one, instead of staying open and crowding the view.
///
/// Each button keeps its own onClick action; this just adds a "close the menu"
/// listener on top. No per-button wiring needed — drop this on the menu root.
/// </summary>
public class MenuAutoClose : MonoBehaviour
{
    [Tooltip("Panel to deactivate after a child button is clicked. Defaults to this GameObject.")]
    [SerializeField] private GameObject panelToClose;

    [Tooltip("Also hook buttons that are inactive at startup (e.g. on a hidden menu).")]
    [SerializeField] private bool includeInactive = true;

    [Tooltip("Optional: buttons listed here are ignored (won't close the menu).")]
    [SerializeField] private Button[] ignoreButtons;

    [Tooltip("Ignore a close request within this many seconds of the menu opening, so the same click/tap that opened it can't immediately close it again (the classic 'need to double-click to open' bug).")]
    [SerializeField] private float reopenGuardSeconds = 0.2f;

    private float shownTime = -999f;

    private void OnEnable()
    {
        // Stamp when the menu became visible so CloseMenu can ignore the opening click.
        shownTime = Time.unscaledTime;
    }

    private void Awake()
    {
        if (panelToClose == null)
        {
            panelToClose = gameObject;
        }

        Button[] buttons = GetComponentsInChildren<Button>(includeInactive);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button b = buttons[i];
            if (IsIgnored(b))
            {
                continue;
            }
            b.onClick.AddListener(CloseMenu);
        }
    }

    private bool IsIgnored(Button b)
    {
        if (ignoreButtons == null)
        {
            return false;
        }
        for (int i = 0; i < ignoreButtons.Length; i++)
        {
            if (ignoreButtons[i] == b)
            {
                return true;
            }
        }
        return false;
    }

    private void CloseMenu()
    {
        // Don't let the very click/tap that opened the menu also close it.
        if (Time.unscaledTime - shownTime < reopenGuardSeconds)
        {
            return;
        }

        if (panelToClose != null)
        {
            panelToClose.SetActive(false);
        }
    }
}
