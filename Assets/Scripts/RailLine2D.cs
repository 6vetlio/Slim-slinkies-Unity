using UnityEngine;

[ExecuteAlways]
public class RailLine2D : MonoBehaviour
{
    [Header("Points")]
    public Transform pointA;
    public Transform pointB;

    [Header("2D Line")]
    public Vector2 center;
    public float length = 12f;
    public float angleDegrees = 0f;
    public float zPosition = 0f;
    public bool useLocalPosition = false;
    public bool updateInEditMode = true;

    private void OnValidate()
    {
        if (updateInEditMode)
        {
            ApplyLine();
        }
    }

    private void Update()
    {
        if (!Application.isPlaying && updateInEditMode)
        {
            ApplyLine();
        }
    }

    [ContextMenu("Apply 2D Rail Line")]
    public void ApplyLine()
    {
        if (pointA == null || pointB == null)
        {
            return;
        }

        float radians = angleDegrees * Mathf.Deg2Rad;
        Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        Vector2 halfOffset = direction * (length * 0.5f);

        SetPointPosition(pointA, center - halfOffset);
        SetPointPosition(pointB, center + halfOffset);
    }

    private void SetPointPosition(Transform point, Vector2 position2D)
    {
        Vector3 position = new Vector3(position2D.x, position2D.y, zPosition);

        if (useLocalPosition)
        {
            point.localPosition = position;
        }
        else
        {
            point.position = position;
        }
    }
}
