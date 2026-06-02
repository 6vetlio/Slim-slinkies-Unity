using UnityEngine;

public class ShopList : MonoBehaviour
{
    public GameObject ShopLists;

    public void Shoplist()
    {
        ShopLists.SetActive(!ShopLists.activeSelf);
    }

}
