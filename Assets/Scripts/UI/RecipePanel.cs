using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 조합표 (기획서 p.10 표): 재료 아이콘 + 재료 아이콘 = 결과 아이콘, 7종 + 오류뭉텅이.
/// 화면 오른쪽, 처음엔 펼침, Tab으로 접기/펴기. 지금 단말기가 요구하는 결과 줄은 강조.
/// </summary>
public class RecipePanel : MonoBehaviour
{
    public const float Width = 330f;
    static readonly Color Bg = new Color(0.05f, 0.08f, 0.15f, 0.94f);
    static readonly Color Accent = new Color(0.22f, 0.82f, 1f);
    static readonly Color Need = new Color(1f, 0.82f, 0.25f);

    struct Row { public ItemType a, b, result; public Image bg; public TMP_Text name; }

    readonly List<Row> rows = new List<Row>();
    RectTransform panel;
    GameObject body;
    TMP_Text header;
    bool open = true;
    StageManager stage;
    ItemIconSet icons;

    public static readonly (ItemType a, ItemType b, ItemType r)[] Recipes =
    {
        (ItemType.Planning, ItemType.Planning, ItemType.ContentDoc),
        (ItemType.Planning, ItemType.Programming, ItemType.SystemDoc),
        (ItemType.Programming, ItemType.Graphic, ItemType.CharacterCode),
        (ItemType.Programming, ItemType.QA, ItemType.CodeFix),
        (ItemType.Graphic, ItemType.QA, ItemType.GraphicFix),
        (ItemType.Programming, ItemType.Programming, ItemType.GameBuild),
        (ItemType.Graphic, ItemType.Graphic, ItemType.ArtFinal),
    };

    public void Build(Transform canvasRoot, ItemIconSet iconSet, StageManager sm)
    {
        icons = iconSet; stage = sm;
        panel = new GameObject("RecipePanel", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        panel.SetParent(canvasRoot, false);
        panel.anchorMin = new Vector2(1f, 0f); panel.anchorMax = new Vector2(1f, 1f);
        panel.pivot = new Vector2(1f, 0.5f);
        panel.offsetMin = new Vector2(-Width, 146f); panel.offsetMax = new Vector2(0f, -146f);   // 위·아래 HUD 띠 사이
        var bg = panel.GetComponent<Image>(); bg.color = Bg; bg.raycastTarget = false;
        var ol = panel.gameObject.AddComponent<Outline>(); ol.effectColor = Accent * 0.6f; ol.effectDistance = new Vector2(2f, -2f);

        header = StageHUD.Label(panel, "", new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(Width - 20f, 44f), 26f, TextAlignmentOptions.Center, Accent, true);

        body = new GameObject("Rows", typeof(RectTransform));
        body.transform.SetParent(panel, false);
        var br = (RectTransform)body.transform;
        br.anchorMin = Vector2.zero; br.anchorMax = Vector2.one; br.offsetMin = Vector2.zero; br.offsetMax = new Vector2(0f, -60f);

        const float rowH = 84f;
        for (int i = 0; i < Recipes.Length; i++) rows.Add(MakeRow(Recipes[i].a, Recipes[i].b, Recipes[i].r, -i * rowH));
        // 오류뭉텅이: 표에 없는 조합
        var err = MakeRow(ItemType.ErrorLump, ItemType.ErrorLump, ItemType.ErrorLump, -Recipes.Length * rowH);
        err.name.text = "<color=#FF7A7A>그 외 조합 = 오류뭉텅이</color>";
        rows.Add(err);
        Apply();
    }

    Row MakeRow(ItemType a, ItemType b, ItemType r, float y)
    {
        var row = new Row { a = a, b = b, result = r };
        var bgGo = new GameObject("Row", typeof(RectTransform), typeof(Image));
        bgGo.transform.SetParent(body.transform, false);
        var rt = (RectTransform)bgGo.transform;
        rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f); rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, y - 6f); rt.sizeDelta = new Vector2(-16f, 78f);
        row.bg = bgGo.GetComponent<Image>(); row.bg.raycastTarget = false; row.bg.color = new Color(1, 1, 1, 0.04f);

        bool isError = r == ItemType.ErrorLump;
        if (!isError)
        {
            Icon(rt, a, new Vector2(42f, -28f));
            Text(rt, "+", new Vector2(86f, -28f), 26f);
            Icon(rt, b, new Vector2(130f, -28f));
            Text(rt, "=", new Vector2(174f, -28f), 26f);
            Icon(rt, r, new Vector2(218f, -28f));
        }
        else Icon(rt, r, new Vector2(42f, -28f));
        row.name = StageHUD.Label(rt, ItemNames.Get(r), new Vector2(0f, 1f), new Vector2(isError ? 190f : 157f, -66f), new Vector2(290f, 22f), 17f, TextAlignmentOptions.Center, new Color(0.8f, 0.86f, 1f), false);
        return row;
    }

    void Icon(RectTransform parent, ItemType t, Vector2 pos)
    {
        var img = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        img.transform.SetParent(parent, false);
        StageHUD.Place(img.rectTransform, new Vector2(0f, 1f), pos, new Vector2(50f, 50f));
        img.sprite = icons != null ? icons.Get(t) : null; img.preserveAspect = true; img.raycastTarget = false;
    }

    void Text(RectTransform parent, string s, Vector2 pos, float size) =>
        StageHUD.Label(parent, s, new Vector2(0f, 1f), pos, new Vector2(30f, 40f), size, TextAlignmentOptions.Center, Color.white, true);

    void Update()
    {
        if (panel == null) return;
        if (Input.GetKeyDown(KeyCode.Tab)) Toggle();

        // 지금 단말기가 요구하는 결과 줄 강조
        var needed = new HashSet<ItemType>();
        if (stage != null && stage.terminals != null)
            foreach (var t in stage.terminals) if (t != null) foreach (var it in t.Remaining) needed.Add(it);
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 5f);
        foreach (var r in rows)
        {
            bool hot = r.result != ItemType.ErrorLump && needed.Contains(r.result);
            r.bg.color = hot ? new Color(Need.r, Need.g, Need.b, 0.18f + 0.12f * pulse) : new Color(1, 1, 1, 0.04f);
            r.name.color = hot ? Need : new Color(0.8f, 0.86f, 1f);
        }
    }

    // 맵 크기는 고정 (카메라는 처음부터 패널 자리를 비워 둠). 패널만 오른쪽 밖으로 0.2초 슬라이드.
    float slide;            // 0 = 펼침, 1 = 접힘
    TMP_Text tab;

    public void Toggle() { open = !open; Apply(); }

    void Apply()
    {
        header.text = "조합표 <size=18><color=#8FA3C7>(Tab 접기)</color></size>";
        if (tab == null)
        {
            var tabBg = new GameObject("RecipeTab", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            tabBg.transform.SetParent(panel.parent, false);
            var r = tabBg.rectTransform; r.anchorMin = r.anchorMax = new Vector2(1f, 1f); r.pivot = new Vector2(1f, 1f);
            r.anchoredPosition = new Vector2(0f, -150f); r.sizeDelta = new Vector2(150f, 56f);
            tabBg.color = Bg; tabBg.raycastTarget = false;
            tab = StageHUD.Label(r, "조합표 <size=18>(Tab)</size>", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(140f, 50f), 24f, TextAlignmentOptions.Center, Accent, true);
        }
        if (CameraFit.I != null) CameraFit.I.sidebar = Width + 10f;      // 항상 같은 자리 비워 둠
    }

    void LateUpdate()
    {
        if (panel == null) return;
        slide = Mathf.MoveTowards(slide, open ? 0f : 1f, Time.unscaledDeltaTime / 0.2f);
        float k = slide * slide * (3f - 2f * slide);                       // 부드럽게
        panel.anchoredPosition = new Vector2(k * (Width + 12f), panel.anchoredPosition.y);
        if (tab != null) tab.transform.parent.gameObject.SetActive(slide > 0.95f);
    }
}
