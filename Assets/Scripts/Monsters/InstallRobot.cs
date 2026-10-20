using UnityEngine;

/// <summary>
/// 몬스터2 패턴 2-B 설치 로봇. 주변에 빨간 감지 원을 보여 주고, 플레이어가 원에 들어오면 n초 뒤 폭발.
/// 폭발 순간 폭발 범위 안이면 데미지 1. 몬스터는 발동시키지 않음.
/// </summary>
public class InstallRobot : MonoBehaviour
{
    float detect = 1.8f, delay = 1f, radius = 1.6f;
    float explodeAt = -1f;
    Transform player;
    SpriteRenderer ring;
    static Sprite circle;

    public void Setup(float detectRadius, float delaySec, float explodeRadius)
    {
        detect = detectRadius; delay = delaySec; radius = explodeRadius;
    }

    void Start()
    {
        var pc = Object.FindAnyObjectByType<PlayerController>();
        if (pc != null) player = pc.transform;
        foreach (var c in GetComponentsInChildren<Collider2D>()) c.enabled = false;   // 밟아서 터지는 판정 끔
        // 빨간 감지 원 (기획서 그림)
        var fx = FxSet.Get();
        var g = new GameObject("DetectRing");
        g.transform.SetParent(transform, false);
        ring = g.AddComponent<SpriteRenderer>();
        ring.sprite = fx != null && fx.warnCircleBorder != null ? fx.warnCircleBorder : Circle();
        float s = ring.sprite.bounds.size.x > 0.01f ? detect * 2f / ring.sprite.bounds.size.x : 1f;
        g.transform.localScale = new Vector3(s / transform.lossyScale.x, s / transform.lossyScale.y, 1f);
        ring.color = new Color(1f, 0.3f, 0.35f, 0.6f);
        ring.sortingOrder = 21;
        // 기획서 그림처럼 감지 범위를 빨간 원으로 채움
        if (fx != null && fx.warnCircleFill != null)
        {
            var fill = new GameObject("DetectFill").AddComponent<SpriteRenderer>();
            fill.transform.SetParent(transform, false);
            fill.sprite = fx.warnCircleFill;
            float fs = detect * 2f / Mathf.Max(0.01f, fx.warnCircleFill.bounds.size.x);
            fill.transform.localScale = new Vector3(fs / transform.lossyScale.x, fs / transform.lossyScale.y, 1f);
            fill.color = new Color(0.85f, 0.2f, 0.15f, 0.35f);
            fill.sortingOrder = -1;
        }
    }

    void Update()
    {
        if (player == null) return;
        float d = Vector2.Distance(player.position, transform.position);
        if (explodeAt < 0f)
        {
            if (d <= detect) explodeAt = Time.time + delay;
            return;
        }
        // 터지기 전: 감지 원을 진하게 깜빡여서 곧 터진다는 걸 보여 줌
        if (ring != null) ring.color = new Color(1f, 0.25f, 0.3f, Mathf.PingPong(Time.time * 4f, 0.6f) + 0.4f);
        if (Time.time < explodeAt) return;
        var pc = player.GetComponent<PlayerController>();
        if (pc != null && d <= radius && !pc.Health.IsInvincible) pc.Health.TakeDamage(1);
        var fx = FxSet.Get();
        if (fx != null) SpriteFrameAnimator.Spawn(fx.vanishFx, transform.position, 22f, 35, radius);
        Destroy(gameObject);
    }

    static Sprite Circle()
    {
        if (circle != null) return circle;
        var t = new Texture2D(64, 64); var px = new Color[64 * 64];
        for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
        { float r = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f)); px[y * 64 + x] = r < 31 && r > 27 ? Color.white : Color.clear; }
        t.SetPixels(px); t.Apply();
        return circle = Sprite.Create(t, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 32f);
    }
}
