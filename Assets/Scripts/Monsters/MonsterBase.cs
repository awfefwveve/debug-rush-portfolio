using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 몬스터 공통. 공격 주기: 이동(patternInterval) → 예고(windup, 공격 범위 표시) → 공격 → 회복.
/// 패턴은 순서대로 돌아감(무작위 X). 패링 시 정지(Stun). 플레이어는 몬스터를 공격할 수 없음.
/// 단위: 1칸 = 1유닛 (팀 규격 32px 격자).
/// </summary>
public abstract class MonsterBase : MonoBehaviour
{
    public enum Phase { Move, Windup, Attack, Recover }

    public float moveSpeed = 2.5f;
    [Tooltip("공격 사이 이동 시간(초)")] public float patternInterval = 2.6f;
    [Tooltip("공격 예고 시간(초)")] public float windupTime = 0.6f;
    [Tooltip("공격 후 회복 시간(초)")] public float recoverTime = 0.5f;
    public bool wander = true;
    public Rect moveArea = new Rect(-11, -4, 22, 8);
    [Tooltip("스테이지 시작 후 첫 공격까지 대기(초)")] public float firstAttackDelay = 3f;

    [Header("공격 강도 (단말기 완료 1칸마다 1단계, 최대 maxAggression)")]
    [Tooltip("단계마다 patternInterval에 곱함")] [Range(0.5f, 1f)] public float intervalStepMultiplier = 0.92f;
    public float minPatternInterval = 1.4f;
    public int maxAggression = 6;
    [Tooltip("단계마다 기본 이동속도 대비 증가 비율")] public float speedStepBonus = 0.06f;
    [Tooltip("플레이어 moveSpeed보다 낮게 유지")] public float maxMoveSpeed = 4.5f;

    public int Aggression { get; private set; }
    /// <summary>실행 중인 패턴 (0 = 패턴1, -1 = 없음). 애니메이션 전환용</summary>
    public int CurrentPattern { get; private set; } = -1;
    public Phase CurrentPhase { get; private set; } = Phase.Move;

    protected Transform target;
    public Transform Target => target;
    protected int patternCount = 1;
    /// <summary>예고 시작 순간 플레이어 방향 (예고와 실제 발사 방향을 일치시킴)</summary>
    protected Vector2 AimDir { get; private set; } = Vector2.down;
    protected Vector2 AimTarget { get; private set; }

    float stunUntil;
    bool halted;
    Vector2 wanderPoint;
    float baseInterval, baseSpeed;
    float currentInterval;
    int nextPattern;
    readonly List<GameObject> telegraphs = new List<GameObject>();
    readonly List<int> patterns = new List<int>();

    protected abstract int PatternMax { get; }
    /// <summary>index: 0 = 패턴1</summary>
    protected abstract IEnumerator RunPattern(int index);
    /// <summary>예고 표시: 패턴별로 원형/직선 예고를 추가</summary>
    protected virtual void Telegraph(int index) { }

    public bool IsStunned => Time.time < stunUntil;

    /// <summary>mask: 사용할 패턴 비트 (0이면 앞에서부터 count개). 패턴은 목록 순서대로 반복</summary>
    public void Init(Transform playerTarget, int count, int mask = 0)
    {
        target = playerTarget;
        patterns.Clear();
        for (int i = 0; i < PatternMax; i++)
            if (mask != 0 ? (mask & (1 << i)) != 0 : i < Mathf.Max(1, count)) patterns.Add(i);
        if (patterns.Count == 0) patterns.Add(0);
        patternCount = patterns.Count;
        nextPattern = 0;
        wanderPoint = transform.position;
        baseInterval = patternInterval;
        baseSpeed = moveSpeed;
        SetAggression(0);
        StartCoroutine(Loop());
    }

    /// <summary>공격 강도 단계 설정: 패턴 간격 감소, 이동속도 소폭 증가 (상한 있음)</summary>
    public void SetAggression(int level)
    {
        Aggression = Mathf.Clamp(level, 0, maxAggression);
        currentInterval = Mathf.Max(minPatternInterval, baseInterval * Mathf.Pow(intervalStepMultiplier, Aggression));
        moveSpeed = Mathf.Min(Mathf.Max(maxMoveSpeed, baseSpeed), baseSpeed * (1f + speedStepBonus * Aggression));
    }

    public void Stun(float seconds) => stunUntil = Mathf.Max(stunUntil, Time.time + seconds);
    public void Halt() { halted = true; StopAllCoroutines(); ClearTelegraphs(); }

    IEnumerator Loop()
    {
        yield return Wait(firstAttackDelay);
        while (!halted)
        {
            int p = patterns[nextPattern];
            nextPattern = (nextPattern + 1) % patterns.Count;

            // 예고
            CurrentPattern = p;
            CurrentPhase = Phase.Windup;
            if (target != null) { AimTarget = target.position; AimDir = (AimTarget - (Vector2)transform.position).normalized; }
            Telegraph(p);
            yield return Wait(windupTime);
            ClearTelegraphs();

            // 공격
            CurrentPhase = Phase.Attack;
            yield return RunPattern(p);

            // 회복
            CurrentPhase = Phase.Recover;
            yield return Wait(recoverTime);

            CurrentPattern = -1;
            CurrentPhase = Phase.Move;
            yield return Wait(currentInterval);
        }
    }

    /// <summary>스턴 중에는 시간이 흐르지 않는 대기 (패링으로 패턴 전체가 멈춤)</summary>
    protected IEnumerator Wait(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            if (!IsStunned) t += Time.deltaTime;
            yield return null;
        }
    }

    void Update()
    {
        UpdateTelegraphs();
        // 예고·공격 중에는 제자리 (읽기 쉽게)
        if (halted || IsStunned || !wander || CurrentPhase != Phase.Move) return;
        if (((Vector2)transform.position - wanderPoint).sqrMagnitude < 0.05f)
            wanderPoint = new Vector2(Random.Range(moveArea.xMin, moveArea.xMax),
                                      Random.Range(moveArea.yMin, moveArea.yMax));
        transform.position = Vector2.MoveTowards(transform.position, wanderPoint, moveSpeed * Time.deltaTime);
    }

    // ---------- 예고 (팀 규격: 원형 예고 128 채움·테두리 분리, 직선 예고 32×128 아래 중앙 기준) ----------
    /// <summary>공격 도중 따로 쓰는 원형 예고 (직접 Destroy)</summary>
    protected GameObject TelegraphCircleTimed(Vector2 center, float radius)
    {
        TelegraphCircle(center, radius);
        return telegraphs.Count > 0 ? telegraphs[telegraphs.Count - 1] : null;
    }

    protected void TelegraphCircle(Vector2 center, float radius)
    {
        var fx = FxSet.Get();
        if (fx == null) return;
        var root = new GameObject("Warn_Circle");
        root.transform.position = center;
        AddTelegraphPart(root, fx.warnCircleFill, radius * 2f / Mathf.Max(0.01f, fx.warnCircleFill ? fx.warnCircleFill.bounds.size.x : 1f), 0.0f);
        AddTelegraphPart(root, fx.warnCircleBorder, radius * 2f / Mathf.Max(0.01f, fx.warnCircleBorder ? fx.warnCircleBorder.bounds.size.x : 1f), 0.9f);
        telegraphs.Add(root);
    }

    protected void TelegraphLine(Vector2 origin, Vector2 dir, float length, float width)
    {
        var fx = FxSet.Get();
        if (fx == null || fx.warnLine == null) return;
        var go = new GameObject("Warn_Line");
        go.transform.position = origin;
        go.transform.up = dir;                                  // 아래 중앙 기준 → 길이 방향으로 늘림
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = fx.warnLine; sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = new Vector2(width, length);
        sr.sortingOrder = 20;                                   // 맵 물체보다 위 (예고가 가려지지 않게)
        sr.color = new Color(1f, 0.3f, 0.35f, 0.25f);
        telegraphs.Add(go);
    }

    void AddTelegraphPart(GameObject root, Sprite sprite, float scale, float alpha)
    {
        if (sprite == null) return;
        var child = new GameObject(sprite.name);
        child.transform.SetParent(root.transform, false);
        child.transform.localScale = Vector3.one * scale;
        var sr = child.AddComponent<SpriteRenderer>();
        sr.sprite = sprite; sr.sortingOrder = 20;
        sr.color = new Color(1f, 0.3f, 0.35f, alpha);
    }

    void UpdateTelegraphs()
    {
        if (telegraphs.Count == 0) return;
        float k = 0.5f + 0.5f * Mathf.Sin(Time.time * 18f);
        foreach (var t in telegraphs)
        {
            if (t == null) continue;
            foreach (var sr in t.GetComponentsInChildren<SpriteRenderer>())
            {
                var c = sr.color;
                bool fill = sr.gameObject.name.Contains("fill") || sr.gameObject.name.StartsWith("Warn_Line");
                c.a = fill ? Mathf.MoveTowards(c.a, 0.38f, Time.deltaTime * 0.8f) : 0.55f + 0.4f * k;
                sr.color = c;
            }
        }
    }

    void ClearTelegraphs()
    {
        foreach (var t in telegraphs) if (t != null) Destroy(t);
        telegraphs.Clear();
    }

    void OnDestroy() => ClearTelegraphs();

    // ---------- 헬퍼 ----------
    protected Vector2 DirToPlayer => target != null ? ((Vector2)target.position - (Vector2)transform.position).normalized : Vector2.down;

    protected static Vector2 Rotate(Vector2 v, float deg) => Quaternion.Euler(0, 0, deg) * v;

    protected Projectile Fire(Projectile prefab, Vector2 pos, Vector2 dir, bool parryable)
    {
        var p = Instantiate(prefab, pos, Quaternion.identity);
        p.Launch(this, dir, parryable);
        var fx = FxSet.Get();
        if (fx != null) SpriteFrameAnimator.Spawn(fx.fireFx, pos, 20f, 25, 0.8f);   // 발사 이펙트 1회
        return p;
    }
}
