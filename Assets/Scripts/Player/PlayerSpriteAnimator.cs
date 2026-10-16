using UnityEngine;

/// <summary>플레이어 프레임 애니메이션: 달리기 L/R, 점프(=대쉬 프레임) L/R, 멈추면 달리기 첫 프레임.</summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerSpriteAnimator : MonoBehaviour
{
    public SpriteFrameAnimator anim;
    public Sprite[] runLeft, runRight, dashLeft, dashRight;
    public float runFps = 10f;

    PlayerController player;
    Vector3 lastPos;
    bool facingRight = true;
    bool wasJumping;

    void Awake()
    {
        player = GetComponent<PlayerController>();
        lastPos = transform.position;
    }

    void LateUpdate()
    {
        if (anim == null) return;
        Vector3 delta = transform.position - lastPos;
        lastPos = transform.position;
        bool moving = delta.sqrMagnitude > 0.000001f && !player.IsBeingPulled;
        if (Mathf.Abs(delta.x) > 0.0005f) facingRight = delta.x > 0f;

        if (player.IsJumping)
        {
            var set = facingRight ? dashRight : dashLeft;
            if (set != null && set.Length > 0)
            {
                // 점프 시간 동안 대쉬 프레임을 한 번 재생
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
