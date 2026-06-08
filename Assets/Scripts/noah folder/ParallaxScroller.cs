using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Looping, multi-speed parallax for the UI background. Put this ONE component on
/// <c>worldcontainer</c> (the parent of the chunk/background layers).
///
/// While the train is travelling it scrolls each repeating background layer LEFT at a
/// speed proportional to its depth factor — distant layers (Sky, BackHills) move
/// slowly, near layers (FrontHills, Grass, Rails) move fast — which reads as depth.
/// Layers are matched to factors by a name substring, so no per-object wiring.
///
/// Seamless loop (Tiled-Image technique): with <see cref="autoConfigureTiling"/> on,
/// at Start each matched layer's Image is switched to <c>Type.Tiled</c> and its rect is
/// widened to <see cref="tiledRectWidth"/>, so the sprite repeats across a wide band.
/// The scroll then wraps by ONE tile width (the sprite's native width), which is
/// invisible because every tile is identical — no blue gaps, never empties.
///
/// Only genuinely repeating bands belong here (sky, hills, acres, grass, rails).
/// One-off landmarks (Skyline, city signs, station) are NOT tiled — they slide in at
/// arrival in a separate pass.
///
/// This REPLACES the old "slide the whole worldcontainer" scroll. Disable the
/// TrainMovement component so the two don't fight.
/// </summary>
[DisallowMultipleComponent]
public class ParallaxScroller : MonoBehaviour
{
    [System.Serializable]
    public struct LayerRule
    {
        [Tooltip("Case-insensitive name substring, e.g. \"backhills\".")]
        public string nameContains;
        [Range(0f, 2f)]
        [Tooltip("Scroll speed multiple. <1 = further/slower, 1 = ground speed.")]
        public float factor;
    }

    [Header("Refs")]
    [Tooltip("Auto-found if empty. Used so parallax only runs while travelling.")]
    [SerializeField] private TrainMover trainMover;

    [Header("Speed")]
    [Tooltip("UI units/second a factor-1 layer scrolls while travelling.")]
    [SerializeField] private float baseScrollSpeed = 600f;
    [SerializeField] private bool onlyWhileTravelling = true;

    [Header("Seamless tiling")]
    [Tooltip("At Start, switch matched layers to Tiled Image + widen them so they loop seamlessly.")]
    [SerializeField] private bool autoConfigureTiling = true;
    [Tooltip("Width (local units) the repeating layers are stretched to when auto-tiling.")]
    [SerializeField] private float tiledRectWidth = 3000f;

    [Header("Depth factors (repeating layers only)")]
    [SerializeField]
    private List<LayerRule> rules = new List<LayerRule>
    {
        new LayerRule { nameContains = "sky",         factor = 0.05f },
        new LayerRule { nameContains = "backhills",   factor = 0.25f },
        new LayerRule { nameContains = "backacres",   factor = 0.25f },
        new LayerRule { nameContains = "wheathills",  factor = 0.40f },
        new LayerRule { nameContains = "middlehills", factor = 0.50f },
        new LayerRule { nameContains = "middleacres", factor = 0.50f },
        new LayerRule { nameContains = "fronthills",  factor = 0.80f },
        new LayerRule { nameContains = "frontacres",  factor = 0.80f },
        new LayerRule { nameContains = "grassback",   factor = 0.70f },
        new LayerRule { nameContains = "grassfront",  factor = 1.00f },
        new LayerRule { nameContains = "rails",       factor = 1.00f },
    };

    private class Tracked
    {
        public RectTransform rt;
        public float startX;
        public float tileWidth;
        public float factor;
    }

    private readonly List<Tracked> tracked = new List<Tracked>();
    private float scroll;

    private void Start()
    {
        if (trainMover == null)
        {
            trainMover = FindFirstObjectByType<TrainMover>();
        }

        RectTransform[] all = GetComponentsInChildren<RectTransform>(true);
        foreach (RectTransform rt in all)
        {
            if (rt == transform) continue;

            float factor = MatchFactor(rt.name);
            if (factor < 0f) continue;

            Image img = rt.GetComponent<Image>();

            // Tile width = the sprite's native width (the repeat period). Falls back to
            // the rect width when there's no sprite.
            float tileWidth = (img != null && img.sprite != null)
                ? img.sprite.rect.width
                : rt.rect.width;
            if (tileWidth <= 1f) tileWidth = Mathf.Abs(rt.sizeDelta.x);
            tileWidth = Mathf.Max(1f, tileWidth);

            if (autoConfigureTiling && img != null && img.sprite != null)
            {
                img.type = Image.Type.Tiled;
                // Widen the band so a one-tile wrap is always hidden off-screen.
                Vector2 size = rt.sizeDelta;
                size.x = Mathf.Max(size.x, tiledRectWidth);
                rt.sizeDelta = size;
            }

            tracked.Add(new Tracked
            {
                rt = rt,
                startX = rt.localPosition.x,
                tileWidth = tileWidth,
                factor = factor
            });
        }

        Debug.Log($"[ParallaxScroller] tracking {tracked.Count} layers (baseSpeed={baseScrollSpeed}, autoTile={autoConfigureTiling}).");
    }

    private float MatchFactor(string objectName)
    {
        string n = objectName.ToLowerInvariant();
        for (int i = 0; i < rules.Count; i++)
        {
            string key = rules[i].nameContains;
            if (!string.IsNullOrEmpty(key) && n.Contains(key.ToLowerInvariant()))
            {
                return rules[i].factor;
            }
        }
        return -1f;
    }

    private void LateUpdate()
    {
        bool travelling = trainMover != null && trainMover.MovementStatus == TrainMovementStatus.Travelling;
        if (onlyWhileTravelling && !travelling)
        {
            return;
        }

        scroll += baseScrollSpeed * Time.deltaTime;

        for (int i = 0; i < tracked.Count; i++)
        {
            Tracked t = tracked[i];
            float offset = Mathf.Repeat(scroll * t.factor, t.tileWidth);
            Vector3 p = t.rt.localPosition;
            p.x = t.startX - offset;
            t.rt.localPosition = p;
        }
    }
}
