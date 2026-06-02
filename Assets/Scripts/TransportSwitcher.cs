using UnityEngine;
using UnityEngine.UI;

public class TransportSwitcher : MonoBehaviour
{
    public GameObject trainImage;
    public GameObject hyperloopImage;
    public GameObject tubeLayer;

    [Header("Tier Visuals (optional)")]
    [Tooltip("Optional per-tier train visuals. If empty, all tiers reuse the default trainImage (placeholder behavior).")]
    [SerializeField] private GameObject[] trainTierVisuals;

    private int activeTrainTierVisualIndex = -1;
    private bool subscribedToTierChanges;

    public GameObject GetActiveTransportVisual()
    {
        if (hyperloopImage != null && hyperloopImage.activeInHierarchy)
        {
            return hyperloopImage;
        }

        return trainImage;
    }

    private void Start()
    {
        SwitchToTrain();
        TrySubscribeToTierChanges();

        if (GameManager.Instance != null)
        {
            ApplyTierVisual(GameManager.Instance.CurrentTrainTier);
        }
    }

    private void OnEnable()
    {
        TrySubscribeToTierChanges();
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null && subscribedToTierChanges)
        {
            GameManager.Instance.TrainTierChanged -= ApplyTierVisual;
        }
        subscribedToTierChanges = false;
    }

    private void TrySubscribeToTierChanges()
    {
        if (GameManager.Instance == null || subscribedToTierChanges)
        {
            return;
        }

        GameManager.Instance.TrainTierChanged += ApplyTierVisual;
        subscribedToTierChanges = true;
    }

    public void ApplyTierVisual(int tierIndex)
    {
        // Placeholder behavior: until per-tier visuals are configured, keep the default train.
        // When trainTierVisuals[] is populated, enable the one for this tier and disable the others.
        if (trainTierVisuals == null || trainTierVisuals.Length == 0)
        {
            Debug.Log("[TransportSwitcher] ApplyTierVisual(" + tierIndex + ") — no per-tier visuals configured, keeping placeholder train sprite");
            return;
        }

        for (int i = 0; i < trainTierVisuals.Length; i++)
        {
            if (trainTierVisuals[i] != null)
            {
                trainTierVisuals[i].SetActive(i == tierIndex);
            }
        }
        activeTrainTierVisualIndex = tierIndex;
    }

    public void SwitchToTrain()
    {
        if (trainImage != null)
        {
            trainImage.SetActive(true);
            SetTransportVisualsActive(trainImage, true);
        }

        if (hyperloopImage != null)
        {
            hyperloopImage.SetActive(false);
        }

        if (tubeLayer != null)
        {
            tubeLayer.SetActive(false);
        }
    }

    public void SwitchToHyperloop()
    {
        if (trainImage != null)
        {
            trainImage.SetActive(true);
            SetTransportVisualsActive(trainImage, false);
        }

        if (hyperloopImage != null)
        {
            hyperloopImage.SetActive(true);
        }

        if (tubeLayer != null)
        {
            tubeLayer.SetActive(true);
        }
    }

    private void SetTransportVisualsActive(GameObject transportObject, bool active)
    {
        Image image = transportObject.GetComponent<Image>();
        if (image != null)
        {
            image.enabled = active;
        }

        SpriteRenderer spriteRenderer = transportObject.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = active;
        }
    }

    public bool IsHyperloopActive() =>
        hyperloopImage != null && hyperloopImage.activeInHierarchy;
}
