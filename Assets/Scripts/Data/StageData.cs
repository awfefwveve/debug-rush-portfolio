using System.Collections.Generic;
using UnityEngine;

/// <summary>기획서의 n값 (몬스터별·스테이지별로 Stage Maker에서 조절)</summary>
[System.Serializable]
public class MonsterTuning
{
    [Tooltip("플레이어보다 이만큼 느리게 쫓아감 (칸/초)")] public float speedGap = 3f;
    [Tooltip("패턴별 쿨타임(초). [0] = A 패턴, [1] = B 패턴")] public float[] cooldown = { 3f, 9f };
    [Tooltip("동시에 나온 투사체 중 1개가 패링 가능할 확률 (0~1)")] [Range(0f, 1f)] public float parryChance = 0.5f;
    [Header("벌 1-A 부채꼴 · 1-B 자기장")]
    [Tooltip("3발 부채꼴 좌우 각도(도)")] public float fanAngle = 20f;
    [Tooltip("자기장 유지 시간(초)")] public float fieldDuration = 3f;
    [Tooltip("자기장 반지름(칸)")] public float fieldRadius = 2.2f;
    [Header("몬스터2 2-B 설치 로봇")]
    [Tooltip("감지 범위(칸)")] public float mineDetect = 1.8f;
    [Tooltip("감지 후 폭발까지(초)")] public float mineDelay = 1f;
    [Tooltip("폭발 범위(칸)")] public float mineRadius = 1.6f;
    [Tooltip("동시에 깔려 있을 수 있는 최대 개수")] public int maxMines = 3;
}

[System.Serializable]
public class MonsterSpawn
{
    public GameObject prefab;
    [Tooltip("사용할 패턴 수")]
    [Range(1, 5)] public int patternCount = 1;
    [Tooltip("사용할 패턴 체크 (비트: A=1, B=2). 0이면 앞에서부터 patternCount개")]
    public int patternMask = 0;
    public Vector2 spawnPos;
    public MonsterTuning tuning = new MonsterTuning();
}

[System.Serializable]
public struct DialogueLine
{
    public string speaker;
    [TextArea] public string text;
    public Sprite portrait;
}

/// <summary>재료대 하나 (종류 + 위치)</summary>
[System.Serializable]
public class StationSpot
{
    public ItemType material = ItemType.Planning;
    public Vector2 pos;
}

/// <summary>스테이지별 맵 배치. use=false면 Stage 씬에 놓인 기본 배치를 그대로 사용.</summary>
[System.Serializable]
public class StageLayout
{
    public bool use;
    [Tooltip("방 크기 (칸, 짝수). 기본 30×16 = 1920×1024 화면에 꽉 참")]
    public int width = WallBuilder.DefaultW, height = WallBuilder.DefaultH;
    [Tooltip("벽 칸 (가운데 기준). 비어 있으면 Stage 씬의 기본 벽 사용")]
    public List<Vector2Int> walls = new List<Vector2Int>();
    [Tooltip("바닥 칸. customFloor가 꺼져 있으면 방 안쪽 전체가 바닥")]
    public bool customFloor;
    public List<Vector2Int> floor = new List<Vector2Int>();
    public Vector2 playerStart = new Vector2(0f, -3.4f);
    [Tooltip("재료대 (개수 제한 없음)")]
    public List<StationSpot> stations = new List<StationSpot>();
    [Tooltip("제작대 (개수 제한 없음)")]
    public List<Vector2> crafters = new List<Vector2>();
    [Tooltip("단말기 (개수 제한 없음)")]
    public Vector2[] terminals = { new Vector2(10.5f, 2.0f), new Vector2(-10.5f, -2.4f) };

    // 예전 형식 (재료대 4개·제작대 1개 고정). 불러올 때 위 목록으로 옮김
    [SerializeField, HideInInspector] bool listsReady;
    [SerializeField, HideInInspector] Vector2 planning = new Vector2(5f, -3.2f), programming = new Vector2(9f, -3.2f), graphic = new Vector2(-9f, 3.2f), qa = new Vector2(-5f, 3.2f);
    [SerializeField, HideInInspector] Vector2 craftingTable = Vector2.zero;

    /// <summary>예전 형식 데이터를 목록 형식으로 변환 (한 번만)</summary>
    public void Migrate()
    {
        if (listsReady) return;
        listsReady = true;
        if (stations.Count == 0)
        {
            stations.Add(new StationSpot { material = ItemType.Planning, pos = planning });
            stations.Add(new StationSpot { material = ItemType.Programming, pos = programming });
            stations.Add(new StationSpot { material = ItemType.Graphic, pos = graphic });
            stations.Add(new StationSpot { material = ItemType.QA, pos = qa });
        }
        if (crafters.Count == 0) crafters.Add(craftingTable);
    }

    /// <summary>바닥 칸 집합 (직접 칠하지 않았으면 방 안쪽 전체)</summary>
    public HashSet<Vector2Int> FloorSet()
    {
        if (customFloor) return new HashSet<Vector2Int>(floor);
        var s = new HashSet<Vector2Int>();
        for (int x = WallBuilder.MinX(width) + 1; x < WallBuilder.MaxX(width); x++)
            for (int y = WallBuilder.MinY(height) + 1; y < WallBuilder.MaxY(height); y++) s.Add(new Vector2Int(x, y));
        return s;
    }

    /// <summary>실제로 칠해진 칸(바닥+벽)의 범위. 카메라가 이 범위에 맞춰 줌. 아무것도 없으면 방 크기</summary>
    public RectInt PaintedBounds()
    {
        int x0 = int.MaxValue, y0 = int.MaxValue, x1 = int.MinValue, y1 = int.MinValue;
        foreach (var c in FloorSet()) { x0 = Mathf.Min(x0, c.x); y0 = Mathf.Min(y0, c.y); x1 = Mathf.Max(x1, c.x); y1 = Mathf.Max(y1, c.y); }
        foreach (var c in walls) { x0 = Mathf.Min(x0, c.x); y0 = Mathf.Min(y0, c.y); x1 = Mathf.Max(x1, c.x); y1 = Mathf.Max(y1, c.y); }
        if (x0 > x1) return new RectInt(WallBuilder.MinX(width), WallBuilder.MinY(height), width, height);
        return new RectInt(x0, y0, x1 - x0 + 1, y1 - y0 + 1);
    }

    /// <summary>바닥을 칸 목록으로 바꿔서 직접 칠할 수 있게</summary>
    public void MakeFloorExplicit()
    {
        if (customFloor) return;
        floor = new List<Vector2Int>(FloorSet());
        customFloor = true;
    }

    /// <summary>새로 만든 배치는 목록 형식으로 시작</summary>
    public void MarkListsReady() => listsReady = true;
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

    public MonsterSpawn[] monsters;      // 개수 제한 없음 (기획서 기준 1~2마리)
    [Tooltip("피격 후 무적 시간(초)")] public float playerInvincibleTime = 1.5f;
    [Tooltip("Stage Maker에서 새로 놓는 몬스터의 기본 패턴 (비트)")] public int newMonsterPatternMask = 1;
    public DialogueLine[] intro;         // 스테이지 시작 스토리
    public DialogueLine[] outro;         // 스테이지 종료 스토리

    public StageLayout layout = new StageLayout();

    public float TimeFor(Difficulty d) => timeLimit * timeMultiplier[(int)d];
    public int GoalFor(Difficulty d) => Mathf.Clamp(terminalGoal[(int)d], 2, 8);
    public float RequestTimeFor(Difficulty d)
        => requestTime != null && requestTime.Length > (int)d ? Mathf.Max(0f, requestTime[(int)d]) : 30f;
}
