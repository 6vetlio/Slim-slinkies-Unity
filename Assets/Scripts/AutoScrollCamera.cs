using UnityEngine;

public class AutoScrollCamera : MonoBehaviour
{
    public float speed = 5f;

    void Update()
    {
        Vector3 pos = transform.position;
        pos.x += speed * Time.deltaTime;

        transform.position = pos;
    }
}