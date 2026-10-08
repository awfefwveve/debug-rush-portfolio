using UnityEngine;

/// <summary>
/// 투사체 공용 (탄, 절단기 날, 낙하탄). 프리팹: Collider2D(IsTrigger) + Rigidbody2D(Kinematic) + SpriteRenderer.
/// parryable 투사체는 점프 중 닿으면 패링 성공 → 발사한 몬스터 정지.
/// </summary>
public class Projectile : MonoBehaviour
{
    public float speed = 6f;
    public float lifetime = 8f;
    public int damage = 1;                       // 기획서: 데미지 1 고정
    public float parryStunTime = 3f;

    [Header("패링 가능 색상 구분")]
    public Color normalColor = new Color(0.8f, 0.2f, 0.15f);
    public Color parryColor = new Color(0.7f, 0.3f, 1f);

    [Header("분열 (몬스터 1-C 패턴 1)")]
    public Projectile splitPrefab;
    public int splitCount = 0;                   // 8

    public bool Parryable { get; private set; }
    MonsterBase owner;
    Vector2 dir;

    public void Launch(MonsterBase owner, Vector2 direction, bool parryable)
    {
        this.owner = owner;
        dir = direction.normalized;
        Parryable = parryable;
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr) sr.color = parryable ? parryColor : normalColor;
        transform.right = dir;
        Destroy(gameObject, lifetime);
    }

    void Update() => transform.position += (Vector3)(dir * speed * Time.deltaTime);

    void OnTriggerEnter2D(Collider2D c)
    {
        if (c.TryGetComponent(out PlayerController p))
        {
            if (Parryable && p.IsJumping)
            {
                p.NotifyParry();
                if (owner) owner.Stun(parryStunTime);
                Destroy(gameObject);
                return;
            }
            p.Health.TakeDamage(damage);
            Hit();
        }
        else if (c.CompareTag("Wall")) Hit();
    }

    void Hit()
    {
        if (splitCount > 0 && splitPrefab != null)
        {
            for (int i = 0; i < splitCount; i++)
            {
                float ang = 360f / splitCount * i;
                var s = Instantiate(splitPrefab, transform.position, Quaternion.identity);
                s.Launch(owner, Quaternion.Euler(0, 0, ang) * Vector2.right, false);
            }
        }
        Destroy(gameObject);
    }
}
