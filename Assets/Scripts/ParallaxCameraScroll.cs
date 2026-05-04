using UnityEngine;

public class ParallaxCameraScroll : MonoBehaviour
{
    [SerializeField] private float scrollSpeed = 2f;

    private void Update()
    {
        transform.position += Vector3.right * (scrollSpeed * Time.deltaTime);
    }
}
