using UnityEngine;

/// <summary>
/// Dev shortcut: press 1-4 to jump between upgrade stages.
/// Stage 1 = Arriva, 2 = Bullet Train, 3 = Hyperloop, 4 = Dark Hyperloop
/// </summary>
public class DevTrainSwitcher : MonoBehaviour
{
    [SerializeField] private GameObject[] trainVisuals;
    [SerializeField] private TransportSwitcher transportSwitcher;
    [SerializeField] private TrainMover trainMover;

    [Header("Tier Config (per stage 1-4)")]
    [SerializeField] private float[] travelDurations = { 18f, 12f, 6f, 3f };
    [SerializeField] private int[] passengerBonus = { 0, 15, 40, 80 };

    private int currentStage = 0;

    void Start()
    {
        if (trainMover == null)
            trainMover = FindFirstObjectByType<TrainMover>();
        SetStage(0);
    }

    void Update()
    {
        for (int i = 0; i < trainVisuals.Length && i < 4; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                SetStage(i);
        }
    }

    void SetStage(int stage)
    {
        currentStage = stage;
        for (int i = 0; i < trainVisuals.Length; i++)
        {
            if (trainVisuals[i] != null)
                trainVisuals[i].SetActive(i == stage);
        }

        bool isHyperloop = stage >= 2;
        if (transportSwitcher != null)
        {
            if (isHyperloop)
                transportSwitcher.SwitchToHyperloop();
            else
                transportSwitcher.SwitchToTrain();
        }

        if (trainMover != null && stage < travelDurations.Length)
        {
            if (isHyperloop)
                trainMover.hyperloopDuration = travelDurations[stage];
            else
                trainMover.normalTrainDuration = travelDurations[stage];
        }

        if (GameManager.Instance != null && stage < passengerBonus.Length)
        {
            GameManager.Instance.SetTrainTierEconomy(passengerBonus[stage]);
        }

        Debug.Log($"[DEV] Train stage {stage + 1}: duration={travelDurations[stage]}s, bonus={passengerBonus[stage]} passengers");
    }
}
