using System.Collections;
using UnityEngine;

/// <summary>
/// 몬스터3 = 보스 (기획서). 다른 몬스터처럼 플레이어를 쫓아감 (속도 = 플레이어 - n).
/// 3-A: 탄 1발 → 벽·기계·플레이어에 닿으면 8갈래로 갈라짐, 그중 1발 패링 가능(확률 n)
/// 3-B: 보스 중심 십자 전선 3연속 (점프로 넘기, 각 전선은 n초 뒤 사라짐)
/// 3-C: 플레이어가 있던 자리에 주의 표시 → 낙하물, 5연속 (떨어진 자리에 낙하물 이펙트)
/// </summary>
public class Monster1C : MonsterBase
{
    public static readonly string[] PatternNames = { "3-A 8갈래 탄", "3-B 십자 전선 3연속", "3-C 낙하 5연속" };

    [Header("3-A")]
    public Projectile splitProjectilePrefab;   // splitCount = 8, splitPrefab = 작은 탄

    [Header("3-B")]
    public Hazard linePrefab;                  // jumpable = true, lifetime = 전선 유지 시간
    public float lineLength = 30f, lineThickness = 1f;
    public int lineRepeat = 3;
    public float lineGap = 1.1f;               // 다음 십자까지 (초)

    [Header("3-C")]
    public GameObject warnMarkerPrefab;
    public Projectile fallingPrefab;           // 위에서 아래로
    public int dropCount = 5;
    public float warnTime = 0.6f, dropGap = 0.3f, dropHeight = 6f, impactScale = 0.9f;
    [Tooltip("낙하물 피해 범위 (칸, 주의 표시 원과 같거나 작게)")] public float impactRadius = 0.6f;

    protected override int PatternMax => 3;

    // 빨간 공격 예고는 끔 (10/22). 3-C 주의 표시는 패턴 자체라 그대로
    protected override void Telegraph(int index) { }

    protected override IEnumerator RunPattern(int index)
    {
        switch (index)
        {
            case 0: yield return SplitShot(); break;
            case 1: yield return CrossLines(); break;
            default: yield return Drops(); break;
        }
    }

    IEnumerator SplitShot()
    {
        var p = Fire(splitProjectilePrefab, transform.position, AimDir, false);
        if (p != null) p.splitParryChance = Tuning.parryChance;    // 8갈래 중 1발 패링 가능
        yield return Wait(0.8f);
    }

    IEnumerator CrossLines()
    {
        for (int k = 0; k < lineRepeat; k++)
        {
            for (int i = 0; i < 2; i++)   // 가로선 + 세로선 = 십자
            {
                var h = Instantiate(linePrefab, transform.position, Quaternion.Euler(0, 0, i * 90f));
                h.transform.localScale = new Vector3(lineLength, lineThickness, 1f);
            }
            yield return Wait(lineGap);
        }
        yield return Wait(Mathf.Max(0f, linePrefab.lifetime - lineGap));
    }

    IEnumerator Drops()
    {
        var fx = FxSet.Get();
        float fall = fallingPrefab != null && fallingPrefab.speed > 0f ? dropHeight / fallingPrefab.speed : 0.4f;
        for (int i = 0; i < dropCount; i++)
        {
            Vector2 pos = target.position;                       // 플레이어가 서 있는 곳
            var marker = warnMarkerPrefab != null ? Instantiate(warnMarkerPrefab, pos, Quaternion.identity) : null;
            yield return Wait(warnTime);
            var shot = Fire(fallingPrefab, pos + Vector2.up * dropHeight, Vector2.down, false);
            if (shot != null) shot.visualOnly = true;
            StartCoroutine(Impact(pos, fall, marker, fx));
            yield return Wait(dropGap);
        }
        yield return Wait(fall);
    }

    IEnumerator Impact(Vector2 pos, float delay, GameObject marker, FxSet fx)
    {
        yield return Wait(delay);
        if (marker != null) Destroy(marker);
        // 떨어지는 순간, 주의 표시 원(반지름 약 0.7칸) 안에 있을 때만 피해
        var p = target != null ? target.GetComponent<PlayerController>() : null;
        if (p != null && Vector2.Distance(p.transform.position, pos) <= impactRadius && !p.Health.IsInvincible) p.Health.TakeDamage(1);
        if (fx != null && fx.dropFx != null && fx.dropFx.Length > 0) SpriteFrameAnimator.Spawn(fx.dropFx, pos, 18f, 35, impactScale);
    }
}
