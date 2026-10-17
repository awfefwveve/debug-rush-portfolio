using UnityEngine;

/// <summary>바닥 위험요소: 전선(점프로 회피 가능), 설치 로봇/지뢰(밟으면 폭발).</summary>
public class Hazard : MonoBehaviour
{
    public int damage = 1;
    public bool jumpable = false;          // 전선=true
    public bool destroyOnHit = false;      // 지뢰=true
    public float lifetime = 0f;            // 0이면 영구 (지뢰), 전선은 3초 등

    void Start()
    {
        foreach (var r in GetComponentsInChildren<SpriteRenderer>()) r.sortingOrder = 22;   // 전선·낙하 표시는 맵 물체보다 위
        if (lifetime > 0f) Destroy(gameObject, lifetime);
    }

    void OnTriggerStay2D(Collider2D c)
    {
        if (!c.TryGetComponent(out PlayerController p)) return;
        if (jumpable && p.IsJumping) return;
        if (p.Health.IsInvincible) return;

        p.Health.TakeDamage(damage);
        if (destroyOnHit) Destroy(gameObject);
    }
}
