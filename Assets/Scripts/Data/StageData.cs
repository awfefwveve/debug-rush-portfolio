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

    public MonsterSpawn[] monsters;      // 스테이지당 1~2마리
    public DialogueLine[] intro;         // 스테이지 시작 스토리
    public DialogueLine[] outro;         // 스테이지 종료 스토리

    public float TimeFor(Difficulty d) => timeLimit * timeMultiplier[(int)d];
    public int GoalFor(Difficulty d) => Mathf.Clamp(terminalGoal[(int)d], 2, 8);
}
