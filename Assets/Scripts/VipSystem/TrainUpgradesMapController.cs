using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    [Tooltip("If true, skips tier 0 (the base train) — it's already owned at start.")]
    [SerializeField] private bool skipBaseTier = true;
    [Tooltip("If true, only spawns the next-purchasable tier (single CTA). Owned tiers and far-future tiers stay hidden.")]
    [SerializeField] private bool onlyShowNextTier = false;

    private readonly List<TrainUpgradeButton> spawnedButtons = new List<TrainUpgradeButton>();
    private bool subscribed;

    private void Start()
    {
        TrySubscribeAndBuild();
    }

    private void OnEnable()
    {
        TrySubscribeAndBuild();
        RefreshAll();
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
    private void HandleTierChanged(int newTier)
    {
        // In single-CTA mode the spawned button set changes when the tier changes
        // (the next tier shifts up by one). Rebuild rather than refresh-in-place.
        if (onlyShowNextTier)
        {
            RebuildButtons();
        }
        RefreshAll();
    }

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

        int count = GameManager.Instance.TrainTierCount;
        int startIndex = skipBaseTier ? 1 : 0;

        if (onlyShowNextTier)
        {
            int next = GameManager.Instance.CurrentTrainTier + 1;
            if (next < startIndex || next >= count)
            {
                return; // No purchasable next tier — show nothing.
            }
            startIndex = next;
            count = next + 1;
        }

        for (int i = startIndex; i < count; i++)
        {
            GameObject go = Instantiate(buttonPrefab, buttonContainer);
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
        for (int i = 0; i < spawnedButtons.Count; i++)
        {
            if (spawnedButtons[i] != null)
            {
                spawnedButtons[i].Refresh();
            }
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
        bool canBuy = GameManager.Instance.CanBuyTrainTier(tierIndex) && !trainMoving;

        string priceText = owned ? "Owned" : "Buy — EUR " + def.cost.ToString("F0");

        if (nameLabel != null)
        {
            // When there's a separate price label, the name label only shows the name.
            // When there's only one label, combine name + price onto two lines.
            nameLabel.text = priceLabel != null
                ? def.displayName + (owned ? " (Owned)" : "")
                : def.displayName + "\n" + priceText;
        }

        if (priceLabel != null)
        {
            priceLabel.text = priceText;
        }

        if (iconImage != null && def.icon != null)
        {
            iconImage.sprite = def.icon;
            iconImage.enabled = true;
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
