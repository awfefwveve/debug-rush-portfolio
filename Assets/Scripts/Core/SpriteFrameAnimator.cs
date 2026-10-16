using UnityEngine;

/// <summary>스프라이트 프레임 배열을 순서대로 재생 (아트 팀 PNG 프레임용). Animator 없이 동작.</summary>
public class SpriteFrameAnimator : MonoBehaviour
{
    public SpriteRenderer target;
    public Sprite[] frames;
    public float fps = 12f;
    public bool loop = true;
    [Tooltip("true면 Time.timeScale 영향 없음")] public bool unscaled = false;

    float t;
    Sprite[] playing;

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
        if (newFrames == playing && !restart) return;
        playing = newFrames;
        t = 0f;
        Apply(0);
    }

    /// <summary>재생하지 않고 특정 프레임 표시</summary>
    public void Hold(Sprite[] set, int index)
    {
        playing = set;
        t = 0f;
        Apply(index);
    }

    public bool Paused { get; set; }

    void Update()
    {
        if (Paused || playing == null || playing.Length == 0 || fps <= 0f) return;
        t += unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
        int i = Mathf.FloorToInt(t * fps);
        Apply(loop ? i % playing.Length : Mathf.Min(i, playing.Length - 1));
    }

    void Apply(int i)
    {
        if (target == null || playing == null || playing.Length == 0) return;
        var s = playing[Mathf.Clamp(i, 0, playing.Length - 1)];
        if (s != null) target.sprite = s;
    }
}
