using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Shared styling helpers for the upgrade menu so the train-tier list and the
/// minor-upgrade list look identical and read correctly. Both lists spawn the
/// same Button.prefab, whose label had word-wrapping ON — when a button ended up
/// narrow every label wrapped to one character per line ("F/A/I/R/I/N/G"). These
/// helpers force a sane column layout and a no-wrap, auto-sizing label so the
/// menu is readable and big enough to tap on a phone. No new art — reuses the
/// existing Button.prefab and TMP font.
/// </summary>
public static class UpgradeUiStyle
{
    /// <summary>
    /// Make a container lay its spawned buttons out as a clean vertical stack that
    /// fills the column width (so labels never collapse to 1 char wide).
    /// </summary>
    public static void ConfigureColumn(RectTransform container, float spacing = 10f, int pad = 10, float buttonHeight = 84f)
    {
        if (container == null) return;

        // The container may already carry a GridLayoutGroup (the minor-upgrade grid does).
        // Unity forbids a second LayoutGroup on the same object, so reuse whatever's there
        // and make it behave as one clean full-width vertical column.
        GridLayoutGroup grid = container.GetComponent<GridLayoutGroup>();
        if (grid != null)
        {
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 1;
            grid.startAxis = GridLayoutGroup.Axis.Vertical;
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.spacing = new Vector2(0f, spacing);
            grid.padding = new RectOffset(pad, pad, pad, pad);
            float w = container.rect.width - pad * 2f;
            if (w < 80f) w = 240f; // fallback before the layout has settled; self-corrects on Refresh
            grid.cellSize = new Vector2(w, buttonHeight);
            return;
        }

        VerticalLayoutGroup vlg = container.GetComponent<VerticalLayoutGroup>();
        if (vlg == null) vlg = container.gameObject.AddComponent<VerticalLayoutGroup>();
        if (vlg == null) return; // defensive: AddComponent can fail if another LayoutGroup slipped in
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;   // buttons fill the column width
        vlg.childForceExpandHeight = false; // height comes from the LayoutElement
        vlg.spacing = spacing;
        vlg.padding = new RectOffset(pad, pad, pad, pad);
        vlg.childAlignment = TextAnchor.UpperCenter;
    }

    /// <summary>
    /// Style one freshly-instantiated Button.prefab: give it a big touch height and
    /// a single no-wrap, auto-sizing label that fills the button.
    /// </summary>
    public static void StyleButton(GameObject go, float minHeight = 78f)
    {
        if (go == null) return;

        LayoutElement le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();
        le.minHeight = minHeight;
        le.preferredHeight = minHeight;
        le.minWidth = 120f;
        le.flexibleWidth = 1f;

        TMP_Text[] labels = go.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            TMP_Text t = labels[i];
            t.textWrappingMode = TextWrappingModes.NoWrap; // never wrap to 1 char/line
            t.overflowMode = TextOverflowModes.Ellipsis;
            t.enableAutoSizing = true;
            t.fontSizeMin = 12f;
            t.fontSizeMax = 30f;
            t.alignment = TextAlignmentOptions.Center;

            RectTransform rt = t.rectTransform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = new Vector2(12f, 6f);
            rt.offsetMax = new Vector2(-12f, -6f);
        }
    }
}
