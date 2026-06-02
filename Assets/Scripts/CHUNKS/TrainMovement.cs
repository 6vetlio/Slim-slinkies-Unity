using UnityEngine;

public class TrainMovement : MonoBehaviour
{
    [SerializeField] private float trainSpeed = 5f;
    [SerializeField] private Transform worldContainer; // Parent of all chunks
    
    private void Update()
    {
        // Move the world left (chunks move left, train stays in place visually)
        worldContainer.position -= Vector3.right * trainSpeed * Time.deltaTime;
    }
}