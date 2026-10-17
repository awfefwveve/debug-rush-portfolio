using TMPro;
using UnityEngine;

/// <summary>
/// 단말기 표시 (기획서 p.10 + 팀 아트 규격):
/// - 본체 그림 3상태: 오류(요청 대기) / 전송(납품 직후) / 복구(수리 완료)
/// - 노란 요구사항 박스 + 요구 아이콘, 완료 게이지 칸
/// - 남은 시간: 그림과 분리된 UI (칸이 줄어드는 바 + 남은 초)
/// </summary>
public class TerminalView : MonoBehaviour
{
    public BugTerminal terminal;
    public ItemIconSet icons;
    public SpriteRenderer[] requestIcons;      // 2개
    public SpriteRenderer[] gaugeCells;        // 8개
    public Color filled = new Color(1f, 0.8f, 0.25f);
    public Color empty = new Color(0.35f, 0.35f, 0.35f);

    [Header("본체 3상태 (오류 / 전송 / 복구)")]
    public Sprite errorSprite;
    public Sprite transmitSprite;
    public Sprite recoveredSprite;
    public float transmitShowTime = 0.6f;

    [Header("요구사항 박스")]
    public Color requestBoxColor = new Color(1f, 0.85f, 0.1f, 0.95f);
    public float requestBoxPadding = 0.12f;

    [Header("남은 시간 바 (그림과 분리된 UI)")]
    public int timerCells = 6;
    public Color timerColor = new Color(0.3f, 0.9f, 1f);
    public Color timerWarnColor = new Color(1f, 0.3f, 0.25f);
    [Range(0f, 1f)] public float warnRatio = 0.34f;

    SpriteRenderer body, requestBox;
    SpriteRenderer[] cells;
    TextMeshPro timerText;
    float transmitUntil;
    int lastCompleted, lastRemaining;
    static Sprite whiteSprite;

    void OnEnable() => terminal.Changed += Refresh;
    void OnDisable() => terminal.Changed -= Refresh;

    void Start()
    {
        body = GetComponent<SpriteRenderer>();
        lastCompleted = terminal.Completed; lastRemaining = terminal.Remaining.Count;
        Refresh();
    }

    void Refresh()
    {
        // 납품 감지 → 전송 상태 잠깐 표시
        if (terminal.Completed > lastCompleted || (terminal.Completed == lastCompleted && terminal.Remaining.Count < lastRemaining))
            transmitUntil = Time.time + transmitShowTime;
        lastCompleted = terminal.Completed; lastRemaining = terminal.Remaining.Count;

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

    void Update()
    {
        if (body != null)
        {
            Sprite s = terminal.IsDone ? recoveredSprite : Time.time < transmitUntil ? transmitSprite : errorSprite;
            if (s != null) body.sprite = s;
        }
        RefreshTimer();
    }

    void RefreshTimer()
    {
        if (cells == null) CreateTimer();
        bool on = terminal.HasTimer;
        float ratio = terminal.TimeRatio;
        int lit = on ? Mathf.CeilToInt(ratio * cells.Length) : 0;
        Color c = ratio <= warnRatio ? timerWarnColor : timerColor;
        for (int i = 0; i < cells.Length; i++)
        {
            cells[i].enabled = on;
            cells[i].color = i < lit ? c : new Color(0.15f, 0.18f, 0.28f, 0.9f);
        }
        timerText.enabled = on;
        if (on) { timerText.text = Mathf.CeilToInt(terminal.TimeLeft) + "초"; timerText.color = c; }

        // 요구사항 박스 바로 위
        Vector3 anchor = requestBox != null && requestBox.enabled
            ? new Vector3(requestBox.bounds.center.x, requestBox.bounds.max.y + 0.25f, 0f)
            : transform.position + Vector3.up * 2.4f;
        const float w = 0.26f, gap = 0.06f;
        float total = cells.Length * w + (cells.Length - 1) * gap;
        for (int i = 0; i < cells.Length; i++)
            cells[i].transform.position = anchor + new Vector3(-total * 0.5f + w * 0.5f + i * (w + gap), 0f, 0f);
        timerText.transform.position = anchor + new Vector3(total * 0.5f + 0.55f, 0f, 0f);
    }

    void CreateTimer()
    {
        EnsureWhiteSprite();
        cells = new SpriteRenderer[Mathf.Max(1, timerCells)];
        for (int i = 0; i < cells.Length; i++)
        {
            var go = new GameObject("TimerCell" + i);
            go.transform.SetParent(transform, true);
            Vector3 parent = transform.lossyScale;
            go.transform.localScale = new Vector3(0.26f / Mathf.Max(0.0001f, parent.x), 0.2f / Mathf.Max(0.0001f, parent.y), 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = whiteSprite; sr.sortingOrder = 45;
            cells[i] = sr;
        }
        var textGo = new GameObject("TimerText");
        textGo.transform.SetParent(transform, true);
        timerText = textGo.AddComponent<TextMeshPro>();
        timerText.font = KoreanTMPFont.Get();
        timerText.fontSize = 3.2f; timerText.fontStyle = FontStyles.Bold;
        timerText.alignment = TextAlignmentOptions.Center;
        timerText.outlineWidth = 0.25f; timerText.outlineColor = new Color32(8, 12, 24, 255);
        timerText.rectTransform.sizeDelta = new Vector2(1.6f, 0.6f);
        timerText.GetComponent<MeshRenderer>().sortingOrder = 46;
    }

    void RefreshRequestBox()
    {
        if (requestBox == null)
        {
            EnsureWhiteSprite();
            var go = new GameObject("RequestBox");
            go.transform.SetParent(transform, false);
            requestBox = go.AddComponent<SpriteRenderer>();
            requestBox.sprite = whiteSprite; requestBox.color = requestBoxColor;
            requestBox.sortingOrder = 43;
        }
        bool any = false;
        Bounds b = default;
        foreach (var sr in requestIcons)
        {
            if (sr == null || !sr.enabled) continue;
            sr.sortingOrder = 44;
            var ib = sr.sprite != null ? sr.bounds : new Bounds(sr.transform.position, Vector3.one * 0.5f);
            if (!any) { b = ib; any = true; } else b.Encapsulate(ib);
        }
        requestBox.enabled = any;
        if (!any) return;
        var t = requestBox.transform;
        t.position = new Vector3(b.center.x, b.center.y, 0f);
        Vector3 world = new Vector3(b.size.x + requestBoxPadding * 2f, b.size.y + requestBoxPadding * 2f, 1f);
        Vector3 p = t.parent != null ? t.parent.lossyScale : Vector3.one;
        t.localScale = new Vector3(world.x / Mathf.Max(0.0001f, p.x), world.y / Mathf.Max(0.0001f, p.y), 1f);
    }

    static void EnsureWhiteSprite()
    {
        if (whiteSprite != null) return;
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        whiteSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }
}
