using UnityEngine;

public class PopupToggle : MonoBehaviour
{
    public GameObject overlayPanel;
    public GameObject popup;

    public void TogglePopup()
    {
        popup.SetActive(!popup.activeSelf);
    }
    public void OpenOverlay()
    {
        overlayPanel.SetActive(true);
    }

    public void CloseOverlay()
    {
        overlayPanel.SetActive(false);
    }
}
