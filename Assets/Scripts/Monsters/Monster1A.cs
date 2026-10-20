using System.Collections;
using UnityEngine;

/// <summary>
/// 몬스터1 = 벌 (수정 기획서). 1-A: 쿨마다 플레이어 쪽 3발 부채꼴(정면·좌우 n도), 벽·기계에 닿으면 사라짐, 그중 1발 패링 가능(확률 n).
/// 1-B: 쿨마다 벌 중심 원형 자기장 n초 — 안에 들어오면 벌 쪽으로 끌려가고, 벌 몸에 닿으면 데미지(이때만 접촉 데미지) 후 무적으로 빠져나감.
/// 예전 A-1 직선탄·A-4 십자 전선·A-5 낙하는 몬스터3(보스)용으로 코드만 남겨 둠.
/// </summary>
public class Monster1A : MonsterBase
{
    public const int FanA = 0, FieldB = 1;
    public static readonly string[] PatternNames = { "1-A 3발 부채꼴", "1-B 자기장" };

    [Header("A-1 / A-3 탄")]
    public Projectile projectilePrefab;
    public float spreadAngle = 20f;
    public float warnLength = 9f;

    [Header("A-2 자기장")]
    public MagneticField fieldPrefab;

    [Header("A-4 십자 전선")]
    public Hazard linePrefab;
    public float lineLength = 30f, lineThickness = 1f;

    [Header("A-5 원형 예고 낙하")]
    public int dropCount = 3;
    public float dropRadius = 1.1f, dropWarn = 0.7f, dropGap = 0.25f;

    protected override int PatternMax => 2;

    public override void ApplyTuning(MonsterTuning t, float playerSpeed)
    {
        base.ApplyTuning(t, playerSpeed);
        if (t != null) spreadAngle = t.fanAngle;
    }

    protected override void Telegraph(int index)
    {
        if (index == FanA) for (int i = 0; i < 3; i++) TelegraphLine(transform.position, Rotate(AimDir, (i - 1) * spreadAngle), warnLength, 0.5f);
        else TelegraphCircle(transform.position, Tuning.fieldRadius);
    }

    protected override IEnumerator RunPattern(int index)
    {
        if (index == FanA) yield return Spread();
        else yield return Field();
    }

    IEnumerator Spread()
    {
        // 동시에 나온 3발 중 1발만 패링 가능(보라색), 확률 n
        int parryIdx = Random.value < Tuning.parryChance ? Random.Range(0, 3) : -1;
        for (int i = 0; i < 3; i++)
            Fire(projectilePrefab, transform.position, Rotate(AimDir, (i - 1) * spreadAngle), i == parryIdx);
        yield return Wait(0.2f);
    }

    IEnumerator Field()
    {
        var f = Instantiate(fieldPrefab, transform.position, Quaternion.identity, transform);
        f.center = transform;
        f.lifetime = Tuning.fieldDuration;
        f.SetRadius(Tuning.fieldRadius);
        yield return Wait(f.lifetime);
    }

    // ---------- 아래는 몬스터3(보스)용으로 남겨 둔 예전 패턴 (지금 벌은 안 씀) ----------
    IEnumerator Straight() { Fire(projectilePrefab, transform.position, AimDir, false); yield return Wait(0.2f); }

    IEnumerator Cross()
    {
        if (linePrefab == null) yield break;
        for (int i = 0; i < 2; i++)
        {
            var h = Instantiate(linePrefab, transform.position, Quaternion.Euler(0, 0, i * 90f));
            h.transform.localScale = new Vector3(lineLength, lineThickness, 1f);
        }
        yield return Wait(linePrefab.lifetime);
    }

    IEnumerator Drops()
    {
        Vector2 spot = AimTarget;                          // 첫 지점은 예고 단계에서 이미 표시됨
        for (int i = 0; i < dropCount; i++)
        {
            if (i > 0)
            {
                spot = target != null ? (Vector2)target.position : spot;
                var warn = TelegraphCircleTimed(spot, dropRadius);
                yield return Wait(dropWarn);
                if (warn != null) Destroy(warn);
            }
            Impact(spot);
            yield return Wait(dropGap);
        }
    }

    void Impact(Vector2 pos)
    {
        var go = new GameObject("DropImpact");
        go.transform.position = pos;
        var col = go.AddComponent<CircleCollider2D>(); col.isTrigger = true; col.radius = dropRadius;
        var rb = go.AddComponent<Rigidbody2D>(); rb.bodyType = RigidbodyType2D.Kinematic;
        var hz = go.AddComponent<Hazard>(); hz.damage = 1; hz.jumpable = false; hz.lifetime = 0.25f;
        var fx = FxSet.Get();
        if (fx != null) SpriteFrameAnimator.Spawn(fx.vanishFx, pos, 22f, 35, dropRadius);
        // (화면 흔들림 연출 제거: 기획서에 없음)
    }

    float FieldRadius()
    {
        if (fieldPrefab == null) return 2.5f;
        var c = fieldPrefab.GetComponent<CircleCollider2D>();
        return c != null ? c.radius * fieldPrefab.transform.localScale.x : 2.5f;
    }
}
