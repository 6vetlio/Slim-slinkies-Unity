using UnityEngine;

/// <summary>
/// Dev shortcut: press number keys 1..N (up to 9) to jump to that train tier for testing.
/// Each key ONLY calls GameManager.DevSetTrainTier, which fires TrainTierChanged. The
/// visual swap is then handled by the single source of truth — TransportSwitcher.ApplyTierVisual
/// (driven by trainTierVisuals[]) — and the speedometer/economy/upgrade panel all follow the
/// same event. This avoids the old "two trains at once" bug, where this switcher toggled its
/// own trainVisuals[] AND called SwitchToHyperloop(), activating two train GameObjects.
/// </summary>
public class DevTrainSwitcher : MonoBehaviour
{
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
            }
        }
    }
}
