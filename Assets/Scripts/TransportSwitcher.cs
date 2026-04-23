using UnityEngine;

public class TransportSwitcher : MonoBehaviour
{
    public GameObject trainImage;
    public GameObject hyperloopImage;

    public void SwitchToTrain()
    {
        trainImage.SetActive(true);
        hyperloopImage.SetActive(false);
    }

    public void SwitchToHyperloop()
    {
        Debug.Log("train: " + trainImage.name + " hyperloop: " + hyperloopImage.name);
        trainImage.SetActive(false);
        hyperloopImage.SetActive(true);
        Debug.Log("hyperloop active: " + hyperloopImage.activeSelf);
    }
}