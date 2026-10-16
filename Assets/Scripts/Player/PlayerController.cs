using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(PlayerInventory), typeof(PlayerHealth))]
public class PlayerController : MonoBehaviour
{
    [Header("이동")]
    public float moveSpeed = 5f;              // 몬스터 moveSpeed보다 커야 함

    [Header("상호작용 (E)")]
    public float interactRadius = 1.1f;
    public LayerMask interactMask;

    [Header("점프 = 패링 (Space)")]
    public float jumpDuration = 0.5f;
    public float jumpCooldown = 0.3f;
    public float jumpHeight = 0.6f;
    public Transform visual;                  // 스프라이트가 들어있는 자식

    public bool IsJumping { get; private set; }
    public bool IsBeingPulled { get; private set; }
    public bool InputLocked { get; set; }

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
        Inventory = GetComponent<PlayerInventory>();
        Health = GetComponent<PlayerHealth>();
        if (GetComponent<PauseMenu>() == null) gameObject.AddComponent<PauseMenu>();
        if (GetComponent<StageFX>() == null) gameObject.AddComponent<StageFX>();
    }

    void Update()
    {
        if (InputLocked || IsBeingPulled) { input = Vector2.zero; return; }

        input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;

        if (Input.GetKeyDown(KeyCode.E)) TryInteract();
        if (Input.GetKeyDown(KeyCode.Q)) DropLastItem();
        if (Input.GetKeyDown(KeyCode.Space) && !IsJumping && Time.time >= nextJumpTime)
            StartCoroutine(JumpRoutine());
    }

    void FixedUpdate()
    {
        if (InputLocked || IsBeingPulled) return;
        rb.MovePosition(rb.position + input * moveSpeed * Time.fixedDeltaTime);
    }

    // ---------- 상호작용 ----------
    void TryInteract()
    {
        var hits = Physics2D.OverlapCircleAll(transform.position, interactRadius, interactMask);
        IInteractable best = null;
        float bestDist = float.MaxValue;
        foreach (var h in hits)
        {
            if (!h.TryGetComponent(out IInteractable it)) continue;
            float d = ((Vector2)h.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = it; }
        }
        best?.Interact(this);
    }

    void DropLastItem()
    {
        if (Inventory.items.Count == 0) return;
        int index = Inventory.items.Count - 1;
        ItemType item = Inventory.items[index];
        Inventory.RemoveAt(index);
        var dropped = new GameObject("Dropped " + item);
        dropped.transform.position = transform.position + Vector3.down * 0.45f;
        var drop = dropped.AddComponent<DroppedItem>();
        drop.Setup(item);
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
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
