using System.Collections;
using UnityEngine;

/// <summary>1-A 벌 (드라이브 "몬스터 1"). P1: 3방향 투사체(랜덤 1발 패링 가능) / P2: 자기장</summary>
public class Monster1A : MonsterBase
{
    public Projectile projectilePrefab;
    public MagneticField fieldPrefab;
    public float spreadAngle = 20f;
    public float warnLength = 9f;

    protected override int PatternMax => 2;

    protected override void Telegraph(int index)
    {
        if (index == 0)
            for (int i = 0; i < 3; i++) TelegraphLine(transform.position, Rotate(AimDir, (i - 1) * spreadAngle), warnLength, 0.5f);
        else
            TelegraphCircle(transform.position, FieldRadius());
    }

    protected override IEnumerator RunPattern(int index)
    {
        if (index == 0) yield return Pattern1();
        else yield return Pattern2();
    }

    IEnumerator Pattern1()
    {
        int parryIdx = Random.Range(0, 3);                 // 랜덤 1발이 패링 가능
        for (int i = 0; i < 3; i++)
            Fire(projectilePrefab, transform.position, Rotate(AimDir, (i - 1) * spreadAngle), i == parryIdx);
        yield return Wait(0.2f);
    }

    IEnumerator Pattern2()
    {
        var f = Instantiate(fieldPrefab, transform.position, Quaternion.identity, transform);
        f.center = transform;
        yield return Wait(f.lifetime);
    }

    float FieldRadius()
    {
        if (fieldPrefab == null) return 2.5f;
        var c = fieldPrefab.GetComponent<CircleCollider2D>();
        return c != null ? c.radius * fieldPrefab.transform.localScale.x : 2.5f;
    }
}
