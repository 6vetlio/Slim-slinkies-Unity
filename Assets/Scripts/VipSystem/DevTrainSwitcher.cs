using UnityEngine;

/// <summary>
/// Dev shortcut: press number keys 1..N (up to 9) to jump to that train tier for testing.
/// Speed (secondsPerChunk), visuals, passenger bonus and income multiplier all come from
/// the real TrainTierDefinition list on GameManager — each key just calls
/// GameManager.DevSetTrainTier, which fires TrainTierChanged so TransportSwitcher,
/// SpeedometerWidget, TrainMover and the upgrade panel all respond exactly like a real
/// purchase. Legacy fields (trainVisuals, transportSwitcher) are still applied if wired
/// in the scene, but the tier system is the source of truth.
/// </summary>
public class DevTrainSwitcher : MonoBehaviour
{
    [Header("Legacy / optional — leave empty when tier visuals are set on TransportSwitcher")]
    [SerializeField] private GameObject[] trainVisuals;
    [SerializeField] private TransportSwitcher transportSwitcher;

    void Update()
    {
        int count = GameManager.Instance != null ? GameManager.Instance.TrainTierCount : 0;
        if (count <= 0) return;

        int max = Mathf.Min(count, 9);
        for (int i = 0; i < max; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                GameManager.Instance.DevSetTrainTier(i);
                ApplyLegacyFallback(i);
            }
        }
    }

    private void ApplyLegacyFallback(int stage)
    {
        if (trainVisuals != null)
        {
            for (int i = 0; i < trainVisuals.Length; i++)
            {
                if (trainVisuals[i] != null)
                {
                    trainVisuals[i].SetActive(i == stage);
                }
            }
        }
        if (transportSwitcher != null)
        {
            bool isHyperloop = stage >= 2;
            if (isHyperloop) transportSwitcher.SwitchToHyperloop();
            else transportSwitcher.SwitchToTrain();
        }
    }
}
