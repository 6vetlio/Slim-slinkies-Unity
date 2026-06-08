using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
// NOTE: scene still serialises `skipBaseTier` and `onlyShowNextTier` against
// this controller. Unity discards those fields silently — keep them deleted.

/// <summary>
/// Spawns one buy-button per train tier defined on GameManager. Lives inside
/// the map panel — only shows tiers above the currently owned tier as
/// purchasable; owned tiers show as "Owned"; tiers beyond the next purchasable
/// one are visible but disabled.
///
/// Setup:
///  - Place on a parent UI panel that is a child of the map root.
///  - Assign `buttonContainer` (a Transform with VerticalLayoutGroup or similar).
///  - Assign `buttonPrefab` (a Button with a TMP_Text label, optional second
///    TMP_Text for price, and an Image for the train icon).
///  - Optional: assign `trainMover` to disable buys while the train is moving.
/// </summary>
public class TrainUpgradesMapController : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private RectTransform buttonContainer;
    [SerializeField] private GameObject buttonPrefab;

    [Header("Gating")]
    [Tooltip("Optional. If assigned, upgrade buttons are disabled while the train is moving.")]
    [SerializeField] private TrainMover trainMover;

    [Header("Layout")]
    [Tooltip("If true, the panel reanchors itself when it opens. Saves brutus a scene anchor pass.")]
    [SerializeField] private bool forceLayoutOnEnable = true;
    [Tooltip("Where the panel sits on the screen. Default (0.55, 0)-(1, 1) puts it on the right side, leaving the map peek on the left.")]
    [SerializeField] private Vector2 panelAnchorMin = new Vector2(0.55f, 0f);
    [SerializeField] private Vector2 panelAnchorMax = new Vector2(1f, 1f);
    [Tooltip("Split inside the panel: train upgrades on the left column, minor upgrades on the right column. 0.55 leaves a bit more room for the minor list since it has more entries.")]
    [Range(0.1f, 0.9f)]
    [SerializeField] private float trainColumnRightEdge = 0.5f;
    [Tooltip("Optional. If assigned, alpha is bumped to backgroundAlpha so the map underneath is hidden.")]
    [SerializeField] private Image panelBackground;
    [Range(0f, 1f)]
    [SerializeField] private float backgroundAlpha = 0.92f;
    [Tooltip("If true, this panel hides itself on Awake so the upgrades page isn't visible on game start.")]
    [SerializeField] private bool startClosed = true;

    [Header("Minor Upgrades Reparent")]
    [Tooltip("If true, on enable the controller finds a MinorUpgradesPanelController in the scene and reparents it under this panel so train tiers + minor upgrades share one page.")]
    [SerializeField] private bool autoMergeMinorUpgrades = true;
    [Tooltip("Optional explicit reference. Leave null to FindFirstObjectByType at runtime.")]
    [SerializeField] private MinorUpgradesPanelController minorUpgradesPanel;

    [Header("Header")]
    [Tooltip("Optional. The 'TRAIN STAGE X/5' label. Auto-found by the child named 'TierHeader' if left null.")]
    [SerializeField] private TMP_Text tierHeaderLabel;

    private readonly List<TrainUpgradeButton> spawnedButtons = new List<TrainUpgradeButton>();
    private bool subscribed;

    private bool startClosedApplied;

    private void ResolveHeader()
    {
        if (tierHeaderLabel == null)
        {
            Transform h = transform.Find("TierHeader");
            if (h != null) tierHeaderLabel = h.GetComponent<TMP_Text>();
        }
    }

    private void Awake()
    {
        if (startClosed && !startClosedApplied)
        {
            startClosedApplied = true;
            // Defer until end-of-frame so MapStopController's wiring (which also
            // toggles this panel) doesn't race us on Awake order.
            gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        TrySubscribeAndBuild();
    }

    private void OnEnable()
    {
        if (forceLayoutOnEnable)
        {
            ApplyPanelLayout();
        }
        BringSelfToTop();
        TryMergeMinorUpgradesPanel();
        ResolveHeader();
        if (forceLayoutOnEnable)
        {
            ApplyColumnSplit();
        }
        TrySubscribeAndBuild();
        RefreshAll();
    }

    private void BringSelfToTop()
    {
        // The most recently opened UI panel should render on top of the rest.
        transform.SetAsLastSibling();
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null && parentCanvas.transform != transform)
        {
            // Bubble the bring-to-top up to the canvas root in case our parent
            // panel is also stacked with siblings (e.g. map root and HUD).
            Transform t = transform;
            while (t.parent != null && t.parent != parentCanvas.transform)
            {
                t.parent.SetAsLastSibling();
                t = t.parent;
            }
        }
    }

    private void TryMergeMinorUpgradesPanel()
    {
        if (!autoMergeMinorUpgrades) return;

        if (minorUpgradesPanel == null)
        {
            minorUpgradesPanel = FindFirstObjectByType<MinorUpgradesPanelController>(FindObjectsInactive.Include);
        }
        if (minorUpgradesPanel == null) return;

        // Reparent under this panel so train tiers and minor upgrades render on
        // the same page. Keep the minor-upgrades panel's own layout intact.
        if (minorUpgradesPanel.transform.parent != transform)
        {
            minorUpgradesPanel.transform.SetParent(transform, false);
            minorUpgradesPanel.transform.SetAsLastSibling();
        }
        minorUpgradesPanel.gameObject.SetActive(true);
    }

    private void ApplyPanelLayout()
    {
        // Anchor the whole upgrades panel to the configured screen region (default:
        // right half) so it doesn't drown the rest of the HUD.
        RectTransform panelRect = transform as RectTransform;
        if (panelRect != null)
        {
            // Big, near-fullscreen and centred — this is its OWN page (not docked beside
            // the map) and must be comfortable to tap on a phone. (The old right-half
            // anchors are ignored on purpose.)
            panelRect.anchorMin = new Vector2(0.04f, 0.05f);
            panelRect.anchorMax = new Vector2(0.96f, 0.95f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panelRect.pivot = new Vector2(0.5f, 0.5f);
        }

        if (panelBackground == null)
        {
            panelBackground = GetComponent<Image>();
        }
        if (panelBackground != null)
        {
            Color c = panelBackground.color;
            c.a = Mathf.Max(c.a, backgroundAlpha);
            panelBackground.color = c;
        }
    }

    private void ApplyColumnSplit()
    {
        // A LayoutGroup on the panel ROOT force-positions its children every frame,
        // which OVERRIDES the column anchors set below — the train-tier column and the
        // minor-upgrade column then collapse on top of each other (the overlapping
        // "REI / NEO / ARRIVA / OWNED" garble). The two-column split is done by explicit
        // anchors, so the root must NOT also run an auto-layout. Disable it if present.
        var rootLayout = GetComponent<UnityEngine.UI.LayoutGroup>();
        if (rootLayout != null)
        {
            rootLayout.enabled = false;
        }

        // Reserve the top strip of the panel for the header (TRAIN STAGE X/5).
        const float headerHeight = 64f;

        // Header: full-width strip pinned to the top of the panel.
        if (tierHeaderLabel != null)
        {
            RectTransform hr = tierHeaderLabel.rectTransform;
            hr.anchorMin = new Vector2(0f, 1f);
            hr.anchorMax = new Vector2(1f, 1f);
            hr.pivot = new Vector2(0.5f, 1f);
            hr.offsetMin = new Vector2(16f, -headerHeight);
            hr.offsetMax = new Vector2(-16f, -8f);
            tierHeaderLabel.alignment = TextAlignmentOptions.Center;
            tierHeaderLabel.textWrappingMode = TextWrappingModes.NoWrap;
            tierHeaderLabel.enableAutoSizing = true;
            tierHeaderLabel.fontSizeMin = 16f;
            tierHeaderLabel.fontSizeMax = 40f;
        }

        // Train upgrades occupy the left column (below the header), minor upgrades the
        // right column. Without this, both controllers stretch full-panel and their
        // children stack on top of each other.
        if (buttonContainer != null)
        {
            buttonContainer.anchorMin = new Vector2(0f, 0f);
            buttonContainer.anchorMax = new Vector2(trainColumnRightEdge, 1f);
            buttonContainer.offsetMin = new Vector2(12f, 12f);
            buttonContainer.offsetMax = new Vector2(-6f, -(headerHeight + 6f));
            buttonContainer.pivot = new Vector2(0.5f, 0.5f);
        }

        if (minorUpgradesPanel != null)
        {
            RectTransform minorRect = minorUpgradesPanel.transform as RectTransform;
            if (minorRect != null)
            {
                minorRect.anchorMin = new Vector2(trainColumnRightEdge, 0f);
                minorRect.anchorMax = new Vector2(1f, 1f);
                minorRect.offsetMin = new Vector2(6f, 12f);
                minorRect.offsetMax = new Vector2(-12f, -(headerHeight + 6f));
                minorRect.pivot = new Vector2(0.5f, 0.5f);
            }
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null && subscribed)
        {
            GameManager.Instance.MoneyChanged -= HandleStateChanged;
            GameManager.Instance.TrainTierChanged -= HandleTierChanged;
        }
        subscribed = false;
    }

    private void Update()
    {
        // Refresh interactability based on current train movement state.
        for (int i = 0; i < spawnedButtons.Count; i++)
        {
            if (spawnedButtons[i] != null)
            {
                spawnedButtons[i].SetTrainMoving(trainMover != null && trainMover.MovementStatus == TrainMovementStatus.Travelling);
            }
        }
    }

    private void TrySubscribeAndBuild()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        if (!subscribed)
        {
            GameManager.Instance.MoneyChanged += HandleStateChanged;
            GameManager.Instance.TrainTierChanged += HandleTierChanged;
            subscribed = true;
        }

        if (trainMover == null)
        {
            trainMover = FindFirstObjectByType<TrainMover>();
        }

        RebuildButtons();
        RefreshAll();
    }

    private void HandleStateChanged() => RefreshAll();
    private void HandleTierChanged(int newTier) => RefreshAll();

    private void RebuildButtons()
    {
        if (buttonContainer == null || buttonPrefab == null || GameManager.Instance == null)
        {
            return;
        }

        for (int i = spawnedButtons.Count - 1; i >= 0; i--)
        {
            if (spawnedButtons[i] != null && spawnedButtons[i].gameObject != null)
            {
                Destroy(spawnedButtons[i].gameObject);
            }
        }
        spawnedButtons.Clear();

        // Make the container stack buttons cleanly at full column width.
        UpgradeUiStyle.ConfigureColumn(buttonContainer);

        // Always show every tier. Owned ones (including tier 0 at start) render as
        // "Owned"; the next one is the buy CTA; further ones are 🔒 Locked.
        int count = GameManager.Instance.TrainTierCount;
        for (int i = 0; i < count; i++)
        {
            GameObject go = Instantiate(buttonPrefab, buttonContainer);
            UpgradeUiStyle.StyleButton(go); // big touch height + no-wrap label
            TrainUpgradeButton wrapper = go.GetComponent<TrainUpgradeButton>();
            if (wrapper == null)
            {
                wrapper = go.AddComponent<TrainUpgradeButton>();
            }
            wrapper.Bind(i);
            spawnedButtons.Add(wrapper);
        }
    }

    private void RefreshAll()
    {
        // Keep the column layout in sync (grid cell width tracks real container width).
        UpgradeUiStyle.ConfigureColumn(buttonContainer);

        for (int i = 0; i < spawnedButtons.Count; i++)
        {
            if (spawnedButtons[i] != null)
            {
                spawnedButtons[i].Refresh();
            }
        }

        if (tierHeaderLabel != null && GameManager.Instance != null)
        {
            int tier = GameManager.Instance.CurrentTrainTier;
            int total = GameManager.Instance.TrainTierCount;
            TrainTierDefinition cur = GameManager.Instance.CurrentTrainTierDef;
            string name = cur != null ? cur.displayName : "";
            tierHeaderLabel.text = $"TRAIN STAGE {tier + 1}/{total}"
                                   + (string.IsNullOrEmpty(name) ? "" : " — " + name);
        }
    }
}

/// <summary>
/// Wrapper attached to each spawned train upgrade button. Auto-resolves a
/// Button, two TMP_Text labels (name + price), and an icon Image from its
/// children.
/// </summary>
public class TrainUpgradeButton : MonoBehaviour
{
    private int tierIndex = -1;
    private Button button;
    private TMP_Text nameLabel;
    private TMP_Text priceLabel;
    private Image iconImage;
    private bool trainMoving;

    public void Bind(int index)
    {
        tierIndex = index;
        button = GetComponentInChildren<Button>(true);

        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
        if (texts.Length > 0) nameLabel = texts[0];
        if (texts.Length > 1) priceLabel = texts[1];

        Image[] images = GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            // Skip the button's own background image if present.
            if (button != null && images[i].gameObject == button.gameObject)
            {
                continue;
            }
            iconImage = images[i];
            break;
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClick);
        }

        Refresh();
    }

    public void SetTrainMoving(bool moving)
    {
        if (trainMoving == moving) return;
        trainMoving = moving;
        Refresh();
    }

    public void Refresh()
    {
        if (GameManager.Instance == null || tierIndex < 0)
        {
            return;
        }

        TrainTierDefinition def = GameManager.Instance.GetTrainTier(tierIndex);
        if (def == null)
        {
            return;
        }

        bool owned = GameManager.Instance.IsTrainTierOwned(tierIndex);
        bool isNext = GameManager.Instance.IsNextTrainTier(tierIndex);
        bool minorUpgradesDone = GameManager.Instance.AllMinorUpgradesPurchasedForCurrentTier;
        bool canBuy = GameManager.Instance.CanBuyTrainTier(tierIndex) && !trainMoving;

        string priceText;
        string namePrefix = "";
        if (owned)
        {
            priceText = "Owned";
        }
        else if (!isNext)
        {
            priceText = "🔒 Locked";
            namePrefix = "🔒 ";
        }
        else if (!minorUpgradesDone)
        {
            priceText = "Finish current train's upgrades";
            namePrefix = "🔒 ";
        }
        else
        {
            priceText = "Buy — EUR " + def.cost.ToString("F0");
        }

        if (nameLabel != null)
        {
            // When there's a separate price label, the name label only shows the name.
            // When there's only one label, combine name + price onto two lines.
            nameLabel.text = priceLabel != null
                ? namePrefix + def.displayName + (owned ? " (Owned)" : "")
                : namePrefix + def.displayName + "\n" + priceText;
        }

        if (priceLabel != null)
        {
            priceLabel.text = priceText;
        }

        // Hide the icon slot when there's no sprite, so it isn't a white box over the label.
        if (iconImage != null)
        {
            iconImage.enabled = def.icon != null;
            if (def.icon != null) iconImage.sprite = def.icon;
        }

        if (button != null)
        {
            button.interactable = canBuy;
        }
    }

    private void HandleClick()
    {
        if (GameManager.Instance == null || tierIndex < 0)
        {
            return;
        }

        GameManager.Instance.TryBuyTrainTier(tierIndex);
        Refresh();
    }
}
