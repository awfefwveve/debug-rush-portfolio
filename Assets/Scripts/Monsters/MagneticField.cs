using UnityEngine;

/// <summary>몬스터 1-A 패턴 2. 닿으면 몬스터에게 끌려가며 데미지(이후 무적).</summary>
public class MagneticField : MonoBehaviour
{
    public float lifetime = 3f;
    public float pullDuration = 0.4f;
    [HideInInspector] public Transform center;

    void Start() => Destroy(gameObject, lifetime);

    void OnTriggerStay2D(Collider2D c)
    {
        if (!c.TryGetComponent(out PlayerController p)) return;
        if (p.IsBeingPulled || p.Health.IsInvincible) return;

        p.Health.TakeDamage(1);
        p.Pull(center ? (Vector2)center.position : (Vector2)transform.position, pullDuration);
    }
}
