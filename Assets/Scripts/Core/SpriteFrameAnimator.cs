using UnityEngine;

/// <summary>스프라이트 프레임 배열을 순서대로 재생 (아트 팀 PNG 프레임용). Animator 없이 동작.</summary>
public class SpriteFrameAnimator : MonoBehaviour
{
    public SpriteRenderer target;
    public Sprite[] frames;
    public float fps = 12f;
    public bool loop = true;
    [Tooltip("재생이 끝나면 오브젝트 삭제 (1회용 이펙트)")] public bool destroyOnEnd = false;

    float t;
    Sprite[] playing;
    bool holding;

    public bool Paused { get; set; }
    public bool Finished => !loop && playing != null && t * fps >= playing.Length;

    void Awake()
    {
        if (target == null) target = GetComponentInChildren<SpriteRenderer>();
        playing = frames;
        Apply(0);
    }

    /// <summary>다른 프레임 묶음으로 전환. 같은 묶음이면 이어서 재생.</summary>
    public void Play(Sprite[] newFrames, float newFps, bool newLoop, bool restart = false)
    {
        fps = newFps;
        loop = newLoop;
        bool same = newFrames == playing && !holding;
        holding = false;
        if (same && !restart) return;
        playing = newFrames;
        t = 0f;
        Apply(0);
    }

    /// <summary>재생하지 않고 특정 프레임 고정 표시</summary>
    public void Hold(Sprite[] set, int index)
    {
        playing = set;
        holding = true;
        t = 0f;
        Apply(index);
    }

    void Update()
    {
        if (Paused || holding || playing == null || playing.Length == 0 || fps <= 0f) return;
        t += Time.deltaTime;
        int i = Mathf.FloorToInt(t * fps);
        if (!loop && i >= playing.Length && destroyOnEnd) { Destroy(gameObject); return; }
        Apply(loop ? i % playing.Length : Mathf.Min(i, playing.Length - 1));
    }

    void Apply(int i)
    {
        if (target == null || playing == null || playing.Length == 0) return;
        var s = playing[Mathf.Clamp(i, 0, playing.Length - 1)];
        if (s != null) target.sprite = s;
    }

    /// <summary>1회용 프레임 이펙트 생성</summary>
    public static SpriteFrameAnimator Spawn(Sprite[] frames, Vector3 pos, float fps, int sortingOrder, float scale = 1f)
    {
        if (frames == null || frames.Length == 0) return null;
        var go = new GameObject("FX");
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = sortingOrder;
        var a = go.AddComponent<SpriteFrameAnimator>();
        a.target = sr; a.frames = frames; a.fps = fps; a.loop = false; a.destroyOnEnd = true;
        a.playing = frames; a.Apply(0);
        return a;
    }
}
