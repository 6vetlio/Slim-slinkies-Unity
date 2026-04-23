using UnityEngine;

public class UIParallax : MonoBehaviour
{
    [Header("Camera")]
    public Camera cameraToFollow;

    [Header("Parallax Strength")]
    [Range(-2f, 2f)] public float parallaxStrengthX = 0.5f;
    [Range(-2f, 2f)] public float parallaxStrengthY = 0f;
    [Range(-2f, 2f)] public float parallaxStrengthZ = 0.3f;
    public float scrollMultiplier = 100f;

    [Header("Repeat")]
    public bool wrapHorizontally = true;
    public float tileWidth = 1920f;
    public float tileSpacing = 0f;

    [Header("Tile Copies")]
    public bool buildCopiesOnStart = false;
    public RectTransform tileToCopy;
    [Min(0)] public int copiesLeft = 1;
    [Min(0)] public int copiesRight = 1;
    public bool destroyOldGeneratedCopies = true;

    private RectTransform _rect;
    private Vector3 _startLocalPos;
    private float _currentOffsetX;
    private Vector3 _lastCamPos;
    private Camera _cam;

    void Start()
    {
        _cam = cameraToFollow != null ? cameraToFollow : Camera.main;

        if (_cam == null)
        {
            Debug.LogError("UIParallax: Camera.main not found!");
            return;
        }

        _rect = GetComponent<RectTransform>();
        _startLocalPos = _rect.localPosition;
        _lastCamPos = _cam.transform.position;

        if (buildCopiesOnStart)
        {
            BuildTileCopies();
        }
    }

    void LateUpdate()
    {
        if (_cam == null) return;

        Vector3 delta = _cam.transform.position - _lastCamPos;
        _lastCamPos = _cam.transform.position;

        _currentOffsetX += (delta.x * parallaxStrengthX
                         + delta.y * parallaxStrengthY
                         + delta.z * parallaxStrengthZ) * scrollMultiplier;

        if (wrapHorizontally && tileWidth > 0f)
        {
            _currentOffsetX %= tileWidth + tileSpacing;
        }

        _rect.localPosition = new Vector3(
            _startLocalPos.x + _currentOffsetX,
            _startLocalPos.y,
            _startLocalPos.z
        );
    }

    [ContextMenu("Build Tile Copies")]
    public void BuildTileCopies()
    {
        RectTransform sourceTile = tileToCopy != null ? tileToCopy : _rect;

        if (sourceTile == null)
        {
            sourceTile = GetComponent<RectTransform>();
        }

        if (sourceTile == null)
        {
            Debug.LogWarning("UIParallax: No tile RectTransform found.");
            return;
        }

        if (destroyOldGeneratedCopies)
        {
            DestroyGeneratedCopies();
        }

        float step = tileWidth + tileSpacing;

        for (int i = 1; i <= copiesLeft; i++)
        {
            CreateCopy(sourceTile, -step * i, "Generated Left " + i);
        }

        for (int i = 1; i <= copiesRight; i++)
        {
            CreateCopy(sourceTile, step * i, "Generated Right " + i);
        }
    }

    [ContextMenu("Destroy Generated Copies")]
    public void DestroyGeneratedCopies()
    {
        RectTransform sourceTile = tileToCopy != null ? tileToCopy : _rect;

        if (sourceTile == null)
        {
            sourceTile = GetComponent<RectTransform>();
        }

        Transform copyParent = sourceTile != null && sourceTile.parent != null ? sourceTile.parent : transform;

        for (int i = copyParent.childCount - 1; i >= 0; i--)
        {
            Transform child = copyParent.GetChild(i);

            if (child.name.StartsWith("Parallax Generated"))
            {
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }
    }

    private void CreateCopy(RectTransform sourceTile, float localOffsetX, string label)
    {
        RectTransform copy = Instantiate(sourceTile, sourceTile.parent);
        copy.name = "Parallax Generated " + label;
        copy.localScale = sourceTile.localScale;
        copy.localRotation = sourceTile.localRotation;
        copy.anchoredPosition = sourceTile.anchoredPosition + Vector2.right * localOffsetX;

        UIParallax copiedParallax = copy.GetComponent<UIParallax>();
        if (copiedParallax != null)
        {
            if (Application.isPlaying)
            {
                Destroy(copiedParallax);
            }
            else
            {
                DestroyImmediate(copiedParallax);
            }
        }
    }
}
