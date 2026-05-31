using UnityEngine;

public class ShopItem : MonoBehaviour
{
    public GameObject itemPrefab;
    public Transform placementCanvas;

    public void BuyItem()
    {
        GameObject item = Instantiate(itemPrefab, placementCanvas);

        item.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
    }
}
