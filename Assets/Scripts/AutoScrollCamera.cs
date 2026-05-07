using UnityEngine;

public class AutoScrollCamera : MonoBehaviour
{
    [SerializeField] private bool allowAutoScroll = false;
    public float speed = 5f;
    private Vector3 lockedPosition;

    private void Awake()
    {
        lockedPosition = transform.position;
    }

    void Update()
    {
        if (!allowAutoScroll)
        {
            transform.position = lockedPosition;
            return;
        }

        Vector3 pos = transform.position;
        pos.x += speed * Time.deltaTime;

        transform.position = pos;
        lockedPosition = pos;
    }
}
