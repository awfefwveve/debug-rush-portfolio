using UnityEngine;

/// <summary>효과음 재생 진입점. 씬 전환 뒤에도 유지됩니다.</summary>
public class SoundEffectManager : MonoBehaviour
{
    public static SoundEffectManager Instance { get; private set; }
    public AudioSource source;
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this; DontDestroyOnLoad(gameObject);
        if (source == null) source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
    }
    public static void EnsureInstance()
    {
        if (Instance != null) return;
        var root = new GameObject("SoundEffectManager");
        root.AddComponent<SoundEffectManager>();
    }
    public static void Play(AudioClip clip, float volume = 1f)
    {
        if (Instance != null && clip != null) Instance.source.PlayOneShot(clip, Mathf.Clamp01(volume));
    }
    void OnDestroy() { if (Instance == this) Instance = null; }
}
