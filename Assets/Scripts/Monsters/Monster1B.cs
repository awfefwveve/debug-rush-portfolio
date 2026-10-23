using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 몬스터2 (수정 기획서, 스토리상 이름 "도움 NPC").
/// 2-A: 플레이어가 몬스터의 상하좌우 일직선(같은 행·열)에 있으면 제자리에서 절단기 날 발사 → 그 줄을 따라 벽까지 직진,
///      벽·기계에 닿으면 사라짐, 패링 가능(확률 n). 쏜 직후 바로 다시 움직임.
/// 2-B: 쿨마다 바닥에 설치 로봇 1개. 플레이어가 감지 범위에 들어오면 n초 뒤 폭발, 그때 범위 안이면 데미지. 몬스터는 밟아도 안 터짐.
/// 그림은 임시 (Art/Drive/Monster2 에 넣으면 교체).
/// </summary>
public class Monster1B : MonsterBase
{
    public const int BladeA = 0, MineB = 1;
    public static readonly string[] PatternNames = { "2-A 절단기 날", "2-B 설치 로봇" };

    public Projectile bladePrefab;
    public GameObject minePrefab;
    [Tooltip("같은 행·열로 보는 오차(칸)")] public float alignTolerance = 0.6f;

    readonly Queue<GameObject> mines = new Queue<GameObject>();
    Vector2 lineDir;

    protected override int PatternMax => 2;

    protected override bool CanUse(int index)
    {
        if (index != BladeA) return true;
        if (target == null) return false;
        Vector2 d = (Vector2)target.position - (Vector2)transform.position;
        if (Mathf.Abs(d.y) <= alignTolerance) { lineDir = new Vector2(Mathf.Sign(d.x), 0f); return true; }
        if (Mathf.Abs(d.x) <= alignTolerance) { lineDir = new Vector2(0f, Mathf.Sign(d.y)); return true; }
        return false;
    }

    protected override void Telegraph(int index)
    {
        if (index == BladeA) TelegraphLine(transform.position, lineDir, 30f, 0.6f);
        else TelegraphCircle(transform.position, Tuning.mineDetect);
    }

    protected override IEnumerator RunPattern(int index)
    {
        if (index == BladeA)
        {
            Fire(bladePrefab, transform.position, lineDir, Random.value < Tuning.parryChance);
            yield break;                                   // 쏜 직후 바로 이동 가능
        }
        while (mines.Count >= Mathf.Max(1, Tuning.maxMines))
        {
            var old = mines.Dequeue();
            if (old) Destroy(old);
        }
        var m = Instantiate(minePrefab, MonsterNav.NearestWalkable(Foot), Quaternion.identity);   // 발밑에 설치 (벽·기계 칸이면 가장 가까운 빈 바닥)
        foreach (var h in m.GetComponents<Hazard>()) Destroy(h);   // 예전 "밟으면 폭발" 동작 대신 감지 → 폭발
        var bot = m.AddComponent<InstallRobot>();
        bot.Setup(Tuning.mineDetect, Tuning.mineDelay, Tuning.mineRadius);
        mines.Enqueue(m);
        yield return Wait(0.3f);
    }
}
