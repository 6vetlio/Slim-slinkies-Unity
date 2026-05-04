using UnityEngine;

public class UIParallax : MonoBehaviour
{
    [Header("Camera")]
    public Camera cameraToFollow;

    [Header("Parallax")]
    [Range(-2f, 2f)] public float parallaxStrengthX = 0.5f;
    public float scrollMultiplier = 4f;

    [Header("Wrap")]
    public bool wrapHorizontally = true;
    public float tileWidth = 1920f;

    RectTransform _rect;
    Vector3 _startLocalPos;
    float _currentOffsetX;
    Vector3 _lastCamPos;
    Camera _cam;

    void Start()
    {
        _cam = cameraToFollow != null ? cameraToFollow : Camera.main;
        if (_cam == null) return;

        _rect = GetComponent<RectTransform>();
        _startLocalPos = _rect.localPosition;
        _lastCamPos = _cam.transform.position;
    }

    void LateUpdate()
    {
        if (_cam == null || _rect == null) return;

        Vector3 delta = _cam.transform.position - _lastCamPos;
        _lastCamPos = _cam.transform.position;

        _currentOffsetX += delta.x * parallaxStrengthX * scrollMultiplier;

        if (wrapHorizontally && tileWidth > 0f)
            _currentOffsetX = Mathf.Repeat(_currentOffsetX, tileWidth);

        _rect.localPosition = new Vector3(
            _startLocalPos.x + _currentOffsetX,
            _startLocalPos.y,
            _startLocalPos.z
        );
    }
}
