using UnityEngine;

public class TrainMovement : MonoBehaviour
{
    [SerializeField] private float trainSpeed = 5f;
    [SerializeField] private Transform worldContainer;
    
    private void Update()
    {
       
        worldContainer.position -= Vector3.right * trainSpeed * Time.deltaTime;
    }
}