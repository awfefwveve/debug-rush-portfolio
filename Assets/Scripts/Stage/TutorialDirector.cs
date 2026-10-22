using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 튜토리얼 스테이지(stage 0) 단계 안내. 화면 위쪽 한 줄 안내 → 그 행동을 하면 다음 단계.
/// ① 이동 ② 재료 집기 ③ 재료 그대로 단말기에 ④ 조합해서 단말기에 ⑤ Q 버리기 ⑥ Shift 대쉬 ⑦ 벌 등장 → 패링 ⑧ 남은 요구 채우면 클리어.
/// 단말기 요구사항은 단계에 맞게 정해 줌(① 기획 재료 → ② 시스템 기획서), 중간 단계(⑤~⑦)엔 단말기를 잠가 순서대로 진행.
/// </summary>
public class TutorialDirector : MonoBehaviour
{
    static readonly string[] Steps =
    {
        "<b>WASD</b> 또는 방향키로 움직여 보세요",
        "생성기 앞에서 <b>E</b>를 눌러 재료를 집어 보세요",
        "단말기 위에 뜬 재료를 들고 가서 <b>E</b>로 넣어 보세요",
        "재료 2개를 제작대에 넣고(<b>E</b>) 완성품을 꺼내 단말기에 넣어 보세요  ·  <b>Tab</b> 조합표",
        "재료를 하나 집은 뒤 <b>Q</b>로 버려 보세요",
        "<b>Shift</b>로 대쉬해 보세요",
        "벌이 나타났어요! <color=#C58BFF>보라색 탄</color>이 날아올 때 <b>Space</b>로 점프하면 패링!",
        "남은 단말기 요구를 채우면 클리어!",
    };
    const int StepParry = 6, StepLast = 7;
    const float ParryGiveUp = 45f;   // 패링을 못 해도 이 시간이 지나면 다음 단계

    static readonly ItemType[] LatePool = { ItemType.Planning, ItemType.Programming, ItemType.ContentDoc, ItemType.SystemDoc, ItemType.GameBuild };

    StageManager sm;
    PlayerController player;
    BugTerminal terminal;
    int step;
    float stepStart, doneAt = -1f;
    Vector2 startPos;
    bool dropped, parried, started;

    public void Setup(StageManager stage)
    {
        sm = stage;
        player = stage.player;
        terminal = stage.terminals != null && stage.terminals.Length > 0 ? stage.terminals[0] : null;
        if (terminal != null) terminal.requestOverride = NextRequest;
        player.Dropped += () => dropped = true;
        player.ParrySucceeded += () => parried = true;
    }

    // 생성기가 기획·프로그래밍 둘뿐이라 그 재료로 만들 수 있는 것만 요구
    List<ItemType> NextRequest()
    {
        int done = terminal != null ? terminal.Completed : 0;
        if (done == 0) return new List<ItemType> { ItemType.Planning };
        if (done == 1) return new List<ItemType> { ItemType.SystemDoc };      // 기획 + 프로그래밍
        return new List<ItemType> { LatePool[Random.Range(0, LatePool.Length)] };
    }

    void Update()
    {
        if (sm == null || !sm.Running) return;
        if (!started) { started = true; startPos = player.transform.position; Enter(0); }
        // ⑤~⑦은 잠금, ①~④에서 미리 2개 넘게 채워 바로 클리어되지 않게
        if (terminal != null) terminal.locked = (step >= 4 && step < StepLast) || (step < 4 && terminal.Completed >= 2);
        if (doneAt >= 0f)
        {
            if (Time.time - doneAt >= 0.8f && step < StepLast) Enter(step + 1);   // "좋아요!" 잠깐 보여 주고 다음
            return;
        }
        if (Done()) doneAt = Time.time;
    }

    void Enter(int s)
    {
        step = s; stepStart = Time.time; doneAt = -1f; dropped = false; parried = false;
        if (step == StepParry) sm.SpawnMonsters();
    }

    bool Done()
    {
        switch (step)
        {
            case 0: return Vector2.Distance(player.transform.position, startPos) > 1.5f;
            case 1: return player.Inventory.items.Count > 0;
            case 2: return terminal == null || terminal.Completed >= 1;
            case 3: return terminal == null || terminal.Completed >= 2;
            case 4: return dropped;
            case 5: return player.IsDashing;
            case StepParry: return parried || Time.time - stepStart > ParryGiveUp;
            default: return false;   // 마지막: 단말기가 다 차면 스테이지 클리어
        }
    }

    void OnGUI()
    {
        if (sm == null || !sm.Running || !started || Time.timeScale == 0f) return;
        KoreanIMGUIFont.Apply();
        float s = Screen.height / 1080f;
        float w = 1240f * s, h = 60f * s, x = (Screen.width - w) * 0.5f, y = 72f * s;
        var prev = GUI.color;
        GUI.color = new Color(0.02f, 0.03f, 0.08f, 0.82f);
        GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
        GUI.color = prev;
        var tag = new Rect(x + 8f * s, y + 6f * s, 150f * s, h - 12f * s);
        if (UiArt.Tex("name") != null) UiArt.Draw(tag, "name"); else GUI.Box(tag, "");
        UiArt.Label(new Rect(tag.x, tag.y, tag.width * 0.87f, tag.height), $"튜토리얼 {step + 1}/{Steps.Length}", 18f * s, UiArt.Ink, TextAnchor.MiddleCenter, true);
        string text = doneAt >= 0f && step < StepLast ? "<color=#5BE39A><b>좋아요!</b></color>" : Steps[step];
        UiArt.Label(new Rect(tag.xMax + 16f * s, y, w - tag.width - 32f * s, h), text, 24f * s, new Color(0.93f, 0.95f, 1f), TextAnchor.MiddleLeft);
    }
}
