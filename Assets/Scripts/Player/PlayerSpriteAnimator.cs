using UnityEngine;

/// <summary>
/// 플레이어 프레임 애니메이션: 달리기 L/R(입력 기준), 대쉬(Shift) L/R, 점프(패링) 중엔 달리기 3번(크게 뛴 자세) 고정, 정지 시 달리기 첫 프레임.
/// 이동 판정은 transform 변화가 아니라 입력으로 함 (물리 프레임과 화면 프레임이 달라 매 프레임 리셋되던 문제 수정).
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerSpriteAnimator : MonoBehaviour
{
    public SpriteFrameAnimator anim;
    public Sprite[] runLeft, runRight, dashLeft, dashRight;
    [Tooltip("서 있을 때 그림 (없으면 달리기 첫 장)")] public Sprite idle;
    [Tooltip("서 있을 때 그림 방향별 (다리 벌린 기본 자세 = 달리기 5번)")] public Sprite idleLeft, idleRight;
    public float runFps = 8f;

    PlayerController player;
    bool facingRight = true;
    bool wasDashing;
    Sprite[] idleL, idleR;

    void Awake() { player = GetComponent<PlayerController>(); idleL = new[] { idleLeft }; idleR = new[] { idleRight }; }

    void LateUpdate()
    {
        if (anim == null) return;
        Vector2 move = player.MoveInput;
        bool moving = move.sqrMagnitude > 0.01f && !player.IsBeingPulled && !player.InputLocked;
        if (player.IsDashing && Mathf.Abs(player.DashDir.x) > 0.1f) facingRight = player.DashDir.x > 0f;
        else if (Mathf.Abs(move.x) > 0.1f) facingRight = move.x > 0f;
        if (anim.target != null) anim.target.flipX = false;

        if (player.IsDashing)
        {
            var set = facingRight ? dashRight : dashLeft;
            if (set != null && set.Length > 0)
            {
                // 대쉬 시간 동안 프레임 전체를 한 번
                float fps = set.Length / Mathf.Max(0.05f, player.dashDuration);
                anim.Play(set, fps, false, !wasDashing);
                wasDashing = true;
                return;
            }
        }
        wasDashing = false;

        var run = facingRight ? runRight : runLeft;
        if (run == null || run.Length == 0) return;
        if (player.IsJumping) { anim.Hold(run, Mathf.Min(2, run.Length - 1)); return; }
        if (moving) anim.Play(run, runFps, true);
        else if ((facingRight ? idleRight : idleLeft) != null) anim.Hold(facingRight ? idleR : idleL, 0);   // 다리 벌린 기본 자세
        else if (idle != null) { anim.Hold(new[] { idle }, 0); if (anim.target != null) anim.target.flipX = !facingRight; }
        else anim.Hold(run, 0);
    }
}
