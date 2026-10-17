using UnityEngine;

/// <summary>
/// 플레이어 프레임 애니메이션: 달리기 L/R(입력 기준), 점프(=대쉬 프레임) L/R, 정지 시 달리기 첫 프레임.
/// 이동 판정은 transform 변화가 아니라 입력으로 함 (물리 프레임과 화면 프레임이 달라 매 프레임 리셋되던 문제 수정).
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerSpriteAnimator : MonoBehaviour
{
    public SpriteFrameAnimator anim;
    public Sprite[] runLeft, runRight, dashLeft, dashRight;
    public float runFps = 11f;

    PlayerController player;
    bool facingRight = true;
    bool wasJumping;

    void Awake() => player = GetComponent<PlayerController>();

    void LateUpdate()
    {
        if (anim == null) return;
        Vector2 move = player.MoveInput;
        bool moving = move.sqrMagnitude > 0.01f && !player.IsBeingPulled && !player.InputLocked;
        if (Mathf.Abs(move.x) > 0.1f) facingRight = move.x > 0f;

        if (player.IsJumping)
        {
            var set = facingRight ? dashRight : dashLeft;
            if (set != null && set.Length > 0)
            {
                float fps = set.Length / Mathf.Max(0.05f, player.jumpDuration);
                anim.Play(set, fps, false, !wasJumping);
            }
            wasJumping = true;
            return;
        }
        wasJumping = false;

        var run = facingRight ? runRight : runLeft;
        if (run == null || run.Length == 0) return;
        if (moving) anim.Play(run, runFps, true);
        else anim.Hold(run, 0);
    }
}
