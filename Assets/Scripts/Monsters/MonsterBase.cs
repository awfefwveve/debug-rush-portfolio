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

    protected Transform target;
    protected int patternCount = 1;
    float stunUntil;
    bool halted;
    Vector2 wanderPoint;

    protected abstract int PatternMax { get; }
    /// <summary>index: 0 = 패턴1</summary>
    protected abstract IEnumerator RunPattern(int index);

    public bool IsStunned => Time.time < stunUntil;

    public void Init(Transform playerTarget, int count)
    {
        target = playerTarget;
        patternCount = Mathf.Clamp(count, 1, PatternMax);
        wanderPoint = transform.position;
        StartCoroutine(Loop());
    }

    public void Stun(float seconds) => stunUntil = Mathf.Max(stunUntil, Time.time + seconds);
    public void Halt() { halted = true; StopAllCoroutines(); }

    IEnumerator Loop()
    {
        yield return Wait(1f);
        while (!halted)
        {
            yield return RunPattern(Random.Range(0, patternCount));
            yield return Wait(patternInterval);
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
