using System.Collections;
using UnityEngine;

/// <summary>
/// 몬스터 공통: 패턴 랜덤 순환, 패링 시 정지(Stun), 느린 배회.
/// 플레이어는 몬스터를 공격할 수 없음. moveSpeed는 플레이어보다 느리게 유지.
/// </summary>
public abstract class MonsterBase : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float patternInterval = 2f;
    public bool wander = true;
    public Rect moveArea = new Rect(-8, -4, 16, 8);
    [Tooltip("스테이지 시작 후 첫 공격까지 대기(초)")] public float firstAttackDelay = 3f;

    [Header("공격 강도 (단말기 완료 1칸마다 1단계 상승)")]
    [Tooltip("단계마다 patternInterval에 곱함")] [Range(0.5f, 1f)] public float intervalStepMultiplier = 0.85f;
    public float minPatternInterval = 0.6f;
    [Tooltip("단계마다 기본 이동속도 대비 증가 비율")] public float speedStepBonus = 0.1f;
    [Tooltip("플레이어 moveSpeed보다 낮게 유지")] public float maxMoveSpeed = 4f;

    public int Aggression { get; private set; }
    /// <summary>실행 중인 패턴 (0 = 패턴1, -1 = 없음). 애니메이션 전환용</summary>
    public int CurrentPattern { get; private set; } = -1;

    protected Transform target;
    protected int patternCount = 1;
    float stunUntil;
    bool halted;
    Vector2 wanderPoint;
    float baseInterval, baseSpeed;
    float currentInterval;

    protected abstract int PatternMax { get; }
    /// <summary>index: 0 = 패턴1</summary>
    protected abstract IEnumerator RunPattern(int index);

    public bool IsStunned => Time.time < stunUntil;

    public void Init(Transform playerTarget, int count)
    {
        target = playerTarget;
        patternCount = Mathf.Clamp(count, 1, PatternMax);
        wanderPoint = transform.position;
        baseInterval = patternInterval;
        baseSpeed = moveSpeed;
        SetAggression(0);
        StartCoroutine(Loop());
    }

    /// <summary>공격 강도 단계 설정: 패턴 간격 감소, 이동속도 소폭 증가</summary>
    public void SetAggression(int level)
    {
        Aggression = Mathf.Max(0, level);
        currentInterval = Mathf.Max(minPatternInterval, baseInterval * Mathf.Pow(intervalStepMultiplier, Aggression));
        moveSpeed = Mathf.Min(Mathf.Max(maxMoveSpeed, baseSpeed), baseSpeed * (1f + speedStepBonus * Aggression));
    }

    public void Stun(float seconds) => stunUntil = Mathf.Max(stunUntil, Time.time + seconds);
    public void Halt() { halted = true; StopAllCoroutines(); }

    IEnumerator Loop()
    {
        yield return Wait(firstAttackDelay);
        while (!halted)
        {
            CurrentPattern = Random.Range(0, patternCount);
            yield return RunPattern(CurrentPattern);
            CurrentPattern = -1;
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
        if (halted || IsStunned || !wander) return;
        if (((Vector2)transform.position - wanderPoint).sqrMagnitude < 0.05f)
            wanderPoint = new Vector2(Random.Range(moveArea.xMin, moveArea.xMax),
                                      Random.Range(moveArea.yMin, moveArea.yMax));
        transform.position = Vector2.MoveTowards(transform.position, wanderPoint, moveSpeed * Time.deltaTime);
    }

    // ---------- 헬퍼 ----------
    protected Vector2 DirToPlayer => ((Vector2)target.position - (Vector2)transform.position).normalized;

    protected static Vector2 Rotate(Vector2 v, float deg) => Quaternion.Euler(0, 0, deg) * v;

    protected Projectile Fire(Projectile prefab, Vector2 pos, Vector2 dir, bool parryable)
    {
        var p = Instantiate(prefab, pos, Quaternion.identity);
        p.Launch(this, dir, parryable);
        return p;
    }
}
