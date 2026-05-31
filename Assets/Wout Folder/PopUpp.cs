using UnityEngine;

public class PopupToggles: MonoBehaviour
{
    public GameObject popup;

    public void TogglePopupp()
    {
        popup.SetActive(!popup.activeSelf);
    }
    

}
