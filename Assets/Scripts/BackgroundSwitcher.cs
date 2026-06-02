using UnityEngine;

public class SceneSwitcher : MonoBehaviour
{
    public GameObject GreenHillScene;
    public GameObject WheatFieldScene;

    void Start()
    {
        GreenHillScene.SetActive(true);
        WheatFieldScene.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            GreenHillScene.SetActive(true);
            WheatFieldScene.SetActive(false);
        }

        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            GreenHillScene.SetActive(false);
            WheatFieldScene.SetActive(true);
        }
    }
}