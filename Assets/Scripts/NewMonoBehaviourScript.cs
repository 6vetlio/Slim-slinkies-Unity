using UnityEngine;

public class LockY : MonoBehaviour
{
    float lockedY;

    void Start()
    {
        lockedY = transform.position.y;
    }

    void LateUpdate()
    {
        Vector3 pos = transform.position;
        pos.y = lockedY;
        transform.position = pos;
    }
}