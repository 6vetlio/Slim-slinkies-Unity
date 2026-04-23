using UnityEngine;

public class ManualTrainController : MonoBehaviour
{
    public Transform pointA;
    public Transform pointB;
    public float speed = 5f;
    public bool stopAtPointB = true;
    
    private Transform target;
    private bool stoppedAtStation;
    private bool movingToPointB = true;
    
    public bool IsStoppedAtStation => stoppedAtStation;
    public event System.Action ReachedStationStop;
    
    void Start()
    {
        // Auto-find points if not set
        if (pointA == null)
        {
            pointA = transform.Find("PointA");
        }
        if (pointB == null)
        {
            pointB = transform.Find("PointB");
        }
        
        if (pointA != null && pointB != null)
        {
            // Start at PointA, move to PointB
            transform.position = pointA.position;
            target = pointB;
            movingToPointB = true;
            Debug.Log("ManualTrainController: Starting train movement from PointA to PointB");
        }
        else
        {
            Debug.LogError("ManualTrainController: Could not find PointA and PointB");
        }
    }
    
    void Update()
    {
        if (target == null) return;
        
        // Move towards target
        transform.position = Vector3.MoveTowards(
            transform.position,
            target.position,
            speed * Time.deltaTime
        );
        
        // Check if reached target
        if (Vector3.Distance(transform.position, target.position) < 0.01f)
        {
            if (stopAtPointB && movingToPointB && target == pointB)
            {
                // Reached PointB - stop and trigger event
                stoppedAtStation = true;
                target = null;
                ReachedStationStop?.Invoke();
                Debug.Log("ManualTrainController: Reached PointB and stopped");
                return;
            }
            
            // Switch targets
            if (target == pointA)
            {
                target = pointB;
                movingToPointB = true;
            }
            else
            {
                target = pointA;
                movingToPointB = false;
            }
        }
    }
    
    public void DepartFromStation()
    {
        if (!stoppedAtStation) return;
        
        stoppedAtStation = false;
        target = pointA;
        movingToPointB = false;
        Debug.Log("ManualTrainController: Departing from station to PointA");
    }
    
    public void ForceMoveToPointB()
    {
        stoppedAtStation = false;
        target = pointB;
        movingToPointB = true;
        Debug.Log("ManualTrainController: Forced movement to PointB");
    }
}
