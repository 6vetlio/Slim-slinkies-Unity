using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class HudController : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private TMP_Text passengersText;
    [SerializeField] private TMP_Text incomeText;
    [SerializeField] private TMP_Text onboardVipCountText;
    [SerializeField] private TMP_Text onboardVipNamesText;

    [Header("Money Popup")]
    [SerializeField] private TMP_Text moneyPopupPrefab;
    [SerializeField] private Transform moneyPopupParent;
    [SerializeField] private Color rewardColor = new Color(0.25f, 1f, 0.35f);
    [SerializeField] private Color penaltyColor = new Color(1f, 0.25f, 0.25f);
    [SerializeField] private Color passiveIncomeColor = new Color(0.5f, 1f, 0.5f);
    [SerializeField] private Color notEnoughMoneyColor = new Color(1f, 0.5f, 0f);
    [SerializeField] private float popupLifetime = 1.25f;
    [SerializeField] private float popupRiseDistance = 45f;
    [SerializeField] private float passiveIncomePopupThreshold = 10f;

    private bool subscribedToGameManager;
    private float accumulatedPassiveIncome;
    private float passivePopupCooldown;

    public void Configure(
        TMP_Text newMoneyText,
        TMP_Text newPassengersText,
        TMP_Text newIncomeText,
        TMP_Text newOnboardVipCountText,
        TMP_Text newOnboardVipNamesText,
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
        if (onboardVipCountText == null)
        {
            onboardVipCountText = newOnboardVipCountText;
        }
        if (onboardVipNamesText == null)
        {
            onboardVipNamesText = newOnboardVipNamesText;
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
            GameManager.Instance.NotEnoughMoney -= HandleNotEnoughMoney;
        }

        subscribedToGameManager = false;
    }

    private void Update()
    {
        TrySubscribeToGameManager();
        RefreshOnboardVipTimer();
        TrackPassiveIncome();
    }
    
    private void TrackPassiveIncome()
    {
        // Passive income popups disabled per user request
        // VIP delivery popups are the primary money feedback
    }

    public void Refresh()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        if (moneyText != null)
        {
            moneyText.text = "EUR " + GameManager.Instance.Money.ToString("F0");
        }

        if (passengersText != null)
        {
            passengersText.text = "Passengers: " + GameManager.Instance.EffectivePassengers;
        }

        if (incomeText != null)
        {
            incomeText.text = "Income/s: EUR " + GameManager.Instance.PassiveIncomePerSecond.ToString("F1");
        }

        RefreshOnboardVipTimer();
    }

    private void RefreshOnboardVipTimer()
{
    if (GameManager.Instance == null)
    {
        return;
    }

    var onboard = GameManager.Instance.OnboardVips;
    int max = GameManager.Instance.MaxOnboardVips;

    // Count text
    if (onboardVipCountText != null)
    {
        onboardVipCountText.text = $"Onboard ({onboard.Count}/{max})";
    }

    // Names text
    if (onboardVipNamesText == null)
    {
        return;
    }

    if (onboard.Count == 0)
    {
        onboardVipNamesText.text = "Empty";
        return;
    }

    System.Text.StringBuilder sb = new System.Text.StringBuilder();

    for (int i = 0; i < onboard.Count; i++)
    {
        VipPassenger vip = onboard[i];

        sb.Append(vip.PassengerName);

        // Optional: keep destination + timer info
        sb.Append(" → ")
          .Append(vip.DestinationStationName)
          .Append(" (")
          .Append(Mathf.CeilToInt(vip.DeliveryTimeRemaining))
          .Append("s)");

        if (i < onboard.Count - 1)
        {
            sb.Append('\n');
        }
    }

    onboardVipNamesText.text = sb.ToString();
}

    private void HandleVipRewarded(VipPassenger vip, int amount)
    {
        Debug.Log("[HudController] VIP Rewarded: " + vip.PassengerName + " EUR " + amount);
        ShowMoneyPopup("+EUR " + amount, rewardColor);
    }

    private void HandleVipPenalized(VipPassenger vip, int amount)
    {
        Debug.Log("[HudController] VIP Penalized: " + vip.PassengerName + " EUR " + amount);
        ShowMoneyPopup("-EUR " + amount, penaltyColor);
    }

    private void HandleNotEnoughMoney(string message)
    {
        Debug.Log("[HudController] Not enough money: " + message);
        ShowMoneyPopup(message, notEnoughMoneyColor);
    }

    private void ShowMoneyPopup(string text, Color color)
    {
        if (moneyPopupPrefab == null)
        {
            Debug.LogWarning("[HudController] moneyPopupPrefab is null, cannot show popup: " + text);
            return;
        }
        
        if (moneyPopupParent == null)
        {
            Debug.LogWarning("[HudController] moneyPopupParent is null, using transform as parent");
        }

        Transform parent = moneyPopupParent != null ? moneyPopupParent : transform;
        TMP_Text popup = Instantiate(moneyPopupPrefab, parent);
        popup.gameObject.SetActive(true);
        popup.transform.SetAsLastSibling();

        if (moneyText != null && moneyText.font != null)
        {
            popup.font = moneyText.font;
        }

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
        GameManager.Instance.NotEnoughMoney += HandleNotEnoughMoney;
        subscribedToGameManager = true;
    }
}
