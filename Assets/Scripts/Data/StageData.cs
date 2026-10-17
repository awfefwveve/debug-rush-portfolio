using UnityEngine;

[System.Serializable]
public class MonsterSpawn
{
    public GameObject prefab;
    [Tooltip("사용할 패턴 수 (1-1: 1, 1-2: 2 ... 기획서 p.10 표 참고)")]
    [Range(1, 3)] public int patternCount = 1;
    public Vector2 spawnPos;
}

[System.Serializable]
public struct DialogueLine
{
    public string speaker;
    [TextArea] public string text;
    public Sprite portrait;
}

/// <summary>스테이지별 맵 배치. use=false면 Stage 씬에 놓인 기본 배치를 그대로 사용.</summary>
[System.Serializable]
public class StageLayout
{
    public bool use;
    public Vector2 playerStart = new Vector2(0f, -3.4f);
    [Header("재료대")]
    public Vector2 planning = new Vector2(5f, -3.2f);
    public Vector2 programming = new Vector2(9f, -3.2f);
    public Vector2 graphic = new Vector2(-9f, 3.2f);
    public Vector2 qa = new Vector2(-5f, 3.2f);
    [Header("제작대 / 단말기")]
    public Vector2 craftingTable = new Vector2(0f, 0f);
    [Tooltip("단말기 개수 = 배열 길이 (1~4)")]
    public Vector2[] terminals = { new Vector2(10.5f, 2.0f), new Vector2(-10.5f, -2.4f) };

    public Vector2 MaterialPos(ItemType t)
    {
        switch (t)
        {
            case ItemType.Planning: return planning;
            case ItemType.Programming: return programming;
            case ItemType.Graphic: return graphic;
            default: return qa;
        }
    }

    public void SetMaterialPos(ItemType t, Vector2 p)
    {
        switch (t)
        {
            case ItemType.Planning: planning = p; break;
            case ItemType.Programming: programming = p; break;
            case ItemType.Graphic: graphic = p; break;
            default: qa = p; break;
        }
    }
}

[CreateAssetMenu(menuName = "DebugRush/Stage Data")]
public class StageData : ScriptableObject
{
    public int chapter = 1;
    public int stage = 1;
    public string displayName = "1-1";
    [TextArea] public string summary;
    public string sceneName = "Stage";

    [Min(10)] public float timeLimit = 180f;
    [Tooltip("Easy / Normal / Hard")] public float[] timeMultiplier = { 1.3f, 1f, 0.8f };
    [Tooltip("난이도별 단말기 완료 목표 (2~8)")] public int[] terminalGoal = { 2, 3, 4 };
    [Tooltip("난이도별 단말기 요구사항 하나당 남은 시간(초). 0 = 무제한")] public float[] requestTime = { 40f, 30f, 22f };

    public MonsterSpawn[] monsters;      // 스테이지당 1~2마리
    public DialogueLine[] intro;         // 스테이지 시작 스토리
    public DialogueLine[] outro;         // 스테이지 종료 스토리

    public StageLayout layout = new StageLayout();

    public float TimeFor(Difficulty d) => timeLimit * timeMultiplier[(int)d];
    public int GoalFor(Difficulty d) => Mathf.Clamp(terminalGoal[(int)d], 2, 8);
    public float RequestTimeFor(Difficulty d)
        => requestTime != null && requestTime.Length > (int)d ? Mathf.Max(0f, requestTime[(int)d]) : 30f;
}
