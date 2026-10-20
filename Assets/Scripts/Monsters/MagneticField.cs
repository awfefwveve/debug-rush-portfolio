using UnityEngine;

/// <summary>
/// 몬스터1 패턴 1-B 자기장. 범위 안에 들어온 플레이어를 몬스터 쪽으로 끌어당김(이것만으로는 데미지 없음).
/// 끌려가서 몬스터 몸에 닿으면 데미지 → 플레이어 무적 시간 동안은 안 끌려서 빠져나갈 수 있음.
/// </summary>
public class MagneticField : MonoBehaviour
{
    public float lifetime = 3f;
    public float pullDuration = 0.4f;
    [Tooltip("몬스터 몸에 닿았다고 보는 거리(칸)")] public float contactRadius = 0.7f;
    [HideInInspector] public Transform center;

    float baseRadius = -1f;

    void Start()
    {
        // 파장은 단말기·재료대 등 맵 물체보다 위에 보이게 (탄환 30보다는 아래)
        foreach (var r in GetComponentsInChildren<SpriteRenderer>()) r.sortingOrder = 25;
        Destroy(gameObject, lifetime);
    }

    /// <summary>자기장 반지름(칸) 맞추기: 그림과 판정 원을 같이 키움</summary>
    public void SetRadius(float radius)
    {
        var c = GetComponent<CircleCollider2D>();
        if (c == null) return;
        if (baseRadius < 0f) baseRadius = c.radius * transform.localScale.x;
        if (baseRadius <= 0.01f) return;
        transform.localScale *= radius / baseRadius;
        baseRadius = radius;
    }

    void OnTriggerStay2D(Collider2D c)
    {
        if (!c.TryGetComponent(out PlayerController p)) return;
        if (p.Health.IsInvincible) return;                        // 맞은 뒤 무적이면 안 끌림 → 빠져나감
        Vector2 at = center ? (Vector2)center.position : (Vector2)transform.position;
        if (Vector2.Distance(p.transform.position, at) <= contactRadius) { p.Health.TakeDamage(1); return; }
        if (!p.IsBeingPulled) p.Pull(at, pullDuration);
    }
}
