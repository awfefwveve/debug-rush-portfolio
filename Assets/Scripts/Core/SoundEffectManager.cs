using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 효과음·브금 재생 진입점. 씬 전환 뒤에도 유지됩니다. 음량은 GameSettings(설정창) 값을 따릅니다.
/// 브금(Resources/Bgm): 스테이지 = battle(전투상황 노래), 그 외 화면(시작·스테이지 선택·업적) = title(시작 노래). 반복 재생.
/// </summary>
public class SoundEffectManager : MonoBehaviour
{
    public static SoundEffectManager Instance { get; private set; }
    public AudioSource source;
    public AudioSource bgmSource;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;
        PlayBgm(Resources.Load<AudioClip>(scene.name == "Stage" ? "Bgm/battle" : "Bgm/title"));
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this; DontDestroyOnLoad(gameObject);
        if (source == null) source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        if (bgmSource == null) bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.playOnAwake = false;
        bgmSource.loop = true;
        ApplyVolumes();
        GameSettings.Changed += ApplyVolumes;
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
    /// <summary>브금 교체 (같은 곡이면 이어서 재생)</summary>
    public static void PlayBgm(AudioClip clip)
    {
        EnsureInstance();
        var s = Instance.bgmSource;
        if (s.clip == clip && s.isPlaying) return;
        s.clip = clip;
        if (clip != null) s.Play(); else s.Stop();
    }
    void ApplyVolumes()
    {
        if (source) source.volume = GameSettings.SfxVolume;
        if (bgmSource) bgmSource.volume = GameSettings.BgmVolume;
    }
    void OnDestroy()
    {
        GameSettings.Changed -= ApplyVolumes;
        if (Instance == this) Instance = null;
    }
}
