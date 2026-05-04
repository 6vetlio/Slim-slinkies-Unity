using UnityEngine;
using UnityEngine.UI;

public class RouteLineUI : MonoBehaviour
{
    [SerializeField] private string fromStationId;
    [SerializeField] private string toStationId;
    [SerializeField] private Image trackImage;
    [SerializeField] private Image fillImage;
    [SerializeField] private float trackThickness = 6f;
    [SerializeField] private float fillThickness = 4f;
    [SerializeField] private Color trackColorUnlocked = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    [SerializeField] private Color trackColorLocked = new Color(0.3f, 0.3f, 0.3f, 0.25f);
    [SerializeField] private Color fillColor = new Color(0.2f, 0.75f, 1f, 1f);

    public string FromStationId => fromStationId;
    public string ToStationId => toStationId;

    private void Awake()
    {
        if (fillImage != null)
        {
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillAmount = 0f;
        }
    }

    public void UpdateVisuals(Vector2 fromPos, Vector2 toPos, bool isUnlocked)
    {
        if (trackImage == null || fillImage == null) return;

        Vector2 dir = toPos - fromPos;
        float distance = dir.magnitude;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Vector2 center = (fromPos + toPos) * 0.5f;

        RectTransform trackRT = trackImage.rectTransform;
        trackRT.anchoredPosition = center;
        trackRT.sizeDelta = new Vector2(distance, trackThickness);
        trackRT.localEulerAngles = new Vector3(0f, 0f, angle);
        trackImage.color = isUnlocked ? trackColorUnlocked : trackColorLocked;

        RectTransform fillRT = fillImage.rectTransform;
        fillRT.anchoredPosition = center;
        fillRT.sizeDelta = new Vector2(distance, fillThickness);
        fillRT.localEulerAngles = new Vector3(0f, 0f, angle);
        fillImage.color = fillColor;
        fillImage.fillAmount = 0f;
    }

    public void SetFillAmount(float progress, bool flipDirection)
    {
        if (fillImage == null) return;

        fillImage.fillOrigin = flipDirection ? 1 : 0;
        fillImage.fillAmount = progress;
    }

    public void ClearFill()
    {
        if (fillImage != null)
            fillImage.fillAmount = 0f;
    }
}
