using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Put this on the button that opens the train-upgrade page. The upgrade page is its
/// OWN full-screen panel (not docked beside the map): opening it closes the map (and any
/// other listed panels) so only one panel is ever open at a time, and clicking again
/// closes it. Reuses an existing cluster Button — no new art.
/// </summary>
[RequireComponent(typeof(Button))]
public class UpgradePanelButton : MonoBehaviour
{
    [Tooltip("The train-upgrade panel this button opens/closes (e.g. TrainUpgradesPanel).")]
    [SerializeField] private GameObject upgradesPanel;

    [Tooltip("Panels to force-closed when the upgrade page opens (e.g. the map root) so only one is open at a time.")]
    [SerializeField] private GameObject[] panelsToClose;

    private void Awake()
    {
        Button b = GetComponent<Button>();
        if (b != null)
        {
            b.onClick.RemoveListener(Toggle);
            b.onClick.AddListener(Toggle);
        }
    }

    public void Toggle()
    {
        if (upgradesPanel == null) return;

        bool open = !upgradesPanel.activeSelf;
        if (open && panelsToClose != null)
        {
            for (int i = 0; i < panelsToClose.Length; i++)
            {
                if (panelsToClose[i] != null) panelsToClose[i].SetActive(false);
            }
        }
        upgradesPanel.SetActive(open);
        if (open) upgradesPanel.transform.SetAsLastSibling();
    }
}
