using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Shows a small, easy-to-read price tag under every map Station: the unlock cost while a
/// station is still locked ("EUR 2000"), or "OPEN" once it's unlocked. The stations are
/// nested inside the map prefab, so one label per station is built at runtime here instead
/// of hand-authoring seven of them. Refreshes when money changes (a station may have just
/// been unlocked) and whenever the map is shown.
/// </summary>
public class StationPriceLabels : MonoBehaviour
{
    [Tooltip("Font for the price tags. If empty, the first TMP font found in the scene is reused.")]
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private float fontSize = 6.5f;
    [Tooltip("Label offset from the station centre, in canvas units. Negative Y sits it below the pin.")]
    [SerializeField] private Vector2 offset = new Vector2(0f, -13f);

    // Both colors set to white
    [SerializeField] private Color lockedColor = Color.white;
    [SerializeField] private Color openColor = Color.white;

    private readonly List<Station> stations = new List<Station>();
    private readonly List<TMP_Text> stationLabels = new List<TMP_Text>();
    private bool built;
    private bool subscribed;

    private void OnEnable()
    {
        Build();
        TrySubscribe();
        Refresh();
    }

    private void Start()
    {
        Build();
        TrySubscribe();
        Refresh();
    }

    private void OnDisable()
    {
        if (subscribed && GameManager.Instance != null)
        {
            GameManager.Instance.MoneyChanged -= Refresh;
        }
        subscribed = false;
    }

    private void TrySubscribe()
    {
        if (subscribed || GameManager.Instance == null) return;
        GameManager.Instance.MoneyChanged += Refresh;
        subscribed = true;
    }

    private void Build()
    {
        if (built) return;

        Station[] found = FindObjectsByType<Station>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (found == null || found.Length == 0) return;

        if (font == null)
        {
            TMP_Text any = FindFirstObjectByType<TMP_Text>(FindObjectsInactive.Include);
            if (any != null) font = any.font;
        }

        for (int i = 0; i < found.Length; i++)
        {
            Station s = found[i];
            if (s == null) continue;

            TMP_Text label = GetOrCreateLabel(s);
            if (label == null) continue;

            stations.Add(s);
            stationLabels.Add(label);
        }

        built = stationLabels.Count > 0;
    }

    private TMP_Text GetOrCreateLabel(Station s)
    {
        Transform existing = s.transform.Find("PriceLabel");
        if (existing != null)
        {
            TMP_Text t0 = existing.GetComponent<TMP_Text>();
            if (t0 != null) return t0;
        }

        GameObject go = new GameObject("PriceLabel", typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();

        rt.SetParent(s.transform, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = offset;
        rt.sizeDelta = new Vector2(64f, 10f);
        rt.localScale = Vector3.one;

        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();

        if (font != null)
            t.font = font;

        t.fontSize = fontSize;
        t.enableAutoSizing = false;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;

        return t;
    }

    private void Refresh()
    {
        for (int i = 0; i < stations.Count; i++)
        {
            Station s = stations[i];
            TMP_Text t = stationLabels[i];

            if (s == null || t == null)
                continue;

            if (s.IsUnlocked)
            {
                t.text = "OPEN";
                t.color = Color.white;
            }
            else
            {
                t.text = "EUR " + s.UnlockCost.ToString("0");
                t.color = Color.white;
            }
        }
    }
}