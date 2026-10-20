using System.Collections;
using UnityEngine;

/// <summary>
/// 버그에 지배당한 로봇 (기획서 몬스터 시스템 예시).
/// P1: 플레이어를 향해 투사체 3발 (랜덤 1발 패링 가능) / P2: 제자리에서 십자 전선 (밟으면 데미지, 일정 시간 후 사라짐, 점프로 회피)
/// </summary>
public class MonsterRobot : MonsterBase
{
    /// <summary>Stage Maker 토글 이름 (기획서: 3발 투사체 + 십자 전선)</summary>
    public static readonly string[] PatternNames = { "B-1 투사체 3발", "B-2 십자 전선" };

    [Header("패턴 1")]
    public Projectile projectilePrefab;
    public int shotCount = 3;
    public float shotGap = 0.22f;

    [Header("패턴 2")]
    public Hazard linePrefab;                  // jumpable = true, lifetime > 0
    public float lineLength = 30f, lineThickness = 1f;

    protected override int PatternMax => 2;


    protected override void Telegraph(int index)
    {
        if (index == 0) TelegraphLine(transform.position, AimDir, 10f, 0.5f);
        else
        {
            foreach (var d in new[] { Vector2.right, Vector2.left, Vector2.up, Vector2.down })
                TelegraphLine(transform.position, d, lineLength * 0.5f, lineThickness);
        }
    }

    protected override IEnumerator RunPattern(int index)
    {
        if (index == 0) yield return Pattern1();
        else yield return Pattern2();
    }

    IEnumerator Pattern1()
    {
        int parryIdx = Random.Range(0, shotCount);
        for (int i = 0; i < shotCount; i++)
        {
            Fire(projectilePrefab, transform.position, AimDir, i == parryIdx);
            yield return Wait(shotGap);
        }
    }

    IEnumerator Pattern2()
    {
        for (int i = 0; i < 2; i++)
        {
            var h = Instantiate(linePrefab, transform.position, Quaternion.Euler(0, 0, i * 90f));
            h.transform.localScale = new Vector3(lineLength, lineThickness, 1f);
        }
        yield return Wait(linePrefab.lifetime);
    }
}
