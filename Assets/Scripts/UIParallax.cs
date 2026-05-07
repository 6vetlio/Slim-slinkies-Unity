using UnityEngine;

public class UIParallax : MonoBehaviour
{
    [Header("Train")]
    [SerializeField] private TrainMover trainMover;
    public Camera cameraToFollow;

    [Header("Parallax")]
    [Range(-2f, 2f)] public float parallaxStrengthX = 0.5f;
    public float scrollMultiplier = 4f;
    [SerializeField] private bool moveOnlyWhileTravelling = true;
    [SerializeField] private float idleSpeed = 0f;

    [Header("Wrap")]
    public bool wrapHorizontally = true;
    public float tileWidth = 1920f;

    private RectTransform rect;
    private Vector3 startLocalPos;
    private RectTransform[] tiles;
    private Vector2[] tileStartPositions;
    private float wrapMinX;
    private float wrapSpan;
    private float currentOffsetX;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        CacheTiles();
    }

    private void Start()
    {
        if (rect == null) return;

        if (trainMover == null)
            trainMover = FindFirstObjectByType<TrainMover>();

        startLocalPos = rect.localPosition;
        CacheTiles();
    }

    private void LateUpdate()
    {
        if (rect == null) return;

        // Keep the layer container anchored; only its editable child tiles scroll.
        rect.localPosition = startLocalPos;

        bool isTravelling = trainMover != null && trainMover.MovementStatus == TrainMovementStatus.Travelling;
        if (!isTravelling && moveOnlyWhileTravelling)
        {
            return;
        }

        float speed = isTravelling
            ? Mathf.Abs(parallaxStrengthX) * scrollMultiplier * 100f
            : idleSpeed;

        if (Mathf.Approximately(speed, 0f))
        {
            return;
        }

        if (tiles == null || tiles.Length != transform.childCount)
        {
            CacheTiles();
        }

        if (tiles == null || tiles.Length == 0)
        {
            return;
        }

        currentOffsetX += speed * Time.deltaTime;

        if (!wrapHorizontally || wrapSpan <= 0f)
        {
            for (int i = 0; i < tiles.Length; i++)
            {
                if (tiles[i] == null) continue;
                tiles[i].anchoredPosition = tileStartPositions[i] + Vector2.left * currentOffsetX;
            }

            return;
        }

        float offset = Mathf.Repeat(currentOffsetX, wrapSpan);
        for (int i = 0; i < tiles.Length; i++)
        {
            if (tiles[i] == null) continue;

            float x = tileStartPositions[i].x - offset;
            x = wrapMinX + Mathf.Repeat(x - wrapMinX, wrapSpan);
            tiles[i].anchoredPosition = new Vector2(x, tileStartPositions[i].y);
        }
    }

    private void CacheTiles()
    {
        int childCount = transform.childCount;
        tiles = new RectTransform[childCount];
        tileStartPositions = new Vector2[childCount];

        wrapMinX = float.PositiveInfinity;
        float wrapMaxX = float.NegativeInfinity;

        for (int i = 0; i < childCount; i++)
        {
            tiles[i] = transform.GetChild(i) as RectTransform;
            if (tiles[i] == null) continue;

            tileStartPositions[i] = tiles[i].anchoredPosition;

            float width = Mathf.Abs(tiles[i].rect.width);
            if (width <= 0f)
            {
                width = Mathf.Abs(tiles[i].sizeDelta.x);
            }

            float halfWidth = width * 0.5f;
            wrapMinX = Mathf.Min(wrapMinX, tileStartPositions[i].x - halfWidth);
            wrapMaxX = Mathf.Max(wrapMaxX, tileStartPositions[i].x + halfWidth);
        }

        if (float.IsInfinity(wrapMinX) || float.IsInfinity(wrapMaxX))
        {
            wrapMinX = 0f;
            wrapSpan = Mathf.Max(1f, tileWidth);
            return;
        }

        wrapSpan = Mathf.Max(1f, wrapMaxX - wrapMinX);
    }
}
