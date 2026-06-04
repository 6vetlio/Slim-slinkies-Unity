using System.Reflection;
using UnityEngine;

/// <summary>
/// In-game cheat panel for quickly testing upgrades. Press F1 to toggle.
///
/// Auto-spawns itself on scene load — no GameObject or wiring needed. Pure
/// IMGUI so it never depends on the Canvas setup we're iterating on. Uses
/// reflection for a couple of private GameManager hooks (AddMoney) so we
/// don't have to expose dev-only APIs in production code.
/// </summary>
public class DevMenu : MonoBehaviour
{
    private const KeyCode ToggleKey = KeyCode.F1;

    private bool open = true;
    private Rect window = new Rect(20, 20, 560, 760);
    private string moneyInput = "1000";
    private string vipInput = "1";
    private const float UiScale = 2.5f;
    private static GUIStyle bigLabel;
    private static GUIStyle bigButton;
    private static GUIStyle bigField;
    private static GUIStyle bigWindow;

    private static MethodInfo addMoneyMethod;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<DevMenu>() != null) return;
        var go = new GameObject("[DevMenu]");
        DontDestroyOnLoad(go);
        go.AddComponent<DevMenu>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(ToggleKey)) open = !open;
    }

    private void OnGUI()
    {
        if (!open) return;
        EnsureStyles();
        window = GUILayout.Window(0xDEAFBABE.GetHashCode(), window, DrawWindow, "Dev Menu  (F1 to hide)", bigWindow);
    }

    private static void EnsureStyles()
    {
        if (bigLabel != null) return;
        int fs = Mathf.RoundToInt(14 * UiScale);
        bigLabel = new GUIStyle(GUI.skin.label) { fontSize = fs };
        bigButton = new GUIStyle(GUI.skin.button) { fontSize = fs, fixedHeight = fs * 2f, padding = new RectOffset(12, 12, 6, 6) };
        bigField = new GUIStyle(GUI.skin.textField) { fontSize = fs, fixedHeight = fs * 2f };
        bigWindow = new GUIStyle(GUI.skin.window) { fontSize = fs };
    }

    private void DrawWindow(int id)
    {
        var gm = GameManager.Instance;
        if (gm == null)
        {
            GUILayout.Label("GameManager not ready.", bigLabel);
            GUI.DragWindow();
            return;
        }

        GUILayout.Label($"Money: EUR {gm.Money:0}", bigLabel);
        GUILayout.Label($"Income/s: EUR {gm.PassiveIncomePerSecond:0.0}", bigLabel);
        GUILayout.Label($"Tier: {gm.CurrentTrainTier} / {gm.TrainTierCount - 1}  |  {gm.CurrentTrainTopSpeedKmh:0} km/h", bigLabel);
        GUILayout.Label($"Onboard: {gm.OnboardVips.Count}/{gm.MaxOnboardVips}", bigLabel);
        GUILayout.Label($"Minor upgrades: {CountOwnedMinor(gm)} / {gm.MinorUpgradeCount}", bigLabel);

        GUILayout.Space(10);
        GUILayout.Label("Money", bigLabel);
        GUILayout.BeginHorizontal();
        moneyInput = GUILayout.TextField(moneyInput, bigField, GUILayout.Width(160));
        if (GUILayout.Button("Add", bigButton) && int.TryParse(moneyInput, out var v)) AddMoney(gm, v);
        if (GUILayout.Button("+10k", bigButton)) AddMoney(gm, 10000);
        if (GUILayout.Button("+100k", bigButton)) AddMoney(gm, 100000);
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        GUILayout.Label("Tier", bigLabel);
        if (GUILayout.Button("Force +1 Tier (auto-pay)", bigButton)) ForceTierUp(gm);
        if (GUILayout.Button("Reload scene (reset)", bigButton)) ReloadScene();

        GUILayout.Space(10);
        GUILayout.Label("VIPs", bigLabel);
        GUILayout.BeginHorizontal();
        vipInput = GUILayout.TextField(vipInput, bigField, GUILayout.Width(120));
        if (GUILayout.Button("Spawn", bigButton) && int.TryParse(vipInput, out var n)) SpawnVips(n);
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        if (GUILayout.Button("Buy ALL minor upgrades", bigButton)) BuyAllMinor(gm);
        if (GUILayout.Button("Time skip +60s income", bigButton))
        {
            AddMoney(gm, Mathf.RoundToInt(gm.PassiveIncomePerSecond * 60f));
        }

        GUILayout.Space(10);
        GUILayout.Label("F1 = hide. Drag title to move.", bigLabel);
        GUI.DragWindow(new Rect(0, 0, 10000, 44));
    }

    private static void AddMoney(GameManager gm, int amount)
    {
        if (addMoneyMethod == null)
        {
            addMoneyMethod = typeof(GameManager).GetMethod(
                "AddMoney",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        }
        if (addMoneyMethod == null)
        {
            Debug.LogWarning("[DevMenu] GameManager.AddMoney not found via reflection.");
            return;
        }
        addMoneyMethod.Invoke(gm, new object[] { amount });
    }

    private static void ForceTierUp(GameManager gm)
    {
        int next = gm.CurrentTrainTier + 1;
        var def = gm.GetTrainTier(next);
        if (def == null) { Debug.Log("[DevMenu] Already at max tier."); return; }
        // Throw plenty of money at it so cost is never the blocker.
        AddMoney(gm, Mathf.Max(1, Mathf.CeilToInt(def.cost)) + 10);
        gm.TryBuyTrainTier(next);
    }

    private static void ReloadScene()
    {
        var s = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        UnityEngine.SceneManagement.SceneManager.LoadScene(s.buildIndex);
    }

    private static void SpawnVips(int n)
    {
        var spawner = FindFirstObjectByType<VipSpawnManager>();
        if (spawner == null) { Debug.LogWarning("[DevMenu] No VipSpawnManager in scene."); return; }
        for (int i = 0; i < n; i++) spawner.SpawnVip();
    }

    private static void BuyAllMinor(GameManager gm)
    {
        AddMoney(gm, 1_000_000);
        for (int i = 0; i < gm.MinorUpgradeCount; i++)
        {
            gm.TryBuyMinorUpgrade(i);
        }
    }

    private static int CountOwnedMinor(GameManager gm)
    {
        int count = 0;
        for (int i = 0; i < gm.MinorUpgradeCount; i++)
        {
            if (gm.IsMinorUpgradePurchased(i)) count++;
        }
        return count;
    }
}
