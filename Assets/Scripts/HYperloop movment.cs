using UnityEngine;
using System;

public class TrainMover : MonoBehaviour
{
    public Transform pointA;
    public Transform pointB;
    public float speed = 2f;
    public bool stopAtPointB = true;

    [Header("2D Lock")]
    public bool lockZPosition = true;
    public float zPosition = 0f;

    public event Action ReachedStationStop;

    private Transform target;
    private bool stoppedAtStation;
    private bool initialized = false;

    public bool IsStoppedAtStation => stoppedAtStation;

    void Update()
    {
        // Initialize on first Update if Start() didn't run
        if (!initialized)
        {
            InitializeTrain();
        }
        
        if (target == null) return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            target.position,
            speed * Time.deltaTime
        );

        if (lockZPosition)
        {
            Vector3 position = transform.position;
            position.z = zPosition;
            transform.position = position;
        }

        if (Vector3.Distance(transform.position, target.position) < 0.5f)
        {
            if (stopAtPointB && target == pointB)
            {
                stoppedAtStation = true;
                target = null;
                ReachedStationStop?.Invoke();
                return;
            }

            target = (target == pointA) ? pointB : pointA;
        }
    }
    
    private void InitializeTrain()
    {
        // Auto-find PointA and PointB if not set
        if (pointA == null)
        {
            pointA = transform.Find("PointA");
        }
        if (pointB == null)
        {
            pointB = transform.Find("PointB");
        }
        
        if (pointA == null || pointB == null)
        {
            Debug.LogError("TrainMover: PointA and PointB must be assigned or exist as child objects");
            return;
        }
        
        // Start at PointA, move to PointB
        transform.position = pointA.position;
        target = pointB;
        initialized = true;
        Debug.Log("TrainMover: Initialized and starting movement to PointB");
    }

    public void DepartFromStation()
    {
        if (!stoppedAtStation)
        {
            return;
        }

        stoppedAtStation = false;
        target = pointA;
    }
}
