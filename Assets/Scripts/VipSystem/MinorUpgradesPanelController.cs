using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Spawns one button per minor upgrade defined on the GameManager and wires
/// click-to-purchase. Buttons auto-refresh their label and interactable state
/// when money changes or an upgrade is purchased.
///
/// Setup:
///  - Place this on a parent UI panel in the Canvas.
///  - Assign a button prefab that contains a TMP_Text label (and optionally a
///    second TMP_Text for the cost, a Button, and an Image for an icon).
///  - Assign the container Transform that buttons will be parented to (usually
///    a VerticalLayoutGroup or GridLayoutGroup).
/// </summary>
public class MinorUpgradesPanelController : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private RectTransform buttonContainer;
    [SerializeField] private GameObject buttonPrefab;
    [Tooltip("Optional: hides the panel entirely once every minor upgrade is purchased.")]
    [SerializeField] private bool hideWhenAllPurchased = false;

    private readonly List<MinorUpgradeButton> spawnedButtons = new List<MinorUpgradeButton>();
    private bool subscribed;

    private void OnEnable()
    {
        TrySubscribeAndBuild();
    }

    private void Start()
    {
        TrySubscribeAndBuild();
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null && subscribed)
        {
            GameManager.Instance.MoneyChanged -= HandleStateChanged;
            GameManager.Instance.MinorUpgradePurchased -= HandleUpgradePurchased;
            GameManager.Instance.TrainTierChanged -= HandleTierChanged;
        }
        subscribed = false;
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
            GameManager.Instance.MinorUpgradePurchased += HandleUpgradePurchased;
            GameManager.Instance.TrainTierChanged += HandleTierChanged;
            subscribed = true;
        }

        RebuildButtons();
        RefreshAll();
    }

    private void HandleStateChanged()
    {
        RefreshAll();
    }

    private void HandleUpgradePurchased(int index)
    {
        RefreshAll();
    }

    // The current tier's minor-upgrade list is the source of truth — when the
    // tier changes (player bought the next train), the spawned button set must
    // change to reflect the new tier's upgrades.
    private void HandleTierChanged(int newTier)
    {
        RebuildButtons();
        RefreshAll();
    }

    private void RebuildButtons()
    {
        if (buttonContainer == null || buttonPrefab == null || GameManager.Instance == null)
        {
            return;
        }

        // Clear existing
        for (int i = spawnedButtons.Count - 1; i >= 0; i--)
        {
            if (spawnedButtons[i] != null && spawnedButtons[i].gameObject != null)
            {
                Destroy(spawnedButtons[i].gameObject);
            }
        }
        spawnedButtons.Clear();

        int count = GameManager.Instance.MinorUpgradeCount;
        for (int i = 0; i < count; i++)
        {
            GameObject go = Instantiate(buttonPrefab, buttonContainer);
            MinorUpgradeButton wrapper = go.GetComponent<MinorUpgradeButton>();
            if (wrapper == null)
            {
                wrapper = go.AddComponent<MinorUpgradeButton>();
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

        if (hideWhenAllPurchased && GameManager.Instance != null && GameManager.Instance.AllMinorUpgradesPurchased)
        {
            gameObject.SetActive(false);
        }
    }
}

/// <summary>
/// Component attached to each spawned minor upgrade button prefab. Auto-finds
/// the Button, an optional icon Image, and TMP_Text labels in its children.
/// </summary>
public class MinorUpgradeButton : MonoBehaviour
{
    private int upgradeIndex = -1;
    private Button button;
    private TMP_Text nameLabel;
    private TMP_Text costLabel;
    private Image iconImage;

    public void Bind(int index)
    {
        upgradeIndex = index;
        button = GetComponentInChildren<Button>(true);

        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
        if (texts.Length > 0) nameLabel = texts[0];
        if (texts.Length > 1) costLabel = texts[1];

        Image[] images = GetComponentsInChildren<Image>(true);
        // Skip the button's own background image if present.
        for (int i = 0; i < images.Length; i++)
        {
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

    public void Refresh()
    {
        if (GameManager.Instance == null || upgradeIndex < 0)
        {
            return;
        }

        MinorUpgradeDefinition def = GameManager.Instance.GetMinorUpgrade(upgradeIndex);
        if (def == null)
        {
            return;
        }

        bool purchased = GameManager.Instance.IsMinorUpgradePurchased(upgradeIndex);
        bool unlocked = GameManager.Instance.IsMinorUpgradeUnlocked(upgradeIndex);
        bool canBuy = GameManager.Instance.CanBuyMinorUpgrade(upgradeIndex);

        if (nameLabel != null)
        {
            string prefix = purchased ? "" : (unlocked ? "" : "🔒 ");
            string suffix = purchased ? " (Owned)" : "";
            nameLabel.text = prefix + def.displayName + suffix;
        }

        if (costLabel != null)
        {
            if (purchased)
            {
                costLabel.text = "+EUR " + def.passiveIncomeBonusPerSecond + "/s";
            }
            else if (!unlocked)
            {
                costLabel.text = "Locked";
            }
            else
            {
                costLabel.text = "EUR " + def.cost + "  (+" + def.passiveIncomeBonusPerSecond + "/s)";
            }
        }

        if (iconImage != null && def.icon != null)
        {
            iconImage.sprite = def.icon;
        }

        if (button != null)
        {
            button.interactable = canBuy;
        }
    }

    private void HandleClick()
    {
        if (GameManager.Instance == null || upgradeIndex < 0)
        {
            return;
        }

        GameManager.Instance.TryBuyMinorUpgrade(upgradeIndex);
        Refresh();
    }
}
