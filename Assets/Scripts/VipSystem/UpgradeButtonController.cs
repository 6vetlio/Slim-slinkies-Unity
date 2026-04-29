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

        upgradeButton.SetActive(GameManager.Instance != null && GameManager.Instance.CanBuyUpgrade);
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
