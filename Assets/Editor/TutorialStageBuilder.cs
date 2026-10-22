using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 튜토리얼 스테이지 데이터(1학년 0번, Stage_1_0) 만들기 + 스테이지 선택에 등록. 이후 수정은 Stage Maker에서.
/// 작은 방 20×12, 생성기 2(기획·프로그래밍), 제작대 1, 단말기 1, 벌 1(1-A만, 느리게, 쿨 길게 — 벌 단계에서 등장).
/// 메뉴: DebugRush > Create Tutorial Stage
/// </summary>
public static class TutorialStageBuilder
{
    const string Path = "Assets/DebugRush/Data/Stage_1_0.asset";

    [MenuItem("DebugRush/Create Tutorial Stage")]
    public static void Create()
    {
        var s = AssetDatabase.LoadAssetAtPath<StageData>(Path);
        bool isNew = s == null;
        if (isNew) s = ScriptableObject.CreateInstance<StageData>();

        s.chapter = 1; s.stage = 0; s.displayName = "튜토리얼";
        s.summary = "버그 테스트용 방에서 조작 익히기";
        s.sceneName = "Stage";
        s.timeLimit = 600f;                                   // 넉넉하게 (실패 거의 없게)
        s.timeMultiplier = new[] { 1f, 1f, 1f };
        s.terminalGoal = new[] { 4, 4, 4 };                   // 재료 1 · 완성품 1 · 마지막 단계 2
        s.requestTime = new[] { 0f, 0f, 0f };                 // 요구사항 시간 제한 없음
        s.playerInvincibleTime = 1.5f;
        s.intro = new[]
        {
            new DialogueLine { speaker = "주인공", text = "프로젝트 버그 테스트용 방이네…" },
            new DialogueLine { speaker = "주인공", text = "본격적으로 고치기 전에 조작부터 확인해 보자." },
        };
        s.outro = new[] { new DialogueLine { speaker = "주인공", text = "좋아, 손이 풀렸어. 이제 진짜 버그를 잡으러 가자!" } };

        var l = new StageLayout { use = true, width = 20, height = 12 };
        l.MarkListsReady();
        l.walls = new List<Vector2Int>(WallBuilder.Border(20, 12));
        l.customFloor = false;
        l.playerStart = new Vector2(0.5f, -4.4f);
        l.stations = new List<StationSpot>
        {
            new StationSpot { material = ItemType.Planning, pos = new Vector2(-6f, 1.5f) },
            new StationSpot { material = ItemType.Programming, pos = new Vector2(-6f, -3f) },
        };
        l.crafters = new List<Vector2> { new Vector2(0.5f, -0.5f) };
        l.terminals = new[] { new Vector2(6.5f, 0.5f) };
        s.layout = l;

        var bee = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DebugRush/Prefabs/Monster1A.prefab");
        s.monsters = new[]
        {
            new MonsterSpawn
            {
                prefab = bee, patternCount = 1, patternMask = 1, spawnPos = new Vector2(5.5f, -4f),
                tuning = new MonsterTuning { speedGap = 5f, cooldown = new[] { 5f, 999f }, parryChance = 1f, fanAngle = 22f },
            },
        };

        if (isNew) AssetDatabase.CreateAsset(s, Path); else EditorUtility.SetDirty(s);
        AssetDatabase.SaveAssets();
        StageMakerWindow.Register(s, true);
        Debug.Log("[Tutorial] Stage_1_0 ready");
    }
}
