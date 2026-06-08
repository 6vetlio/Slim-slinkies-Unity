using UnityEngine;

public class QuickToggle : MonoBehaviour
{
    [SerializeField] private GameObject targetPanel;

public void TogglePanel()
{
    Debug.Log("Button clicked!");

    if (targetPanel == null) return;

    targetPanel.SetActive(!targetPanel.activeSelf);
}
}