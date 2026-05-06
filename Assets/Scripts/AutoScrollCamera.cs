using UnityEngine;

public class AutoScrollCamera : MonoBehaviour
{
    public float speed = 5f;
    public float acceleration = 0.5f;

    void Update()
    {
        speed += acceleration * Time.deltaTime;

        transform.position += Vector3.right * speed * Time.deltaTime;
    }
}