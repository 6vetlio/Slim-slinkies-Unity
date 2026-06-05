using UnityEngine;

public class NewBuyButton : MonoBehaviour
{
    public GameManager GameManager ;
    public GameObject ItemPrefab ;
    public Transform Interior ;

    public float price = 500f;
    public void BoughtArt()
    {
        if (GameManager.BuyArt(price))
        {
            Instantiate(ItemPrefab, Interior);
        }
    }

}
