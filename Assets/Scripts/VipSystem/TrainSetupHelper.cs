using UnityEngine;

public class TrainSetupHelper : MonoBehaviour
{
    void Start()
    {
        // Find train and its movement points
        TrainMover trainMover = FindObjectOfType<TrainMover>();
        if (trainMover != null)
        {
            // Try to find PointA and PointB as children of train
            Transform pointA = trainMover.transform.Find("PointA");
            Transform pointB = trainMover.transform.Find("PointB");
            
            if (pointA != null && pointB != null)
            {
                // Use the public fields directly
                trainMover.pointA = pointA;
                trainMover.pointB = pointB;
                Debug.Log("TrainSetupHelper: Successfully set pointA and pointB references");
            }
            else
            {
                Debug.LogError("TrainSetupHelper: Could not find PointA and PointB children");
            }
        }
        else
        {
            Debug.LogError("TrainSetupHelper: Could not find TrainMover component");
        }
        
        // Destroy this helper after setup
        Destroy(this);
    }
}
