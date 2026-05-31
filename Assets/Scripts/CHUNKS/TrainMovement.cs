using UnityEngine;

public class TrainMovement : MonoBehaviour
{
    [SerializeField] private float trainSpeed = 10f; // Units per second
    [SerializeField] private Transform worldContainer; // Parent of all chunks
    
    private void Update()
    {
        // Move the entire world left, making it seem like the train is moving right
        worldContainer.position -= Vector3.right * trainSpeed * Time.deltaTime;
    }
}