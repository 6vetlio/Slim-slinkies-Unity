using UnityEngine;

public class ParallaxCameraScroll : MonoBehaviour
{
    [SerializeField] private bool allowAutoScroll = false;
    [SerializeField] private float scrollSpeed = 2f;

    private void Update()
    {
        if (!allowAutoScroll)
        {
            return;
        }

        transform.position += Vector3.right * (scrollSpeed * Time.deltaTime);
    }
}
