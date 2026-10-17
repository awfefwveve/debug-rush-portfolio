using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 주인공. 1칸 = 1유닛(팀 규격 32px 격자). transform 위치 = 발바닥 중앙.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(PlayerInventory), typeof(PlayerHealth))]
public class PlayerController : MonoBehaviour
{
    [Header("이동")]
    public float moveSpeed = 7f;              // 칸/초. 몬스터 moveSpeed보다 커야 함

    [Header("상호작용 (E)")]
    public float interactRadius = 1.4f;
    public float interactHeight = 0.6f;       // 발바닥에서 몸 중심까지
    public LayerMask interactMask;

    [Header("점프 = 패링 (Space)")]
    public float jumpDuration = 0.5f;
    public float jumpCooldown = 0.3f;
    public float jumpHeight = 0.8f;
    public Transform visual;                  // 스프라이트가 들어있는 자식

    public bool IsJumping { get; private set; }
    public bool IsBeingPulled { get; private set; }
    public bool InputLocked { get; set; }

    /// <summary>현재 이동 입력 (애니메이션 판정용, -1~1)</summary>
    public Vector2 MoveInput => input;
    /// <summary>E로 상호작용될 대상 (하이라이트용)</summary>
    public Component FocusTarget { get; private set; }

    /// <summary>자동 플레이(QA)용 입력. null이면 키보드.</summary>
    [NonSerialized] public Vector2? BotInput;

    public PlayerInventory Inventory { get; private set; }
    public PlayerHealth Health { get; private set; }
    public event Action ParrySucceeded;

    Rigidbody2D rb;
    Vector2 input;
    float nextJumpTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;   // 화면 프레임과 물리 프레임 사이 떨림 방지
        Inventory = GetComponent<PlayerInventory>();
        Health = GetComponent<PlayerHealth>();
        if (GetComponent<PauseMenu>() == null) gameObject.AddComponent<PauseMenu>();
        if (GetComponent<StageFX>() == null) gameObject.AddComponent<StageFX>();
        if (GetComponent<InteractFocus>() == null) gameObject.AddComponent<InteractFocus>();
        if (GetComponent<CarryDisplay>() == null) gameObject.AddComponent<CarryDisplay>();
        if (GetComponent<GameRestoreFX>() == null) gameObject.AddComponent<GameRestoreFX>();
    }

    void Update()
    {
        FocusTarget = FindTarget(out _);
        if (InputLocked || IsBeingPulled) { input = Vector2.zero; return; }

        Vector2 raw = BotInput ?? new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        input = raw.sqrMagnitude > 1f ? raw.normalized : raw;
        if (BotInput.HasValue) return;

        if (Input.GetKeyDown(KeyCode.E)) Interact();
        if (Input.GetKeyDown(KeyCode.Q)) DropLastItem();
        if (Input.GetKeyDown(KeyCode.Space)) Jump();
    }

    void FixedUpdate()
    {
        if (InputLocked || IsBeingPulled) return;
        rb.MovePosition(rb.position + input * moveSpeed * Time.fixedDeltaTime);
    }

    // ---------- 상호작용 ----------
    Vector2 BodyCenter => (Vector2)transform.position + Vector2.up * interactHeight;

    Component FindTarget(out IInteractable best)
    {
        var hits = Physics2D.OverlapCircleAll(BodyCenter, interactRadius, interactMask);
        best = null;
        Component bestComp = null;
        float bestDist = float.MaxValue;
        foreach (var h in hits)
        {
            if (!h.TryGetComponent(out IInteractable it)) continue;
            float d = (h.ClosestPoint(BodyCenter) - BodyCenter).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = it; bestComp = h; }
        }
        return bestComp;
    }

    public void Interact()
    {
        if (InputLocked) return;
        FindTarget(out var best);
        best?.Interact(this);
    }

    public void Jump()
    {
        if (InputLocked || IsJumping || Time.time < nextJumpTime) return;
        StartCoroutine(JumpRoutine());
    }

    public void DropLastItem()
    {
        if (Inventory.items.Count == 0) return;
        int index = Inventory.items.Count - 1;
        ItemType item = Inventory.items[index];
        Inventory.RemoveAt(index);           // 버리기 = 바닥에 두지 않고 바로 삭제
        if (StageFX.I != null) StageFX.I.Popup(transform.position + Vector3.up * 1.6f, ItemNames.Get(item) + " 버림", new Color(0.7f, 0.75f, 0.85f), 0.8f);
    }

    // ---------- 점프(패링) ----------
    IEnumerator JumpRoutine()
    {
        IsJumping = true;
        float t = 0f;
        while (t < jumpDuration)
        {
            t += Time.deltaTime;
            if (visual)
                visual.localPosition = new Vector3(0f, Mathf.Sin(t / jumpDuration * Mathf.PI) * jumpHeight, 0f);
            yield return null;
        }
        if (visual) visual.localPosition = Vector3.zero;
        IsJumping = false;
        nextJumpTime = Time.time + jumpCooldown;
    }

    public void NotifyParry() => ParrySucceeded?.Invoke();

    // ---------- 자기장에 끌려감 ----------
    public void Pull(Vector2 target, float duration)
    {
        if (!IsBeingPulled) StartCoroutine(PullRoutine(target, duration));
    }

    IEnumerator PullRoutine(Vector2 target, float duration)
    {
        IsBeingPulled = true;
        Vector2 start = rb.position;
        float t = 0f;
        while (t < duration)
        {
            t += Time.fixedDeltaTime;
            rb.MovePosition(Vector2.Lerp(start, target, t / duration));
            yield return new WaitForFixedUpdate();
        }
        IsBeingPulled = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * interactHeight, interactRadius);
    }
}
