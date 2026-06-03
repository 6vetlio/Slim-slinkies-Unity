using UnityEngine;

public class TrainMovement : MonoBehaviour
{
    [SerializeField] private Transform worldContainer;
    [SerializeField] private TrainMover trainMover;
    
    private Vector3 chunkWorldStartPosition;      // Where world was when travel started
    private float chunkTravelDistance;             // Total distance world needs to move
    
    public void InitializeChunkTravel(float travelDistance)
    {
        // Called when travel STARTS
        // Save the world's current position
        chunkWorldStartPosition = worldContainer.position;
        
        // Remember how far the world needs to move
        chunkTravelDistance = travelDistance;
        
        Debug.Log($"TrainMovement: Initializing chunk travel for distance {travelDistance}");
    }
    
    private void Update()
    {
        // Only run if train is travelling
        if (trainMover == null || trainMover.MovementStatus != TrainMovementStatus.Travelling)
            return;
        
        // Get progress (0.0 to 1.0) from TrainMover
        float travelProgress = trainMover.TravelProgress;
        
        // Calculate how far to move the world
        float chunkDistanceMoved = chunkTravelDistance * travelProgress;
        
        // Move the world LEFT by this amount
        worldContainer.position = chunkWorldStartPosition - Vector3.right * chunkDistanceMoved;
    }
}