using UnityEngine;
using UnityEngine.UI;

public class UISpriteAnimator : MonoBehaviour
{
    public Sprite[] frames;
    public float fps = 10f;
    public bool loop = true;
    public bool playOnStart = true;
    public bool flipX;

    Image _image;
    int _index;
    float _t;
    bool _playing;

    void Awake()
    {
        _image = GetComponent<Image>();
    }

    void Start()
    {
        if (flipX)
            ((RectTransform)transform).localScale = new Vector3(-1f, 1f, 1f);
        _playing = playOnStart;
        if (frames != null && frames.Length > 0 && _image != null)
            _image.sprite = frames[0];
    }

    void Update()
    {
        if (!_playing || frames == null || frames.Length == 0 || _image == null) return;
        _t += Time.deltaTime;
        float frameDur = 1f / Mathf.Max(0.01f, fps);
        while (_t >= frameDur)
        {
            _t -= frameDur;
            _index++;
            if (_index >= frames.Length)
            {
                if (!loop)
                {
                    _playing = false;
                    return;
                }
                _index = 0;
            }
            _image.sprite = frames[_index];
        }
    }

    public void Play() => _playing = true;
    public void Stop() => _playing = false;
}
