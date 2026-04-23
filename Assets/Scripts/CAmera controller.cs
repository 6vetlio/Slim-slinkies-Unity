using UnityEngine;

public class CameraCycle : MonoBehaviour
{
    public Camera[] cameras;
    private int index = 0;

    void Start()
    {
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

    void ActivateCamera(int i)
    {
        for (int j = 0; j < cameras.Length; j++)
        {
            cameras[j].gameObject.SetActive(j == i);
        }
    }
}