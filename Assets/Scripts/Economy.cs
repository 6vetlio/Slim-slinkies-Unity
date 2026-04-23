using UnityEngine;
using TMPro;

public class Economy : MonoBehaviour
{
    public int passengers = 50;
    public float revenuePerPassenger = 1f;

    float totalMoney = 0f;
    bool upgraded = false;

    public TMP_Text passengersText;
    public TMP_Text revenueText;
    public TMP_Text totalText;

    public GameObject upgradeButton;
    public TransportSwitcher switcher;

    void Update()
    {
        float revenuePerSecond = passengers * revenuePerPassenger;
        totalMoney += revenuePerSecond * Time.deltaTime;

        passengersText.text = "Passengers: " + passengers;
        revenueText.text = "Revenue/s: €" + revenuePerSecond.ToString("F2");
        totalText.text = "Total: €" + totalMoney.ToString("F0");

        if (!upgraded && totalMoney >= 1000f)
        {
            upgraded = true;
            upgradeButton.SetActive(true);
        }
    }

    public void OnUpgradePressed()
    {
        totalMoney -= 1000f;
        passengers = 150;
        upgraded = true;
        upgradeButton.SetActive(false);
        switcher.SwitchToHyperloop();
    }
}