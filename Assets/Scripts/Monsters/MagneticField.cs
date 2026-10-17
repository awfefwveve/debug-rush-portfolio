using UnityEngine;

/// <summary>몬스터 1-A 패턴 2. 닿으면 몬스터에게 끌려가며 데미지(이후 무적).</summary>
public class MagneticField : MonoBehaviour
{
    public float lifetime = 3f;
    public float pullDuration = 0.4f;
    [HideInInspector] public Transform center;

    void Start()
    {
        // 파장은 단말기·재료대 등 맵 물체보다 위에 보이게 (탄환 30보다는 아래)
        foreach (var r in GetComponentsInChildren<SpriteRenderer>()) r.sortingOrder = 25;
        Destroy(gameObject, lifetime);
    }

    void OnTriggerStay2D(Collider2D c)
    {
        if (!c.TryGetComponent(out PlayerController p)) return;
        if (p.IsBeingPulled || p.Health.IsInvincible) return;

        p.Health.TakeDamage(1);
        p.Pull(center ? (Vector2)center.position : (Vector2)transform.position, pullDuration);
    }
}
