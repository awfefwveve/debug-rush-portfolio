using TMPro;
using UnityEngine;

/// <summary>
/// 단말기 표시 (기획서 p.10 + 팀 아트 규격):
/// - 본체 그림 3상태: 오류(요청 대기) / 전송(납품 직후) / 복구(수리 완료)
/// - 요구사항 홀로그램(단말기 위 V자 빔 + 아이템창 + 아이콘), 완료 게이지 칸
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

    [Header("요구사항 홀로그램 (단말기 위에서 위로 퍼지는 V자 빔 + 아이템창 item.png + 아이콘)")]
    public Color beamColor = new Color(1f, 0.22f, 0.45f, 0.9f);        // 아이템창(item.png) 분홍 계열
    public Color hologramTint = new Color(1f, 1f, 1f, 0.9f);           // 아이콘은 원래 색 + 살짝 투명
    [Range(0f, 1f)] public float windowAlpha = 0.8f;
    public float beamMargin = 0.35f;

    [Header("남은 시간 바 (그림과 분리된 UI)")]
    public int timerCells = 6;
    public Color timerColor = new Color(0.3f, 0.9f, 1f);
    public Color timerWarnColor = new Color(1f, 0.3f, 0.25f);
    [Range(0f, 1f)] public float warnRatio = 0.34f;

    SpriteRenderer body, requestBox, window;
    SpriteRenderer[] cells;
    TextMeshPro timerText;
    float transmitUntil;
    int lastCompleted, lastRemaining;
    static Sprite whiteSprite;
    Vector3[] iconBase;

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

        if (iconBase == null)
        {
            iconBase = new Vector3[requestIcons.Length];
            for (int i = 0; i < requestIcons.Length; i++) iconBase[i] = requestIcons[i].transform.localPosition;
        }
        int count = Mathf.Min(terminal.Remaining.Count, requestIcons.Length);
        for (int i = 0; i < requestIcons.Length; i++)
        {
            // 남은 요구 아이콘을 빔 가운데로 모음 (1개 남으면 가운데)
            float cx = (iconBase[0].x + iconBase[requestIcons.Length - 1].x) * 0.5f;
            float step = requestIcons.Length > 1 ? iconBase[1].x - iconBase[0].x : 0f;
            requestIcons[i].transform.localPosition = new Vector3(cx + (i - (count - 1) * 0.5f) * step, iconBase[i].y, iconBase[i].z);
            bool on = i < terminal.Remaining.Count;
            requestIcons[i].enabled = on;
            if (on) { requestIcons[i].sprite = icons.Get(terminal.Remaining[i]); requestIcons[i].color = hologramTint; }
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
        if (requestBox != null && requestBox.enabled)   // 홀로그램 은은하게 깜빡임
            requestBox.color = new Color(beamColor.r, beamColor.g, beamColor.b, beamColor.a * (0.85f + 0.15f * Mathf.Sin(Time.time * 5f)));
        RefreshTimer();
    }

    int shownSec = -1;

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
        if (on)
        {
            int sec = Mathf.CeilToInt(terminal.TimeLeft);
            if (sec != shownSec) { shownSec = sec; timerText.SetText("{0}초", sec); }   // 바뀔 때만 (매 프레임 글자 새로 만들면 렉)
            timerText.color = c;
        }

        // 단말기 아래 완료 칸 밑 (위쪽에 두면 홀로그램이 높아져 화면 위 '조합표(Tab)'와 겹침)
        Vector3 anchor = body != null
            ? new Vector3(transform.position.x, body.bounds.min.y - 0.75f, 0f)
            : transform.position + Vector3.down * 2.9f;
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
            var go = new GameObject("HologramBeam");
            go.transform.SetParent(transform, false);
            requestBox = go.AddComponent<SpriteRenderer>();
            requestBox.sprite = BeamSprite(); requestBox.color = beamColor;
            requestBox.sortingOrder = 42;
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
        if (window != null) window.enabled = any;
        if (!any) return;
        // 아이템창(아트 팀 item.png, 원래 색 + 반투명): 아이콘을 감싸는 틀
        var frame = UiArt.Get("item");
        if (frame != null)
        {
            if (window == null)
            {
                var wgo = new GameObject("ItemWindow");
                wgo.transform.SetParent(transform, false);
                window = wgo.AddComponent<SpriteRenderer>();
                window.sprite = frame; window.drawMode = SpriteDrawMode.Sliced; window.sortingOrder = 43;
                window.color = new Color(1f, 1f, 1f, windowAlpha);
            }
            const float k = 0.5f;                                  // 틀 두께 = 그림 40px × 0.5 / 100 = 0.2칸
            Vector3 wp = transform.lossyScale;
            window.transform.localScale = new Vector3(k / Mathf.Max(0.0001f, wp.x), k / Mathf.Max(0.0001f, wp.y), 1f);
            window.transform.position = new Vector3(b.center.x, b.center.y, 0f);
            window.size = new Vector2(b.size.x + 0.4f, b.size.y + 0.4f) / k;
            window.enabled = true;
        }
        // 빔: 단말기 윗부분에서 시작 (아래 중앙 기준) → 아이콘 위까지, 위쪽 폭 = 아이콘 폭 + 여유
        float bottom = body != null ? body.bounds.max.y - 0.35f : transform.position.y + 1.4f;
        float height = Mathf.Max(0.5f, b.max.y + beamMargin - bottom);
        float width = b.size.x + beamMargin * 2f;
        var t = requestBox.transform;
        t.position = new Vector3(transform.position.x, bottom, 0f);
        var size = requestBox.sprite.bounds.size;
        Vector3 p = t.parent != null ? t.parent.lossyScale : Vector3.one;
        t.localScale = new Vector3(width / size.x / Mathf.Max(0.0001f, p.x), height / size.y / Mathf.Max(0.0001f, p.y), 1f);
    }

    static Sprite beamSprite;

    /// <summary>V자 홀로그램 빔 (코드로 생성): 아래가 좁고 위로 퍼짐, 위로 갈수록 투명, 양옆 테두리는 조금 진하게, 가로 줄무늬</summary>
    static Sprite BeamSprite()
    {
        if (beamSprite != null) return beamSprite;
        const int W = 128, H = 256;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var px = new Color32[W * H];
        for (int y = 0; y < H; y++)
        {
            float v = y / (H - 1f);
            float half = Mathf.Lerp(0.12f, 0.5f, v) * W;          // 아래 폭 24% → 위 100%
            float fade = Mathf.Pow(1f - v, 1.3f);                  // 위로 갈수록 투명
            float scan = (y / 6) % 2 == 0 ? 1f : 0.82f;           // 홀로그램 줄무늬
            for (int x = 0; x < W; x++)
            {
                float d = Mathf.Abs(x + 0.5f - W * 0.5f);
                float a = 0f;
                if (d <= half)
                {
                    float e = d / Mathf.Max(1f, half);                 // 0 가운데 ~ 1 테두리
                    a = Mathf.Lerp(0.35f, 0.75f, e * e) * Mathf.Clamp01((half - d) / 2f + 0.5f);
                }
                px[y * W + x] = new Color32(255, 255, 255, (byte)(255 * Mathf.Clamp01(a * fade * scan)));
            }
        }
        tex.SetPixels32(px); tex.Apply();
        beamSprite = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0f), 128f);
        return beamSprite;
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
