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

    SpriteRenderer requestBox;
    static Sprite whiteSprite;

    void OnEnable() => terminal.Changed += Refresh;
    void OnDisable() => terminal.Changed -= Refresh;
    void Start() => Refresh();

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
        if (whiteSprite == null)
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            whiteSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }
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
