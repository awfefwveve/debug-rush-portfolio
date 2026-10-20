using TMPro;
using UnityEngine;

/// <summary>E로 상호작용할 대상 위에 [E] 표시 (조작 안내).</summary>
public class InteractFocus : MonoBehaviour
{
    public Color glowColor = new Color(0.3f, 0.95f, 1f, 0.55f);

    PlayerController player;
    SpriteRenderer glow;
    TextMeshPro prompt;
    static Sprite roundSprite;

    void Awake() => player = GetComponent<PlayerController>();

    void Start()
    {
        glow = new GameObject("InteractGlow").AddComponent<SpriteRenderer>();
        glow.sprite = RoundSprite();
        glow.drawMode = SpriteDrawMode.Sliced;
        glow.sortingOrder = -50;
        glow.enabled = false;

        var go = new GameObject("InteractPrompt");
        prompt = go.AddComponent<TextMeshPro>();
        prompt.font = KoreanTMPFont.Get();
        prompt.text = "E";
        prompt.fontSize = 5f;
        prompt.fontStyle = FontStyles.Bold;
        prompt.alignment = TextAlignmentOptions.Center;
        prompt.color = new Color(0.3f, 0.95f, 1f);
        prompt.outlineWidth = 0.3f;
        prompt.outlineColor = new Color32(8, 12, 24, 255);
        prompt.rectTransform.sizeDelta = new Vector2(1.2f, 1f);
        prompt.GetComponent<MeshRenderer>().sortingOrder = 70;
        prompt.enabled = false;
    }

    void LateUpdate()
    {
        var target = player != null ? player.FocusTarget : null;
        bool on = target != null && !player.InputLocked;
        glow.enabled = false; prompt.enabled = on;   // 빛나는 테두리 연출은 뺌(기획서에 없음), [E] 안내만
        if (!on) return;

        var sr = target.GetComponent<SpriteRenderer>();
        Bounds b = sr != null && sr.sprite != null ? sr.bounds : new Bounds(target.transform.position, Vector3.one * 2f);
        float pulse = 0f;
        glow.transform.position = new Vector3(b.center.x, b.center.y, 0f);
        glow.size = new Vector2(b.size.x + 0.35f + pulse * 0.1f, b.size.y + 0.35f + pulse * 0.1f);
        glow.color = new Color(glowColor.r, glowColor.g, glowColor.b, glowColor.a * (0.7f + 0.3f * pulse));
        prompt.transform.position = new Vector3(b.center.x, b.max.y + 0.45f + pulse * 0.08f, 0f);
    }

    void OnDestroy()
    {
        if (glow != null) Destroy(glow.gameObject);
        if (prompt != null) Destroy(prompt.gameObject);
    }

    static Sprite RoundSprite()
    {
        if (roundSprite != null) return roundSprite;
        const int n = 64, r = 20;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                int cx = Mathf.Clamp(x, r, n - 1 - r), cy = Mathf.Clamp(y, r, n - 1 - r);
                float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                float a = d > r ? 0f : d > r - 5 ? 1f : 0.25f;          // 테두리 진하게, 안쪽 옅게
                tex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
        tex.Apply();
        roundSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 64f, 0, SpriteMeshType.FullRect, new Vector4(24, 24, 24, 24));
        return roundSprite;
    }
}
