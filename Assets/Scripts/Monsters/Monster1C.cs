using System.Collections;
using UnityEngine;

/// <summary>
/// 1-C 고철 로봇.
/// P1: 투사체 1개 → 벽/플레이어에 닿으면 8방향 분열 / P2: 십자 전선(점프로 회피) / P3: 주의 표시 후 낙하탄 최대 5연속
/// </summary>
public class Monster1C : MonsterBase
{
    [Header("패턴 1")]
    public Projectile splitProjectilePrefab;   // splitCount=8, splitPrefab=작은 탄
    [Range(0f, 1f)] public float parryChance = 0.4f;

    [Header("패턴 2")]
    public Hazard linePrefab;                  // jumpable = true, lifetime = 3
    public float lineLength = 30f, lineThickness = 1f;

    [Header("패턴 3")]
    public GameObject warnMarkerPrefab;
    public Projectile fallingPrefab;           // speed 12, lifetime 0.5 (위에서 아래로)
    public int dropCount = 5;
    public float warnTime = 0.6f, dropGap = 0.3f, dropHeight = 6f;

    protected override int PatternMax => 3;

    void Awake() => wander = false;

    protected override void Telegraph(int index)
    {
        if (index == 0) TelegraphLine(transform.position, AimDir, 10f, 0.6f);
        else if (index == 1)
            foreach (var d in new[] { Vector2.right, Vector2.left, Vector2.up, Vector2.down })
                TelegraphLine(transform.position, d, lineLength * 0.5f, lineThickness);
    }

    protected override IEnumerator RunPattern(int index)
    {
        switch (index)
        {
            case 0: yield return Pattern1(); break;
            case 1: yield return Pattern2(); break;
            default: yield return Pattern3(); break;
        }
    }

    IEnumerator Pattern1()
    {
        Fire(splitProjectilePrefab, transform.position, AimDir, Random.value < parryChance);
        yield return Wait(0.8f);
    }

    IEnumerator Pattern2()
    {
        // 가로선 + 세로선 = 십자
        for (int i = 0; i < 2; i++)
        {
            var h = Instantiate(linePrefab, transform.position, Quaternion.Euler(0, 0, i * 90f));
            h.transform.localScale = new Vector3(lineLength, lineThickness, 1f);
        }
        yield return Wait(linePrefab.lifetime);
    }

    IEnumerator Pattern3()
    {
        for (int i = 0; i < dropCount; i++)
        {
            Vector2 pos = target.position;                       // 플레이어가 서 있는 곳
            var marker = Instantiate(warnMarkerPrefab, pos, Quaternion.identity);
            yield return Wait(warnTime);
            Fire(fallingPrefab, pos + Vector2.up * dropHeight, Vector2.down, false);
            Destroy(marker);
            yield return Wait(dropGap);
        }
    }
}
