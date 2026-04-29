using UnityEngine;

public class CameraCycle : MonoBehaviour
{
    public Camera[] cameras;
    public bool followTarget = false;
    public Transform targetToFollow;
    public Vector3 followOffset = new Vector3(0f, 0f, -10f);
    public float followSmoothness = 8f;
    private int index = 0;

    void Start()
    {
        if (followTarget && targetToFollow == null)
        {
            GameObject train = GameObject.Find("Train");
            if (train != null)
            {
                targetToFollow = train.transform;
            }
        }

        if (Camera.main != null)
        {
            if (followTarget && targetToFollow != null)
            {
                Camera.main.transform.position = targetToFollow.position + followOffset;
            }

            Camera.main.orthographicSize = 50f;
        }
        ActivateCamera(index);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            index = (index + 1) % cameras.Length;
            ActivateCamera(index);
        }
    }

    void LateUpdate()
    {
        if (!followTarget || targetToFollow == null || Camera.main == null)
        {
            return;
        }

        Vector3 desiredPosition = targetToFollow.position + followOffset;
        Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, desiredPosition, followSmoothness * Time.deltaTime);
    }

    void ActivateCamera(int i)
    {
        for (int j = 0; j < cameras.Length; j++)
        {
            cameras[j].gameObject.SetActive(j == i);
        }
    }
}