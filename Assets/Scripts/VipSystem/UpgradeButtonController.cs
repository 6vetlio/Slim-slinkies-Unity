using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeButtonController : MonoBehaviour
{
    [SerializeField] private GameObject upgradeButton;
    [SerializeField] private Button button;
    [SerializeField] private TransportSwitcher transportSwitcher;

    private bool subscribedToGameManager;

    private void Awake()
    {
        ResolveButtonReferences();
    }

    private void OnEnable()
    {
        ResolveButtonReferences();
        TrySubscribeToGameManager();

        if (button != null)
        {
            button.onClick.RemoveListener(HandleUpgradePressed);
            button.onClick.AddListener(HandleUpgradePressed);
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null && subscribedToGameManager)
        {
            GameManager.Instance.MoneyChanged -= Refresh;
        }

        subscribedToGameManager = false;

        if (button != null)
        {
            button.onClick.RemoveListener(HandleUpgradePressed);
        }
    }

    private void Update()
    {
        TrySubscribeToGameManager();
        Refresh(); // Check train movement status every frame
    }

    private void HandleUpgradePressed()
    {
        if (GameManager.Instance == null || !GameManager.Instance.TryBuyUpgrade())
        {
            Refresh();
            return;
        }

        if (transportSwitcher != null)
        {
            transportSwitcher.SwitchToHyperloop();
        }

        Refresh();
    }

    private void Refresh()
    {
        if (upgradeButton == null)
        {
            return;
        }

        // Find TrainMover to check movement status
        var trainMover = FindFirstObjectByType<TrainMover>();
        bool trainTraveling = trainMover != null && trainMover.MovementStatus == TrainMovementStatus.Travelling;

        // Only show button if can afford AND train is not traveling
        bool canShow = GameManager.Instance != null
                       && GameManager.Instance.CanBuyUpgrade
                       && !trainTraveling;

        TMP_Text label = upgradeButton.GetComponentInChildren<TMP_Text>(true);
        if (label != null && GameManager.Instance != null)
        {
            if (!GameManager.Instance.AllMinorUpgradesPurchased)
            {
                label.text = "Upgrade Locked\nComplete minor upgrades";
            }
            else
            {
                label.text = "Upgrade\nEUR " + GameManager.Instance.UpgradeCost.ToString("F0");
            }
        }

        upgradeButton.SetActive(canShow);
    }

    private void TrySubscribeToGameManager()
    {
        if (GameManager.Instance == null || subscribedToGameManager)
        {
            return;
        }

        GameManager.Instance.MoneyChanged += Refresh;
        subscribedToGameManager = true;
        Refresh();
    }

    private void ResolveButtonReferences()
    {
        if (button == null && upgradeButton != null)
        {
            button = upgradeButton.GetComponent<Button>();
        }

        if (upgradeButton == null && button != null)
        {
            upgradeButton = button.gameObject;
        }
    }
}
