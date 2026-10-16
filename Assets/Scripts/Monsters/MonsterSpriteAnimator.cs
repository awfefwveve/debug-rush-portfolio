using UnityEngine;

/// <summary>몬스터 프레임 애니메이션: 평소 = 움직임, 패턴 실행 중 = 패턴별 프레임, 스턴 중 = 정지.</summary>
[RequireComponent(typeof(MonsterBase))]
public class MonsterSpriteAnimator : MonoBehaviour
{
    public SpriteFrameAnimator anim;
    public Sprite[] move;
    [Tooltip("index 0 = 패턴1, 1 = 패턴2 …")]
    public Sprite[] pattern1, pattern2, pattern3;
    public float fps = 10f;

    MonsterBase monster;

    void Awake() => monster = GetComponent<MonsterBase>();

    void LateUpdate()
    {
        if (anim == null) return;
        anim.Paused = monster.IsStunned;
        var set = Pick(monster.CurrentPattern);
        if (set != null && set.Length > 0) anim.Play(set, fps, true);
    }

    Sprite[] Pick(int pattern)
    {
        Sprite[] p = pattern == 0 ? pattern1 : pattern == 1 ? pattern2 : pattern == 2 ? pattern3 : null;
        return p != null && p.Length > 0 ? p : move;
    }
}
