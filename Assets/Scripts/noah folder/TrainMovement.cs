using UnityEngine;

public class TrainMovement : MonoBehaviour
{
    [SerializeField] private Transform worldContainer;
    [SerializeField] private TrainMover trainMover;
    
    private Vector3 chunkWorldStartPosition;      
    private float chunkTravelDistance;             
    
    public void InitializeChunkTravel(float travelDistance)
    {
        
        chunkWorldStartPosition = worldContainer.position;
        
      
        chunkTravelDistance = travelDistance;
        
        Debug.Log($"TrainMovement: Initializing chunk travel for distance {travelDistance}");
    }
    
    private void Update()
    {
      
        if (trainMover == null || trainMover.MovementStatus != TrainMovementStatus.Travelling)
            return;
        
       
        float travelProgress = trainMover.TravelProgress;
        
       
        float chunkDistanceMoved = chunkTravelDistance * travelProgress;
        
        
        worldContainer.position = chunkWorldStartPosition - Vector3.right * chunkDistanceMoved;
    }
}