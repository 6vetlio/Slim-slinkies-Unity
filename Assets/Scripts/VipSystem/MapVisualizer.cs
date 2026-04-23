using UnityEngine;
using UnityEngine.UI;

public class MapVisualizer : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private GameObject stationsContainer;
    [SerializeField] private GameObject linePrefab;
    [SerializeField] private GameObject trainIndicatorPrefab;
    [SerializeField] private TrainMover trainMover;

    private GameObject trainIndicator;
    private RectTransform trainIndicatorRect;

    void Start()
    {
        if (stationsContainer == null)
        {
            stationsContainer = transform.Find("Stations")?.gameObject;
        }

        if (trainMover == null)
        {
            trainMover = FindObjectOfType<TrainMover>();
        }

        CreateMapLines();
        CreateTrainIndicator();
    }

    void Update()
    {
        if (trainIndicator != null && trainMover != null)
        {
            UpdateTrainIndicator();
        }
    }

    private void CreateMapLines()
    {
        if (stationsContainer == null) return;

        var stations = stationsContainer.GetComponentsInChildren<Station>();
        if (stations.Length < 2) return;

        // Create single horizontal progress bar line
        GameObject line = new GameObject("ProgressBarLine");
        line.transform.SetParent(stationsContainer.transform, false);

        RectTransform lineRect = line.AddComponent<RectTransform>();
        Image lineImage = line.AddComponent<Image>();
        lineImage.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);

        // Horizontal line spanning the width
        lineRect.anchoredPosition = new Vector2(0, 0);
        lineRect.sizeDelta = new Vector2(700f, 8f);
        lineRect.pivot = new Vector2(0.5f, 0.5f);
        lineRect.rotation = Quaternion.Euler(0, 0, 0);

        // Position stations along the line
        float startX = -300f;
        float spacing = 200f;

        for (int i = 0; i < stations.Length; i++)
        {
            RectTransform stationRect = stations[i].GetComponent<RectTransform>();
            if (stationRect != null)
            {
                stationRect.anchoredPosition = new Vector2(startX + (i * spacing), 0);
            }
        }
    }

    private void CreateTrainIndicator()
    {
        if (trainIndicatorPrefab != null)
        {
            trainIndicator = Instantiate(trainIndicatorPrefab, stationsContainer.transform);
        }
        else
        {
            trainIndicator = new GameObject("TrainIndicator");
            trainIndicator.transform.SetParent(stationsContainer.transform, false);

            trainIndicatorRect = trainIndicator.AddComponent<RectTransform>();
            trainIndicatorRect.sizeDelta = new Vector2(12f, 12f);
            trainIndicatorRect.pivot = new Vector2(0.5f, 0.5f);

            Image indicatorImage = trainIndicator.AddComponent<Image>();
            indicatorImage.color = Color.red;
        }

        trainIndicatorRect = trainIndicator.GetComponent<RectTransform>();
    }

    private void UpdateTrainIndicator()
    {
        if (trainMover == null)
        {
            Debug.Log("MapVisualizer: trainMover is null");
            return;
        }
        if (trainMover.pointA == null || trainMover.pointB == null) return;
        if (trainIndicatorRect == null) return;

        // Get train position relative to stations
        Vector3 trainPos = trainMover.transform.position;
        Vector3 pointAPos = trainMover.pointA.position;
        Vector3 pointBPos = trainMover.pointB.position;

        // Calculate progress (0 to 1)
        float totalDistance = Vector3.Distance(pointAPos, pointBPos);
        float currentDistance = Vector3.Distance(pointAPos, trainPos);
        float progress = Mathf.Clamp01(currentDistance / totalDistance);

        // Map progress to horizontal line (from -300 to 300)
        float startX = -300f;
        float endX = 300f;
        float indicatorX = Mathf.Lerp(startX, endX, progress);

        trainIndicatorRect.anchoredPosition = new Vector2(indicatorX, 0);
    }
}
