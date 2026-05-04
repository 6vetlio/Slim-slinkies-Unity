using UnityEngine;
using UnityEngine.UI;

public class TransportSwitcher : MonoBehaviour
{
    public GameObject trainImage;
    public GameObject hyperloopImage;
    public GameObject tubeLayer;

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
