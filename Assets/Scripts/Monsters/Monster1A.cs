using System.Collections;
using UnityEngine;

/// <summary>1-A 벌레형 공격 드론. P1: 3방향 투사체(1개 패링 가능) / P2: 자기장</summary>
public class Monster1A : MonsterBase
{
    public Projectile projectilePrefab;
    public MagneticField fieldPrefab;
    public float spreadAngle = 20f;

    protected override int PatternMax => 2;

    protected override IEnumerator RunPattern(int index)
    {
        if (index == 0) yield return Pattern1();
        else yield return Pattern2();
    }

    IEnumerator Pattern1()
    {
        Vector2 d = DirToPlayer;
        int parryIdx = Random.Range(0, 3);                 // 랜덤 1발이 패링 가능
        for (int i = 0; i < 3; i++)
            Fire(projectilePrefab, transform.position, Rotate(d, (i - 1) * spreadAngle), i == parryIdx);
        yield return Wait(0.5f);
    }

    IEnumerator Pattern2()
    {
        var f = Instantiate(fieldPrefab, transform.position, Quaternion.identity, transform);
        f.center = transform;
        yield return Wait(f.lifetime);
    }
}
