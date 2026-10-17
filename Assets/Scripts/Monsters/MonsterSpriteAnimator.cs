using UnityEngine;

/// <summary>
/// 몬스터 프레임 애니메이션: 이동 = 움직임 반복, 예고~공격 = 패턴 프레임 1회 재생(준비→발사), 회복 후 움직임으로 복귀,
/// 기절 중 = 정지 + 깜빡이지 않게 살짝 어둡게.
/// </summary>
[RequireComponent(typeof(MonsterBase))]
public class MonsterSpriteAnimator : MonoBehaviour
{
    public SpriteFrameAnimator anim;
    public Sprite[] move;
    [Tooltip("index 0 = 패턴1, 1 = 패턴2 …")]
    public Sprite[] pattern1, pattern2, pattern3, pattern4, pattern5;
    public float fps = 10f;

    MonsterBase monster;
    int shownPattern = -2;
    SpriteRenderer sr;

    void Awake()
    {
        monster = GetComponent<MonsterBase>();
        sr = anim != null ? anim.target : GetComponentInChildren<SpriteRenderer>();
    }

    void LateUpdate()
    {
        if (anim == null) return;
        bool stunned = monster.IsStunned;
        anim.Paused = stunned;
        if (sr != null) sr.color = stunned ? new Color(0.65f, 0.65f, 0.8f) : Color.white;

        bool attacking = monster.CurrentPhase == MonsterBase.Phase.Windup || monster.CurrentPhase == MonsterBase.Phase.Attack;
        int want = attacking ? monster.CurrentPattern : -1;
        var set = Pick(want);
        if (want != shownPattern)
        {
            shownPattern = want;
            if (want >= 0 && set != move)
            {
                // 예고 시간 + 0.15초 동안 준비→발사 프레임을 한 번 재생하고 마지막 프레임 유지
                float playFps = set.Length / Mathf.Max(0.2f, monster.windupTime + 0.15f);
                anim.Play(set, playFps, false, true);
            }
            else anim.Play(move, fps, true, true);
        }
    }

    Sprite[] Pick(int pattern)
    {
        Sprite[] p = pattern == 0 ? pattern1 : pattern == 1 ? pattern2 : pattern == 2 ? pattern3 : pattern == 3 ? pattern4 : pattern == 4 ? pattern5 : null;
        return p != null && p.Length > 0 ? p : move;
    }
}
