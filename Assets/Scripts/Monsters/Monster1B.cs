using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>1-B 공장 로봇. P1: 절단기 날(상하좌우 직진, 일부 패링 가능) / P2: 설치 로봇(지뢰)</summary>
public class Monster1B : MonsterBase
{
    public Projectile bladePrefab;          // speed 크게 (예: 12)
    public Hazard minePrefab;               // destroyOnHit = true
    [Range(0f, 1f)] public float parryChance = 0.4f;
    public int maxMines = 3;

    readonly Queue<Hazard> mines = new Queue<Hazard>();

    protected override int PatternMax => 2;

    void Awake() => wander = false;         // "제자리에서" 공격

    protected override void Telegraph(int index)
    {
        if (index == 0) TelegraphLine(transform.position, Cardinal(AimDir), 12f, 0.6f);
        else TelegraphCircle(transform.position, 0.8f);
    }

    static Vector2 Cardinal(Vector2 d) => Mathf.Abs(d.x) > Mathf.Abs(d.y) ? new Vector2(Mathf.Sign(d.x), 0) : new Vector2(0, Mathf.Sign(d.y));

    protected override IEnumerator RunPattern(int index)
    {
        if (index == 0) yield return Pattern1();
        else yield return Pattern2();
    }

    IEnumerator Pattern1()
    {
        Vector2 d = AimDir;
        // 4방향 중 플레이어와 가장 가까운 축으로
        Vector2 card = Mathf.Abs(d.x) > Mathf.Abs(d.y)
            ? new Vector2(Mathf.Sign(d.x), 0) : new Vector2(0, Mathf.Sign(d.y));
        Fire(bladePrefab, transform.position, card, Random.value < parryChance);
        yield return Wait(0.6f);
    }

    IEnumerator Pattern2()
    {
        while (mines.Count >= maxMines)
        {
            var old = mines.Dequeue();
            if (old) Destroy(old.gameObject);
        }
        mines.Enqueue(Instantiate(minePrefab, transform.position, Quaternion.identity));
        yield return Wait(0.5f);
    }
}
