using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 업적 화면 (기획서 p.15): 좌측 4x4 아이콘 그리드 + 우측 상세 + 돌아가기.
/// 획득 = 밝은 아이콘, 미획득 = 어두운 아이콘, 비밀(미획득) = 검정 아이콘 + 설명 숨김.
/// </summary>
public class AchievementScreen : MonoBehaviour
{
    public AchievementData[] data;
    public TMP_Text listText, detailText;

    [Header("그리드")]
    public int columns = 4, rows = 4;
    public float spacing = 12f;
    public Color frameColor = new Color(0.15f, 0.15f, 0.2f, 0.9f);
    public Color selectedColor = new Color(1f, 0.85f, 0.2f, 1f);
    public Color lockedTint = new Color(0.3f, 0.3f, 0.3f, 1f);

    int selected;
    Image[] frames, iconImages;
    Image detailIcon;

    public void BackToTitle() => SceneManager.LoadScene("Title");

    void Start()
    {
        StyleBack();
        BuildGrid();
        BuildDetailIcon();
        if (data != null && data.Length > 0) Show(0); else Refresh();
    }

    public void Show(int index)
    {
        if (data == null || index < 0 || index >= data.Length) return;
        selected = index;
        var a = data[index];
        bool unlocked = AchievementManager.IsUnlocked(a.id);
        if (detailText) detailText.text = a.title + "\n\n" + AchievementManager.DescriptionFor(a) + "\n\n" + (unlocked ? "획득함" : "미획득");
        if (detailIcon)
        {
            detailIcon.sprite = IconSprite(a, unlocked);
            detailIcon.color = IconTint(a, unlocked);
            detailIcon.enabled = true;
        }
        Refresh();
        ScrollTo(index);
    }

    void Update()
    {
        if (data == null || data.Length == 0) return;
        int n = data.Length;
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) Show((selected + 1) % n);
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) Show((selected - 1 + n) % n);
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) Show(Mathf.Min(n - 1, selected + columns));
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) Show(Mathf.Max(0, selected - columns));
        if (Input.GetKeyDown(KeyCode.Escape)) BackToTitle();
    }

    Color IconColor(AchievementData a, bool unlocked)
    {
        if (unlocked) return Color.white;
        return a.secret ? Color.black : lockedTint;
    }

    // 미획득은 자물쇠 그림 (아트 팀 lock.png), 없으면 어둡게 칠한 아이콘
    Sprite IconSprite(AchievementData a, bool unlocked) => !unlocked && UiArt.Get("lock") != null ? UiArt.Get("lock") : a.icon;
    Color IconTint(AchievementData a, bool unlocked) => !unlocked && UiArt.Get("lock") != null ? Color.white : IconColor(a, unlocked);

    void Refresh()
    {
        if (frames == null) return;
        for (int i = 0; i < frames.Length; i++)
        {
            bool has = data != null && i < data.Length && data[i] != null;
            // 칸 틀은 모두 item_2 (item.png는 바깥 장식 때문에 칸보다 커 보여 어긋남) → 선택은 밝은 색으로
            if (UiArt.Get("item_2") != null) { frames[i].sprite = UiArt.Get("item_2"); frames[i].type = Image.Type.Sliced; frames[i].color = has && i == selected ? new Color(1f, 0.9f, 0.55f) : (has ? Color.white : new Color(0.6f, 0.6f, 0.65f)); }
            else frames[i].color = has && i == selected ? selectedColor : frameColor;
            iconImages[i].enabled = has;
            if (!has) continue;
            bool got = AchievementManager.IsUnlocked(data[i].id);
            iconImages[i].sprite = IconSprite(data[i], got);
            iconImages[i].color = IconTint(data[i], got);
        }
    }

    ScrollRect scroll;
    float cellStep;

    /// <summary>업적 칸: 4열 그리드를 세로 스크롤 (아트 팀 스크롤바: 바탕 back_scroll · 테두리 line_scroll · 손잡이)</summary>
    void BuildGrid()
    {
        if (listText == null) return;
        var area = listText.rectTransform;
        listText.text = "";
        foreach (Transform child in area) child.gameObject.SetActive(false);   // 예전 목록 항목 숨김
        if (area.parent != null)
            foreach (Transform sibling in area.parent)
                if (sibling.name.StartsWith("Achievement_")) sibling.gameObject.SetActive(false);

        const float barW = 26f, barGap = 16f;
        int count = Mathf.Max(columns * (rows + 1), data != null ? data.Length : 0);   // 한 줄 더 → 스크롤
        count = Mathf.CeilToInt(count / (float)columns) * columns;
        float cell = Mathf.Min((area.rect.width - barW - barGap - spacing * (columns - 1)) / columns,
                               (area.rect.height - spacing * (rows - 1)) / rows);
        cellStep = cell + spacing;

        // 뷰포트(가림) + 내용
        var view = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
        view.SetParent(area, false);
        view.anchorMin = Vector2.zero; view.anchorMax = Vector2.one; view.offsetMin = Vector2.zero; view.offsetMax = new Vector2(-(barW + barGap), 0f);
        var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
        content.SetParent(view, false);
        content.anchorMin = new Vector2(0f, 1f); content.anchorMax = new Vector2(1f, 1f); content.pivot = new Vector2(0.5f, 1f);
        int rowCount = count / columns;
        content.sizeDelta = new Vector2(0f, rowCount * cellStep - spacing);

        scroll = area.gameObject.GetComponent<ScrollRect>(); if (scroll == null) scroll = area.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = view; scroll.content = content; scroll.horizontal = false; scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 40f;

        // 세로 스크롤바
        var barGo = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        var bar = barGo.GetComponent<RectTransform>();
        bar.SetParent(area, false);
        bar.anchorMin = new Vector2(1f, 0f); bar.anchorMax = new Vector2(1f, 1f); bar.pivot = new Vector2(1f, 0.5f);
        bar.sizeDelta = new Vector2(barW, 0f); bar.anchoredPosition = Vector2.zero;
        var barImg = barGo.GetComponent<Image>();
        barImg.sprite = UiArt.Get("back_scroll"); barImg.type = Image.Type.Sliced; barImg.color = barImg.sprite != null ? Color.white : new Color(0.1f, 0.1f, 0.14f);
        var slide = new GameObject("Sliding Area", typeof(RectTransform)).GetComponent<RectTransform>();
        slide.SetParent(bar, false); slide.anchorMin = Vector2.zero; slide.anchorMax = Vector2.one; slide.offsetMin = new Vector2(4f, 4f); slide.offsetMax = new Vector2(-4f, -4f);
        var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        handle.transform.SetParent(slide, false);
        handle.rectTransform.offsetMin = handle.rectTransform.offsetMax = Vector2.zero;
        handle.sprite = UiArt.Get("scroll_handle"); handle.type = Image.Type.Sliced; handle.color = handle.sprite != null ? Color.white : new Color(0.85f, 0.85f, 0.9f);
        var frame = new GameObject("Frame", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        frame.transform.SetParent(bar, false);
        frame.rectTransform.anchorMin = Vector2.zero; frame.rectTransform.anchorMax = Vector2.one; frame.rectTransform.offsetMin = frame.rectTransform.offsetMax = Vector2.zero;
        frame.sprite = UiArt.Get("line_scroll"); frame.type = Image.Type.Sliced; frame.raycastTarget = false; frame.enabled = frame.sprite != null;
        var sb = barGo.GetComponent<Scrollbar>();
        sb.handleRect = handle.rectTransform; sb.targetGraphic = handle; sb.direction = Scrollbar.Direction.BottomToTop;
        scroll.verticalScrollbar = sb; scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

        frames = new Image[count];
        iconImages = new Image[count];
        for (int i = 0; i < count; i++)
        {
            int col = i % columns, row = i / columns;
            var frameGo = new GameObject("Cell" + i, typeof(RectTransform), typeof(Image), typeof(Button));
            frameGo.transform.SetParent(content, false);
            var r = frameGo.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0f, 1f);
            r.sizeDelta = new Vector2(cell, cell);
            r.anchoredPosition = new Vector2(col * cellStep, -row * cellStep);
            frames[i] = frameGo.GetComponent<Image>();
            int idx = i;
            frameGo.GetComponent<Button>().onClick.AddListener(() => Show(idx));

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(frameGo.transform, false);
            var ir = iconGo.GetComponent<RectTransform>();
            ir.anchorMin = Vector2.zero; ir.anchorMax = Vector2.one;
            ir.offsetMin = new Vector2(18f, 18f); ir.offsetMax = new Vector2(-18f, -18f);
            iconImages[i] = iconGo.GetComponent<Image>();
            iconImages[i].preserveAspect = true;
            iconImages[i].raycastTarget = false;
        }
    }

    /// <summary>키보드로 고른 칸이 보이게 스크롤</summary>
    void ScrollTo(int index)
    {
        if (scroll == null || scroll.content == null || scroll.viewport == null) return;
        float viewH = scroll.viewport.rect.height, top = (index / columns) * cellStep, y = scroll.content.anchoredPosition.y;
        if (top < y) y = top;
        else if (top + cellStep > y + viewH) y = top + cellStep - viewH;
        scroll.content.anchoredPosition = new Vector2(0f, Mathf.Clamp(y, 0f, Mathf.Max(0f, scroll.content.rect.height - viewH)));
    }

    // 시안: 왼쪽 위 "◀ 돌아가기" (back_click 삼각형), 상세 칸 오른쪽 아래 마스코트(icon.png)
    void StyleBack()
    {
        var back = GameObject.Find("Back");
        var arrow = UiArt.Get("back_click");
        if (back != null && arrow != null && back.TryGetComponent(out Button b))
        {
            if (b.image != null) b.image.color = new Color(1f, 1f, 1f, 0f);
            var icon = new GameObject("Arrow", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            icon.transform.SetParent(back.transform, false);
            var r = icon.rectTransform; r.anchorMin = r.anchorMax = new Vector2(0f, 0.5f);
            r.anchoredPosition = new Vector2(30f, 0f); r.sizeDelta = new Vector2(34f, 34f); r.localRotation = Quaternion.Euler(0f, 0f, 90f);
            icon.sprite = arrow; icon.raycastTarget = false; icon.color = new Color(0.8f, 0.82f, 0.88f);
            var label = back.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = "돌아가기"; label.color = new Color(0.8f, 0.82f, 0.88f); label.fontStyle = FontStyles.Bold; label.enableWordWrapping = false;
                label.alignment = TextAlignmentOptions.MidlineLeft;
                var lr = label.rectTransform; lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one; lr.offsetMin = new Vector2(60f, 0f); lr.offsetMax = Vector2.zero;
            }
        }
        if (detailText != null && detailText.transform.parent != null)
        {
            // 설명(아이콘·이름·설명·획득) 뒤 반투명 창 (설정창 틀 계열 어두운 색)
            var dt = detailText.rectTransform;
            var panel = new GameObject("DetailBack", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            panel.transform.SetParent(dt.parent, false);
            panel.transform.SetSiblingIndex(dt.GetSiblingIndex());
            var br = panel.rectTransform;
            br.anchorMin = dt.anchorMin; br.anchorMax = dt.anchorMax; br.pivot = dt.pivot;
            br.anchoredPosition = dt.anchoredPosition; br.sizeDelta = dt.sizeDelta + new Vector2(60f, 60f);
            panel.color = new Color(0.16f, 0.17f, 0.24f, 0.6f); panel.raycastTarget = false;
        }
        var mascot = UiArt.Get("icon");
        if (mascot != null && detailText != null)
        {
            var m = new GameObject("Mascot", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            m.transform.SetParent(detailText.rectTransform, false);
            var r = m.rectTransform; r.anchorMin = r.anchorMax = r.pivot = new Vector2(1f, 0f);
            r.anchoredPosition = new Vector2(10f, -10f); r.sizeDelta = new Vector2(150f, 150f);
            m.sprite = mascot; m.preserveAspect = true; m.raycastTarget = false; m.color = new Color(1f, 1f, 1f, 0.85f);
        }
    }

    void BuildDetailIcon()
    {
        if (detailText == null) return;
        var go = new GameObject("DetailIcon", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(detailText.rectTransform, false);
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f);
        r.pivot = new Vector2(0.5f, 1f);
        r.sizeDelta = new Vector2(130f, 130f);
        r.anchoredPosition = new Vector2(0f, -10f);
        detailIcon = go.GetComponent<Image>();
        detailIcon.preserveAspect = true;
        detailIcon.raycastTarget = false;
        detailIcon.enabled = false;
        detailText.margin = new Vector4(detailText.margin.x, 150f, detailText.margin.z, detailText.margin.w);
    }
}
