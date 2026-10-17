using System.Collections;
using UnityEngine;

/// <summary>
/// 챕터1 몬스터 A = 벌 (드라이브 "몬스터 1"). 회의록 기준: 챕터 하나당 몬스터 하나, A-1~A-5 = 벌의 공격 패턴.
/// 스테이지가 오를수록 패턴이 하나씩 추가됨 (StageData 몬스터의 패턴 체크).
/// A-1 탄 발사 / A-2 자기장 / A-3 3발 부채꼴(1발 패링 가능) / A-4 십자 전선(점프로 회피) / A-5 원형 예고 낙하.
/// 모든 패턴은 예고 → 공격 → 회복 템포.
/// </summary>
public class Monster1A : MonsterBase
{
    public const int A1 = 0, A2 = 1, A3 = 2, A4 = 3, A5 = 4;
    public static readonly string[] PatternNames = { "A-1 탄 발사", "A-2 자기장", "A-3 3발 부채꼴", "A-4 십자 전선", "A-5 원형 예고 낙하" };

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

    protected override int PatternMax => 5;

    protected override void Telegraph(int index)
    {
        switch (index)
        {
            case A1: TelegraphLine(transform.position, AimDir, warnLength, 0.5f); break;
            case A2: TelegraphCircle(transform.position, FieldRadius()); break;
            case A3: for (int i = 0; i < 3; i++) TelegraphLine(transform.position, Rotate(AimDir, (i - 1) * spreadAngle), warnLength, 0.5f); break;
            case A4: foreach (var d in new[] { Vector2.right, Vector2.left, Vector2.up, Vector2.down }) TelegraphLine(transform.position, d, lineLength * 0.5f, lineThickness); break;
            default: TelegraphCircle(AimTarget, dropRadius); break;      // 첫 낙하 지점
        }
    }

    protected override IEnumerator RunPattern(int index)
    {
        switch (index)
        {
            case A1: Fire(projectilePrefab, transform.position, AimDir, false); yield return Wait(0.2f); break;
            case A2: yield return Field(); break;
            case A3: yield return Spread(); break;
            case A4: yield return Cross(); break;
            default: yield return Drops(); break;
        }
    }

    IEnumerator Spread()
    {
        int parryIdx = Random.Range(0, 3);                 // 랜덤 1발이 패링 가능 (분홍 마름모)
        for (int i = 0; i < 3; i++)
            Fire(projectilePrefab, transform.position, Rotate(AimDir, (i - 1) * spreadAngle), i == parryIdx);
        yield return Wait(0.2f);
    }

    IEnumerator Field()
    {
        var f = Instantiate(fieldPrefab, transform.position, Quaternion.identity, transform);
        f.center = transform;
        yield return Wait(f.lifetime);
    }

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
        if (StageFX.I != null) StageFX.I.Shake(0.12f, 0.08f);
    }

    float FieldRadius()
    {
        if (fieldPrefab == null) return 2.5f;
        var c = fieldPrefab.GetComponent<CircleCollider2D>();
        return c != null ? c.radius * fieldPrefab.transform.localScale.x : 2.5f;
    }
}
