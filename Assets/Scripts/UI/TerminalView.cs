using UnityEngine;

/// <summary>단말기 월드 UI: 노란 요구사항 박스 + 요구사항 아이콘 + 완료 게이지 칸(최대 8) (기획서 p.10)</summary>
public class TerminalView : MonoBehaviour
{
    public BugTerminal terminal;
    public ItemIconSet icons;
    public SpriteRenderer[] requestIcons;      // 2개
    public SpriteRenderer[] gaugeCells;        // 8개
    public Color filled = new Color(1f, 0.8f, 0.25f);
    public Color empty = new Color(0.35f, 0.35f, 0.35f);

    [Header("요구사항 박스")]
    public Color requestBoxColor = new Color(1f, 0.85f, 0.1f, 0.95f);
    public float requestBoxPadding = 0.12f;

    [Header("남은 시간 칸 (단말기 그림 프레임)")]
    [Tooltip("남은 시간 표시 프레임. [0] = 시간 가득 … [마지막] = 시간 없음. 비우면 임시 칸 막대를 그림")]
    public Sprite[] timerFrames;
    [Tooltip("프레임을 바꿀 SpriteRenderer (단말기 본체). 비우면 이 오브젝트의 SpriteRenderer")]
    public SpriteRenderer timerRenderer;
    public int fallbackCells = 6;
    public Color timerColor = new Color(0.3f, 0.9f, 1f);
    public Color timerWarnColor = new Color(1f, 0.3f, 0.25f);
    [Range(0f, 1f)] public float warnRatio = 0.3f;

    SpriteRenderer requestBox;
    SpriteRenderer[] timerCells;
    TMPro.TextMeshPro timerText;
    static Sprite whiteSprite;

    void OnEnable() => terminal.Changed += Refresh;
    void OnDisable() => terminal.Changed -= Refresh;
    void Start()
    {
        if (timerRenderer == null) timerRenderer = GetComponent<SpriteRenderer>();
        Refresh();
    }

    void Update() => RefreshTimer();

    void RefreshTimer()
    {
        bool on = terminal.HasTimer;
        float ratio = terminal.TimeRatio;

        if (timerFrames != null && timerFrames.Length > 0 && timerRenderer != null)
        {
            // 남은 시간에 따라 단말기 그림 프레임 교체
            int idx = on ? Mathf.Clamp(Mathf.FloorToInt((1f - ratio) * timerFrames.Length), 0, timerFrames.Length - 1) : 0;
            if (timerFrames[idx] != null) timerRenderer.sprite = timerFrames[idx];
            return;
        }

        // 임시: 요구사항 박스 위에 남은 시간 칸 + 초
        if (timerCells == null) CreateFallbackTimer();
        int filledCells = on ? Mathf.CeilToInt(ratio * timerCells.Length) : 0;
        Color c = ratio <= warnRatio ? timerWarnColor : timerColor;
        for (int i = 0; i < timerCells.Length; i++)
        {
            timerCells[i].enabled = on;
            timerCells[i].color = i < filledCells ? c : new Color(0.2f, 0.2f, 0.2f, 0.8f);
        }
        timerText.enabled = on;
        if (on)
        {
            timerText.text = Mathf.CeilToInt(terminal.TimeLeft) + "초";
            timerText.color = c;
        }

        // 요구사항 박스 바로 위에 배치
        Vector3 anchor = requestBox != null && requestBox.enabled
            ? new Vector3(requestBox.bounds.center.x, requestBox.bounds.max.y + 0.18f, 0f)
            : transform.position + Vector3.up * 1.2f;
        const float w = 0.16f, gap = 0.04f;
        float total = timerCells.Length * w + (timerCells.Length - 1) * gap;
        for (int i = 0; i < timerCells.Length; i++)
            timerCells[i].transform.position = anchor + new Vector3(-total * 0.5f + w * 0.5f + i * (w + gap), 0f, 0f);
        timerText.transform.position = anchor + new Vector3(total * 0.5f + 0.35f, 0f, 0f);
    }

    void CreateFallbackTimer()
    {
        EnsureWhiteSprite();
        var first = requestIcons.Length > 0 ? requestIcons[0] : null;
        timerCells = new SpriteRenderer[Mathf.Max(1, fallbackCells)];
        for (int i = 0; i < timerCells.Length; i++)
        {
            var go = new GameObject("TimerCell" + i);
            go.transform.SetParent(transform, true);
            go.transform.localScale = Vector3.one;
            Vector3 parent = transform.lossyScale;
            go.transform.localScale = new Vector3(0.16f / Mathf.Max(0.0001f, parent.x), 0.12f / Mathf.Max(0.0001f, parent.y), 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = whiteSprite;
            if (first != null) { sr.sortingLayerID = first.sortingLayerID; sr.sortingOrder = first.sortingOrder; }
            timerCells[i] = sr;
        }

        var textGo = new GameObject("TimerText");
        textGo.transform.SetParent(transform, true);
        timerText = textGo.AddComponent<TMPro.TextMeshPro>();
        timerText.font = KoreanTMPFont.Get();
        timerText.fontSize = 2.4f;
        timerText.alignment = TMPro.TextAlignmentOptions.Center;
        timerText.rectTransform.sizeDelta = new Vector2(1.2f, 0.4f);
        var mr = timerText.GetComponent<MeshRenderer>();
        if (first != null && mr != null) { mr.sortingLayerID = first.sortingLayerID; mr.sortingOrder = first.sortingOrder + 1; }
    }

    static void EnsureWhiteSprite()
    {
        if (whiteSprite != null) return;
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        whiteSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }

    void Refresh()
    {
        for (int i = 0; i < requestIcons.Length; i++)
        {
            bool on = i < terminal.Remaining.Count;
            requestIcons[i].enabled = on;
            if (on) requestIcons[i].sprite = icons.Get(terminal.Remaining[i]);
        }
        for (int i = 0; i < gaugeCells.Length; i++)
        {
            gaugeCells[i].enabled = i < terminal.goal;
            gaugeCells[i].color = i < terminal.Completed ? filled : empty;
        }
        RefreshRequestBox();
    }

    void RefreshRequestBox()
    {
        if (requestBox == null) requestBox = CreateRequestBox();

        bool any = false;
        Bounds b = default;
        foreach (var sr in requestIcons)
        {
            if (sr == null || !sr.enabled) continue;
            var ib = sr.sprite != null ? sr.bounds : new Bounds(sr.transform.position, Vector3.one * 0.5f);
            if (!any) { b = ib; any = true; } else b.Encapsulate(ib);
        }
        requestBox.enabled = any;
        if (!any) return;

        var t = requestBox.transform;
        t.position = new Vector3(b.center.x, b.center.y, t.position.z);
        Vector3 world = new Vector3(b.size.x + requestBoxPadding * 2f, b.size.y + requestBoxPadding * 2f, 1f);
        Vector3 parent = t.parent != null ? t.parent.lossyScale : Vector3.one;
        t.localScale = new Vector3(world.x / Mathf.Max(0.0001f, parent.x), world.y / Mathf.Max(0.0001f, parent.y), 1f);
    }

    SpriteRenderer CreateRequestBox()
    {
        EnsureWhiteSprite();
        var go = new GameObject("RequestBox");
        go.transform.SetParent(transform, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = whiteSprite;
        sr.color = requestBoxColor;
        var first = requestIcons.Length > 0 ? requestIcons[0] : null;
        if (first != null)
        {
            sr.sortingLayerID = first.sortingLayerID;
            sr.sortingOrder = first.sortingOrder - 1;
        }
        return sr;
    }
}
