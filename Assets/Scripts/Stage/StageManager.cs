using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>스테이지 플레이 흐름: 인트로 → 플레이(타이머) → 클리어/실패 → 아웃트로 → 결과</summary>
public class StageManager : MonoBehaviour
{
    public static StageManager I { get; private set; }

    [Header("씬 참조")]
    public PlayerController player;
    public BugTerminal[] terminals;
    public DialogueUI dialogueUI;             // 없어도 동작

    [Header("에디터에서 Stage 씬만 단독 실행할 때 사용")]
    public StageData debugStage;
    public Difficulty debugDifficulty = Difficulty.Normal;

    public StageData Data { get; private set; }
    public Difficulty Diff { get; private set; }
    public float TimeLeft { get; private set; }
    public bool Running { get; private set; }

    public event Action<float> TimeChanged;
    public event Action<bool> StageEnded;     // true = 클리어

    readonly List<MonsterBase> monsters = new List<MonsterBase>();

    void Awake() { I = this; if (dialogueUI == null) dialogueUI = DialogueUI.CreateRuntime(); }

    IEnumerator Start()
    {
        var gm = GameManager.I;
        Data = (gm != null && gm.CurrentStage != null) ? gm.CurrentStage : debugStage;
        Diff = (gm != null && gm.CurrentStage != null) ? gm.CurrentDifficulty : debugDifficulty;
        if (Data == null) Data = FallbackStage(gm);

        TimeLeft = Data.TimeFor(Diff);
        foreach (var t in terminals) t.Init(Data.GoalFor(Diff));
        player.Health.Died += OnPlayerDied;
        TimeChanged?.Invoke(TimeLeft);

        // 스테이지 시작 스토리
        if (dialogueUI != null && Data.intro != null && Data.intro.Length > 0)
        {
            player.InputLocked = true;
            yield return dialogueUI.Play(Data.intro);
            player.InputLocked = false;
        }

        SpawnMonsters();
        Running = true;
    }

    /// <summary>GameManager·debugStage 둘 다 없을 때: 등록된 스테이지 중 가장 앞 → Resources → 기본값</summary>
    StageData FallbackStage(GameManager gm)
    {
        IEnumerable<StageData> pool = (gm != null && gm.allStages != null) ? gm.allStages : null;
        StageData first = FirstStage(pool) ?? FirstStage(Resources.LoadAll<StageData>(""));
        if (first == null)
        {
            first = ScriptableObject.CreateInstance<StageData>();
            first.monsters = new MonsterSpawn[0];
        }
        Debug.LogWarning($"[StageManager] 스테이지 데이터가 없어 '{first.displayName}'(으)로 진행합니다.");
        return first;
    }

    static StageData FirstStage(IEnumerable<StageData> stages)
    {
        StageData best = null;
        if (stages == null) return null;
        foreach (var s in stages)
        {
            if (s == null) continue;
            if (best == null || s.chapter < best.chapter || (s.chapter == best.chapter && s.stage < best.stage)) best = s;
        }
        return best;
    }

    void SpawnMonsters()
    {
        if (Data.monsters == null) return;
        foreach (var m in Data.monsters)
        {
            if (m == null || m.prefab == null) continue;
            var go = Instantiate(m.prefab, m.spawnPos, Quaternion.identity);
            var mon = go.GetComponent<MonsterBase>();
            mon.Init(player.transform, m.patternCount);
            monsters.Add(mon);
        }
    }

    void Update()
    {
        if (!Running) return;

        TimeLeft -= Time.deltaTime;
        TimeChanged?.Invoke(Mathf.Max(0f, TimeLeft));

        if (AllTerminalsDone()) End(true);
        else if (TimeLeft <= 0f) End(false);
    }

    bool AllTerminalsDone()
    {
        foreach (var t in terminals) if (!t.IsDone) return false;
        return true;
    }

    void OnPlayerDied() { if (Running) End(false); }

    void End(bool cleared)
    {
        Running = false;
        player.InputLocked = true;
        foreach (var m in monsters) if (m) m.Halt();

        if (cleared)
        {
            GameManager.I?.SaveClear(Data, Diff);
            if (Data.chapter == 1 && Data.stage == 1) AchievementManager.Unlock("first_clear");
            if (player.Health.Hp == player.Health.MaxHp) AchievementManager.Unlock("no_damage");
        }
        StartCoroutine(EndRoutine(cleared));
    }

    IEnumerator EndRoutine(bool cleared)
    {
        if (cleared && dialogueUI != null && Data.outro != null && Data.outro.Length > 0)
            yield return dialogueUI.Play(Data.outro);
        StageEnded?.Invoke(cleared);
    }
}
