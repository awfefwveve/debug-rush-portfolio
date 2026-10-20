using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 타격감·피드백: 피격(화면 흔들림·빨간 번쩍임), 패링(짧은 정지·링·팝업), 단말기 완료/시간 초과 팝업,
/// 제작 완료 반짝임, 재료 획득 팝업. 플레이어에 자동으로 붙음.
/// </summary>
public class StageFX : MonoBehaviour
{
    public static StageFX I { get; private set; }

    PlayerController player;
    Camera cam;
    Vector3 camBase;
    float shakeTime, shakeAmp;
    Image flash;
    static Sprite ring;
    readonly Dictionary<BugTerminal, int> completed = new Dictionary<BugTerminal, int>();
    int lastHp = int.MaxValue, lastItemCount;

    void Awake() { I = this; player = GetComponent<PlayerController>(); }

    IEnumerator Start()
    {
        yield return null;                                  // StageManager.Start 이후
        LabelBacking.ApplyAll();                            // 이름표·남은 시간 글자 뒤 검은 판
        cam = Camera.main;
        if (cam != null) camBase = cam.transform.position;
        BuildFlash();

        player.Health.HpChanged += OnHp;
        lastHp = player.Health.Hp;
        player.ParrySucceeded += OnParry;
        player.Inventory.Changed += OnInventory;
        lastItemCount = player.Inventory.items.Count;

        var sm = StageManager.I;
        if (sm != null && sm.terminals != null)
            foreach (var t in sm.terminals)
            {
                if (t == null) continue;
                var term = t;
                completed[term] = term.Completed;
                term.Changed += () => OnTerminal(term);
                term.Expired += () => Popup(term.transform.position + Vector3.up * 1.6f, "시간 초과!", new Color(1f, 0.35f, 0.35f));
            }
        foreach (var table in FindObjectsByType<CraftingTable>(FindObjectsSortMode.None))
        {
            var tb = table;
            tb.OutputChanged += item => { if (item.HasValue) OnCrafted(tb); };
        }
    }

    void OnDestroy() { if (I == this) I = null; }

    // ---------- 이벤트 ----------
    void OnHp(int hp, int max)
    {
        if (hp < lastHp)
        {
            Sfx.Hit();
            Shake(0.3f, 0.22f);
            Flash(new Color(1f, 0.1f, 0.15f, 0.38f));
            Popup(player.transform.position + Vector3.up * 1.1f, "-1", new Color(1f, 0.3f, 0.35f));
        }
        lastHp = hp;
    }

    void OnParry()
    {
        StartCoroutine(HitStop(0.09f));
        Shake(0.15f, 0.1f);
        Flash(new Color(0.8f, 0.6f, 1f, 0.25f));
        Ring(player.transform.position, new Color(0.75f, 0.5f, 1f), 2.4f);
        Popup(player.transform.position + Vector3.up * 1.3f, "패링!", new Color(0.8f, 0.6f, 1f), 1.3f);
    }

    void OnInventory()
    {
        var items = player.Inventory.items;
        if (items.Count > lastItemCount) Sfx.Pickup(); else if (items.Count < lastItemCount) Sfx.ItemOut();
        if (items.Count > lastItemCount && items.Count > 0)
            Popup(player.transform.position + Vector3.up * 1.0f, "+" + ItemNames.Get(items[items.Count - 1]), Color.white, 0.8f);
        lastItemCount = items.Count;
    }

    void OnTerminal(BugTerminal t)
    {
        int before = completed.TryGetValue(t, out var c) ? c : 0;
        if (t.Completed > before)
        {
            Popup(t.transform.position + Vector3.up * 1.7f, t.IsDone ? "수리 완료!" : "오류 수정 +1", new Color(1f, 0.85f, 0.25f), t.IsDone ? 1.4f : 1.1f);
            Ring(t.transform.position, new Color(1f, 0.85f, 0.25f), 3f);
            StartCoroutine(Punch(t.transform, 0.15f));
        }
        completed[t] = t.Completed;
    }

    void OnCrafted(CraftingTable table)
    {
        Sfx.Craft();
        var view = table.GetComponent<CraftingTableView>();
        var target = view != null && view.outputIcon != null ? view.outputIcon.transform : table.transform;
        StartCoroutine(Punch(target, 0.35f));
        Ring(target.position, new Color(1f, 0.75f, 0.3f), 1.8f);
        Popup(target.position + Vector3.up * 0.6f, "완성!", new Color(1f, 0.8f, 0.35f));
    }

    // ---------- 효과 ----------
    public bool Shaking => shakeTime > 0f;

    public void Shake(float time, float amp) { shakeTime = Mathf.Max(shakeTime, time); shakeAmp = Mathf.Max(shakeAmp, amp); }

    void LateUpdate()
    {
        if (cam == null) return;
        if (shakeTime > 0f)
        {
            shakeTime -= Time.unscaledDeltaTime;
            var o = Random.insideUnitCircle * shakeAmp;
            Vector3 b = CameraFit.I != null ? CameraFit.Base : camBase;
            cam.transform.position = b + new Vector3(o.x, o.y, 0f);
            if (shakeTime <= 0f) { shakeAmp = 0f; cam.transform.position = b; }
        }
        if (flash != null && flash.color.a > 0f)
        {
            var c = flash.color; c.a = Mathf.Max(0f, c.a - Time.unscaledDeltaTime * 1.6f); flash.color = c;
        }
    }

    // 패링 순간 정지. 연속 패링이 겹쳐도 원래 속도로 반드시 복구 (겹칠 때 0.05로 고정되던 버그 수정)
    float hitStopUntil;
    bool hitStopping;
    const float HitStopScale = 0.05f;

    IEnumerator HitStop(float seconds)
    {
        if (Time.timeScale <= 0f) yield break;          // 일시정지 중엔 무시
        hitStopUntil = Mathf.Max(hitStopUntil, Time.unscaledTime + seconds);
        if (hitStopping) yield break;                    // 이미 정지 중이면 시간만 연장
        hitStopping = true;
        Time.timeScale = HitStopScale;
        while (Time.unscaledTime < hitStopUntil) yield return null;
        if (Mathf.Approximately(Time.timeScale, HitStopScale)) Time.timeScale = 1f;
        hitStopping = false;
    }

    void OnDisable()
    {
        if (hitStopping && Mathf.Approximately(Time.timeScale, HitStopScale)) Time.timeScale = 1f;
        hitStopping = false;
    }

    IEnumerator Punch(Transform t, float amount)
    {
        if (t == null) yield break;
        Vector3 baseScale = t.localScale;
        float d = 0.25f, e = 0f;
        while (e < d && t != null)
        {
            e += Time.deltaTime;
            float k = 1f + Mathf.Sin(e / d * Mathf.PI) * amount;
            t.localScale = baseScale * k;
            yield return null;
        }
        if (t != null) t.localScale = baseScale;
    }

    void Flash(Color c)
    {
        if (flash == null) return;
        flash.color = c;
    }

    void BuildFlash()
    {
        var canvasGo = new GameObject("FX_Flash", typeof(Canvas), typeof(CanvasScaler));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var img = new GameObject("Flash", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        img.transform.SetParent(canvasGo.transform, false);
        var r = img.rectTransform; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
        img.raycastTarget = false;
        img.color = new Color(1, 1, 1, 0);
        flash = img;
    }

    public void Popup(Vector3 pos, string text, Color color, float scale = 1f)
    {
        var go = new GameObject("Popup");
        go.transform.position = pos;
        var t = go.AddComponent<TextMeshPro>();
        t.font = KoreanTMPFont.Get();
        t.text = text;
        t.fontSize = 4.2f * scale;
        t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center;
        t.color = color;
        t.outlineWidth = 0.25f;
        t.outlineColor = new Color32(10, 14, 28, 255);
        t.rectTransform.sizeDelta = new Vector2(6f, 1.2f);
        t.GetComponent<MeshRenderer>().sortingOrder = 60;
        StartCoroutine(FloatUp(t));
    }

    IEnumerator FloatUp(TextMeshPro t)
    {
        Vector3 start = t.transform.position;
        float d = 0.9f, e = 0f;
        Color c = t.color;
        while (e < d && t != null)
        {
            e += Time.unscaledDeltaTime;
            float k = e / d;
            t.transform.position = start + Vector3.up * (0.9f * Mathf.Sqrt(k));
            t.transform.localScale = Vector3.one * (k < 0.15f ? Mathf.Lerp(0.6f, 1.1f, k / 0.15f) : Mathf.Lerp(1.1f, 1f, (k - 0.15f) / 0.85f));
            t.color = new Color(c.r, c.g, c.b, k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f);
            yield return null;
        }
        if (t != null) Destroy(t.gameObject);
    }

    public void Ring(Vector3 pos, Color color, float maxSize)
    {
        if (ring == null) ring = MakeRing();
        var go = new GameObject("Ring");
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = ring; sr.color = color; sr.sortingOrder = 55;
        StartCoroutine(Expand(sr, maxSize));
    }

    IEnumerator Expand(SpriteRenderer sr, float maxSize)
    {
        float d = 0.35f, e = 0f; Color c = sr.color;
        while (e < d && sr != null)
        {
            e += Time.unscaledDeltaTime;
            float k = e / d;
            sr.transform.localScale = Vector3.one * Mathf.Lerp(0.3f, maxSize, 1f - (1f - k) * (1f - k));
            sr.color = new Color(c.r, c.g, c.b, 1f - k);
            yield return null;
        }
        if (sr != null) Destroy(sr.gameObject);
    }

    static Sprite MakeRing()
    {
        const int n = 128;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                float a = Mathf.Clamp01(1f - Mathf.Abs(d - 0.85f) / 0.12f);
                tex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
    }
}
