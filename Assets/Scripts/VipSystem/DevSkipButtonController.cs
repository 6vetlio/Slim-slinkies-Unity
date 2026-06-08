using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Wires a pre-placed "skip travel" dev button to <see cref="TrainMover.SkipToDestination"/>.
///
/// The button is now an authored Canvas object (no longer built from scratch at
/// runtime — that violated the "no runtime-spawned objects" rule). Place a Button
/// in the scene, assign it to <see cref="devButton"/>, and put this component on it
/// (or anywhere persistent).
///
/// Setup (6vetlio, live in Unity):
///  - Create a UI Button in the HUD canvas labelled "dev button".
///  - Assign it to <see cref="devButton"/> and assign/auto-find <see cref="trainMover"/>.
/// </summary>
public class DevSkipButtonController : MonoBehaviour
{
    [SerializeField] private TrainMover trainMover;
    [Tooltip("The pre-placed dev button in the scene. No button is created at runtime.")]
    [SerializeField] private Button devButton;

    private void Start()
    {
        if (trainMover == null)
        {
            trainMover = FindFirstObjectByType<TrainMover>();
        }

        if (devButton == null)
        {
            devButton = GetComponent<Button>();
        }

        if (devButton != null)
        {
            devButton.onClick.RemoveListener(SkipTravel);
            devButton.onClick.AddListener(SkipTravel);
        }
        else
        {
            Debug.LogWarning("[DevSkipButtonController] No devButton assigned — place a Button in the scene and assign it.");
        }
    }

    private void SkipTravel()
    {
        if (trainMover != null)
        {
            trainMover.SkipToDestination();
        }
    }
}
