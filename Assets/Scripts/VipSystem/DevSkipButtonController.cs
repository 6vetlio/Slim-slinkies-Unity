using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DevSkipButtonController : MonoBehaviour
{
    [SerializeField] private TrainMover trainMover;
    [SerializeField] private Canvas targetCanvas;

    private Button button;

    private void Start()
    {
        if (trainMover == null)
        {
            trainMover = FindFirstObjectByType<TrainMover>();
        }

        if (targetCanvas == null)
        {
            targetCanvas = FindFirstObjectByType<Canvas>();
        }

        CreateButton();
    }

    private void CreateButton()
    {
        if (targetCanvas == null)
        {
            return;
        }

        GameObject buttonObject = new GameObject("Dev Button");
        buttonObject.transform.SetParent(targetCanvas.transform, false);

        RectTransform rectTransform = buttonObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.anchoredPosition = new Vector2(18f, -18f);
        rectTransform.sizeDelta = new Vector2(132f, 40f);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.08f, 0.22f, 0.32f, 0.95f);

        button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(SkipTravel);

        GameObject labelObject = new GameObject("Label");
        labelObject.transform.SetParent(buttonObject.transform, false);

        RectTransform labelRectTransform = labelObject.AddComponent<RectTransform>();
        labelRectTransform.anchorMin = Vector2.zero;
        labelRectTransform.anchorMax = Vector2.one;
        labelRectTransform.offsetMin = Vector2.zero;
        labelRectTransform.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        label.text = "dev button";
        label.fontSize = 18f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
    }

    private void SkipTravel()
    {
        if (trainMover != null)
        {
            trainMover.SkipToDestination();
        }
    }
}
