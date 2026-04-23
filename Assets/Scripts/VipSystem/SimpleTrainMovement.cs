using UnityEngine;

public class SimpleTrainMovement : MonoBehaviour
{
    public Transform pointA;
    public Transform pointB;
    public float speed = 5f;
    public bool stopAtPointB = true;
    
    private Transform target;
    private bool stoppedAtStation;
    
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
            target = pointB;
            Debug.Log("SimpleTrainMovement: Found PointA and PointB, starting movement");
        }
        else
        {
            Debug.LogError("SimpleTrainMovement: Could not find PointA and PointB");
        }
    }
    
    void Update()
    {
        if (target == null) return;
        
        transform.position = Vector3.MoveTowards(
            transform.position,
            target.position,
            speed * Time.deltaTime
        );
        
        if (Vector3.Distance(transform.position, target.position) < 0.01f)
        {
            if (stopAtPointB && target == pointB)
            {
                stoppedAtStation = true;
                target = null;
                ReachedStationStop?.Invoke();
                Debug.Log("SimpleTrainMovement: Reached station and stopped");
                return;
            }
            
            target = (target == pointA) ? pointB : pointA;
        }
    }
    
    public void DepartFromStation()
    {
        if (!stoppedAtStation) return;
        
        stoppedAtStation = false;
        target = pointA;
        Debug.Log("SimpleTrainMovement: Departing from station");
    }
}
