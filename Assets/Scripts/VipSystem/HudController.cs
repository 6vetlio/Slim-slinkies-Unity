using System.Collections;
using TMPro;
using UnityEngine;

public class HudController : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private TMP_Text passengersText;
    [SerializeField] private TMP_Text incomeText;
    [SerializeField] private TMP_Text onboardVipText;

    [Header("Money Popup")]
    [SerializeField] private TMP_Text moneyPopupPrefab;
    [SerializeField] private Transform moneyPopupParent;
    [SerializeField] private Color rewardColor = new Color(0.25f, 1f, 0.35f);
    [SerializeField] private Color penaltyColor = new Color(1f, 0.25f, 0.25f);
    [SerializeField] private float popupLifetime = 1.25f;
    [SerializeField] private float popupRiseDistance = 45f;

    private bool subscribedToGameManager;

    public void Configure(
        TMP_Text newMoneyText,
        TMP_Text newPassengersText,
        TMP_Text newIncomeText,
        TMP_Text newOnboardVipText,
        TMP_Text newMoneyPopupPrefab,
        Transform newMoneyPopupParent)
    {
        if (moneyText == null)
        {
            moneyText = newMoneyText;
        }
        if (passengersText == null)
        {
            passengersText = newPassengersText;
        }
        if (incomeText == null)
        {
            incomeText = newIncomeText;
        }
        if (onboardVipText == null)
        {
            onboardVipText = newOnboardVipText;
        }
        if (moneyPopupPrefab == null)
        {
            moneyPopupPrefab = newMoneyPopupPrefab;
        }
        if (moneyPopupParent == null)
        {
            moneyPopupParent = newMoneyPopupParent;
        }
        TrySubscribeToGameManager();
        Refresh();
    }

    private void OnEnable()
    {
        TrySubscribeToGameManager();
        Refresh();
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null && subscribedToGameManager)
        {
            GameManager.Instance.MoneyChanged -= Refresh;
            GameManager.Instance.VipsChanged -= Refresh;
            GameManager.Instance.VipRewarded -= HandleVipRewarded;
            GameManager.Instance.VipPenalized -= HandleVipPenalized;
        }

        subscribedToGameManager = false;
    }

    private void Update()
    {
        TrySubscribeToGameManager();
        RefreshOnboardVipTimer();
    }

    public void Refresh()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        if (moneyText != null)
        {
            moneyText.text = "Money: EUR " + GameManager.Instance.Money.ToString("F0");
        }

        if (passengersText != null)
        {
            passengersText.text = "Passengers: " + GameManager.Instance.RegularPassengers;
        }

        if (incomeText != null)
        {
            incomeText.text = "Income/s: EUR " + GameManager.Instance.PassiveIncomePerSecond.ToString("F1");
        }

        RefreshOnboardVipTimer();
    }

    private void RefreshOnboardVipTimer()
    {
        if (onboardVipText == null || GameManager.Instance == null)
        {
            return;
        }

        VipPassenger vip = GameManager.Instance.CurrentOnboardVip;
        if (vip == null)
        {
            onboardVipText.text = "Onboard VIP: None";
            return;
        }

        onboardVipText.text = "VIP: " + vip.PassengerName + " to " + vip.DestinationStationName + " (" + Mathf.CeilToInt(vip.DeliveryTimeRemaining) + "s)";
    }

    private void HandleVipRewarded(VipPassenger vip, int amount)
    {
        ShowMoneyPopup("+EUR " + amount, rewardColor);
    }

    private void HandleVipPenalized(VipPassenger vip, int amount)
    {
        ShowMoneyPopup("-EUR " + amount, penaltyColor);
    }

    private void ShowMoneyPopup(string text, Color color)
    {
        if (moneyPopupPrefab == null)
        {
            Debug.Log(text);
            return;
        }

        Transform parent = moneyPopupParent != null ? moneyPopupParent : transform;
        TMP_Text popup = Instantiate(moneyPopupPrefab, parent);
        popup.text = text;
        popup.color = color;
        StartCoroutine(AnimatePopup(popup));
    }

    private IEnumerator AnimatePopup(TMP_Text popup)
    {
        RectTransform rectTransform = popup.rectTransform;
        Vector2 startPosition = rectTransform.anchoredPosition;
        Vector2 endPosition = startPosition + Vector2.up * popupRiseDistance;
        float elapsed = 0f;
        Color startColor = popup.color;

        while (elapsed < popupLifetime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / popupLifetime);
            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, endPosition, t);
            popup.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);
            yield return null;
        }

        Destroy(popup.gameObject);
    }

    private void TrySubscribeToGameManager()
    {
        if (GameManager.Instance == null || subscribedToGameManager)
        {
            return;
        }

        GameManager.Instance.MoneyChanged += Refresh;
        GameManager.Instance.VipsChanged += Refresh;
        GameManager.Instance.VipRewarded += HandleVipRewarded;
        GameManager.Instance.VipPenalized += HandleVipPenalized;
        subscribedToGameManager = true;
    }
}
