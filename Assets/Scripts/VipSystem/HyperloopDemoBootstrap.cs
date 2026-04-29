using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[ExecuteAlways]
public class HyperloopDemoBootstrap : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private TrainMover trainMover;
    [SerializeField] private TrainVipHandler trainVipHandler;
    [SerializeField] private MapStopController mapStopController;
    [SerializeField] private VipSpawnManager vipSpawnManager;
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private GameObject mapRoot;
    [SerializeField] private Button openMapButton;

    [Header("VIP References")]
    [SerializeField] private Station[] stations;
    [SerializeField] private Station startingStation;
    [SerializeField] private VipMarkerUI markerPrefab;
    [SerializeField] private Transform markerParent;

    [Header("HUD References")]
    [SerializeField] private HudController hudController;
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private TMP_Text passengersText;
    [SerializeField] private TMP_Text incomeText;
    [SerializeField] private TMP_Text onboardVipText;
    [SerializeField] private TMP_Text moneyPopupTemplate;
    [SerializeField] private Transform moneyPopupParent;

    private void OnEnable()
    {
        if (!Application.isPlaying)
        {
            EnsureSceneSetup();
        }
    }

    private void Start()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        SceneManager.sceneLoaded += HandleSceneLoaded;
        EnsureSceneSetup();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    [ContextMenu("Rebuild Scene UI")]
    private void RebuildSceneUi()
    {
        EnsureSceneSetup();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureSceneSetup();
    }

    private void EnsureSceneSetup()
    {
        EnsureCoreObjects();

        if (!Application.isPlaying)
        {
            return;
        }
    }

    private void EnsureCoreObjects()
    {
        GameObject gameManagerObject = GameObject.Find("GameManager");
        if (gameManagerObject == null)
        {
            gameManagerObject = new GameObject("GameManager");
        }

        if (gameManagerObject.GetComponent<GameManager>() == null)
        {
            gameManagerObject.AddComponent<GameManager>();
        }
    }

}
