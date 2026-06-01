using UnityEngine;

public class NewBuyButton : MonoBehaviour
{
    public GameManager GameManager ;
    public GameObject ItemPrefab ;
    public Transform Interior ;

    public void BoughtArt()
    {
        if (GameManager.BuyArt())
        {
            Instantiate(ItemPrefab, Interior);
        }
    }

}
