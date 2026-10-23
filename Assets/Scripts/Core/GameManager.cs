using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>씬 간 유지되는 싱글톤. 선택한 스테이지/난이도, 클리어 저장, 해금 판정.</summary>
public class GameManager : MonoBehaviour
{
    public static GameManager I { get; private set; }

    /// <summary>챕터별 HP: 챕터1=3 / 챕터2=2 / 챕터3=1</summary>
    public static readonly int[] ChapterHp = { 3, 2, 1 };

    [Tooltip("모든 챕터의 StageData를 등록")]
    public StageData[] allStages;

    public StageData CurrentStage { get; private set; }
    public Difficulty CurrentDifficulty { get; private set; }

    void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
        if (GetComponent<DebugMenu>() == null) gameObject.AddComponent<DebugMenu>();
        SoundEffectManager.EnsureInstance();
    }

    public void StartStage(StageData stage, Difficulty diff)
    {
        CurrentStage = stage;
        CurrentDifficulty = diff;
        SceneManager.LoadScene(stage.sceneName);
    }

    // ---------- 저장 / 해금 ----------
    static string Key(StageData s, Difficulty d) => $"clear_{s.chapter}_{s.stage}_{(int)d}";

    /// <summary>클리어 별 (0~3) 최고 기록</summary>
    public static int Stars(StageData s) => PlayerPrefs.GetInt($"stars_{s.chapter}_{s.stage}", 0);
    public static void SaveStars(StageData s, int stars)
    {
        if (stars <= Stars(s)) return;
        PlayerPrefs.SetInt($"stars_{s.chapter}_{s.stage}", Mathf.Clamp(stars, 0, 3)); PlayerPrefs.Save();
    }

    public void SaveClear(StageData s, Difficulty d)
    {
        PlayerPrefs.SetInt(Key(s, d), 1);
        PlayerPrefs.Save();
    }

    /// <summary>쉬움/보통/어려움 중 하나라도 클리어했는가</summary>
    public bool IsCleared(StageData s)
        => System.Enum.GetValues(typeof(Difficulty)).Cast<Difficulty>()
                .Any(d => PlayerPrefs.GetInt(Key(s, d), 0) == 1);

    /// <summary>
    /// 이전 스테이지를 어떤 난이도로든 클리어하면 다음 스테이지 개방.
    /// 챕터 첫 스테이지는 이전 챕터 5스테이지를 모두 클리어해야 개방 (기획서 p.12, 챕터당 5스테이지로 수정).
    /// </summary>
    public bool IsUnlocked(StageData s)
    {
        if (s.stage <= 0 || GameSettings.DevMode) return true;   // 튜토리얼은 처음부터 열림 · 개발자 모드면 전부 열림
        var prev = allStages.Where(x => x != null && x.chapter == s.chapter && x.stage >= 1 && x.stage < s.stage)   // 튜토리얼(0)은 해금 조건 아님
                            .OrderByDescending(x => x.stage).FirstOrDefault();
        if (prev != null) return IsCleared(prev);
        if (s.chapter <= 1) return true;

        var prevChapter = allStages.Where(x => x != null && x.chapter == s.chapter - 1 && x.stage >= 1).ToArray();
        return prevChapter.Length >= StageManager.StagesPerChapter && prevChapter.All(IsCleared);
    }
}
