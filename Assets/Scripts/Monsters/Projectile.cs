using UnityEngine;

/// <summary>
/// 투사체 공용 (탄, 절단기 날, 낙하탄). 프리팹: Collider2D(IsTrigger) + Rigidbody2D(Kinematic) + SpriteRenderer.
/// parryable 투사체는 점프 중 닿으면 패링 성공 → 발사한 몬스터 정지.
/// 팀 규격: 일반 탄 16×16 빨강, 패링 가능 탄 24×24 분홍 마름모. 탄은 고정 그림(깜빡임 없음), 불투명.
/// </summary>
public class Projectile : MonoBehaviour
{
    public float speed = 7f;
    public float lifetime = 8f;
    public int damage = 1;                       // 기획서: 데미지 1 고정
    public float parryStunTime = 3f;

    [Header("모양 (비우면 FxSet 공용 탄 사용, useSharedBullet=false면 프리팹 그림 유지)")]
    public bool useSharedBullet = true;
    public Sprite normalSprite, parrySprite;
    [Tooltip("탄 그림을 진행 방향으로 회전 (칼날 등)")] public bool rotateToDirection = false;
    [Tooltip("그림이 위를 보고 그려짐 (톱: 톱니가 위) → 위쪽을 진행 방향으로")] public bool artFacesUp = false;

    [Header("분열 (몬스터 1-C 패턴 1)")]
    public Projectile splitPrefab;
    public int splitCount = 0;                   // 8
    [System.NonSerialized] public float splitParryChance;   // 갈라진 탄 중 1발이 패링 가능할 확률 (보스 3-A)
    [System.NonSerialized] public bool visualOnly;          // 그림만 (보스 3-C 낙하물: 떨어지는 순간 표시 원 안에서만 판정, 떨어지는 길에는 판정 없음)

    public bool Parryable { get; private set; }
    MonsterBase owner;
    Vector2 dir;

    public void Launch(MonsterBase owner, Vector2 direction, bool parryable)
    {
        this.owner = owner;
        dir = direction.normalized;
        Parryable = parryable;

        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr)
        {
            var fx = FxSet.Get();
            Sprite s = parryable ? parrySprite : normalSprite;
            if (s == null && useSharedBullet && fx != null) s = parryable ? fx.bulletParry : fx.bulletNormal;
            if (s != null) sr.sprite = s;
            sr.color = Color.white;
            sr.sortingOrder = 30;
            // 프레임 애니메이션이 붙어 있으면 제거 (탄 깜빡임 원인)
            var anim = sr.GetComponent<SpriteFrameAnimator>();
            if (anim != null) Destroy(anim);
        }
        if (rotateToDirection && artFacesUp) transform.up = dir;
        else if (rotateToDirection) transform.right = dir;
        else transform.rotation = Quaternion.identity;
        Destroy(gameObject, lifetime);
    }

    void Update() => transform.position += (Vector3)(dir * speed * Time.deltaTime);

    void OnTriggerEnter2D(Collider2D c)
    {
        if (visualOnly) return;
        if (c.TryGetComponent(out PlayerController p))
        {
            if (Parryable && p.IsJumping)
            {
                p.NotifyParry();
                if (owner) owner.Stun(parryStunTime);
                Vanish();
                Destroy(gameObject);
                return;
            }
            if (p.Health.IsInvincible) return;           // 무적 중엔 통과 (탄이 사라지며 깜빡이지 않게)
            p.Health.TakeDamage(damage);
            var fx = FxSet.Get();
            if (fx != null) SpriteFrameAnimator.Spawn(fx.hitFx, transform.position, 20f, 35, 1f);
            Hit();
        }
        else if (c.CompareTag("Wall")) Hit();
        // 기계(재료대·제작대·단말기)에 닿아도 사라짐 (수정 기획서)
        else if (c.GetComponentInParent<ResourceStation>() != null || c.GetComponentInParent<CraftingTable>() != null || c.GetComponentInParent<BugTerminal>() != null) Hit();
    }

    void Vanish()
    {
        var fx = FxSet.Get();
        if (fx != null) SpriteFrameAnimator.Spawn(fx.vanishFx, transform.position, 20f, 35, 1f);
    }

    void Hit()
    {
        if (splitCount > 0 && splitPrefab != null)
        {
            int parryIdx = Random.value < splitParryChance ? Random.Range(0, splitCount) : -1;
            for (int i = 0; i < splitCount; i++)
            {
                float ang = 360f / splitCount * i;
                var s = Instantiate(splitPrefab, transform.position, Quaternion.identity);
                s.Launch(owner, Quaternion.Euler(0, 0, ang) * Vector2.right, i == parryIdx);
            }
        }
        Vanish();
        Destroy(gameObject);
    }
}
