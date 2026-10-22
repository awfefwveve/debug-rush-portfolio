using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>맵 화면: 챕터(1~3학년) 드롭다운 + 스테이지 카드 목록</summary>
public class MapScreen : MonoBehaviour
{
    public StageCard cardPrefab;
    public Transform content;
    public TMP_Dropdown chapterDropdown;
    public Button backButton;
    void Start()
    {
        chapterDropdown.onValueChanged.AddListener(i => Build(i + 1));
        backButton.onClick.AddListener(() => SceneManager.LoadScene("Title"));
        EnsureScrollbar();
        StyleArt();
        Build(chapterDropdown != null ? chapterDropdown.value + 1 : 1);
    }

    /// <summary>기획서 p.17: 카드 가로 스크롤 + 하단 스크롤바</summary>
    void EnsureScrollbar()
    {
        var scroll = content != null ? content.GetComponentInParent<ScrollRect>() : null;
        if (scroll == null) return;
        scroll.horizontal = true;
        scroll.vertical = false;
        scroll.scrollSensitivity = Mathf.Max(scroll.scrollSensitivity, 30f);
        if (scroll.horizontalScrollbar != null) return;

        var go = DefaultControls.CreateScrollbar(new DefaultControls.Resources());
        go.name = "HorizontalScrollbar";
        go.transform.SetParent(scroll.transform, false);
        var r = (RectTransform)go.transform;
        r.anchorMin = new Vector2(0f, 0f);
        r.anchorMax = new Vector2(1f, 0f);
        r.pivot = new Vector2(0.5f, 1f);
        r.anchoredPosition = new Vector2(0f, -8f);
        r.sizeDelta = new Vector2(0f, 20f);
        var bar = go.GetComponent<Scrollbar>();
        bar.direction = Scrollbar.Direction.LeftToRight;
        scroll.horizontalScrollbar = bar;
        scroll.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        var bg = go.GetComponent<Image>(); if (bg != null) bg.color = new Color(0.09f, 0.13f, 0.24f);
        if (bar.handleRect != null) { var h = bar.handleRect.GetComponent<Image>(); if (h != null) h.color = new Color(0.22f, 0.82f, 1f); }
    }

    /// <summary>아트 팀 그림: 학년 선택 바(class / 펼친 목록 class_2), 스크롤바(back_scroll 바탕 · line_scroll 테두리 · 손잡이), 돌아가기 버튼 틀</summary>
    void StyleArt()
    {
        if (backButton != null) UiArt.StyleButton(backButton, 28f);
        var cls = UiArt.Get("class");
        if (chapterDropdown != null && cls != null)
        {
            chapterDropdown.image.sprite = cls; chapterDropdown.image.type = Image.Type.Sliced; chapterDropdown.image.color = Color.white;
            if (chapterDropdown.captionText != null) { chapterDropdown.captionText.color = UiArt.Ink; chapterDropdown.captionText.fontStyle = TMPro.FontStyles.Bold; }
            if (chapterDropdown.template != null)
                foreach (var img in chapterDropdown.template.GetComponentsInChildren<Image>(true))
                {
                    if (img.GetComponent<Scrollbar>() != null || img.transform.parent.GetComponent<Scrollbar>() != null) continue;
                    if (img.GetComponent<Mask>() != null) continue;
                    if (img.transform == chapterDropdown.template || img.name == "Item Background") { img.sprite = UiArt.Get("class_2"); img.type = Image.Type.Sliced; img.color = Color.white; }
                }
            if (chapterDropdown.itemText != null) chapterDropdown.itemText.color = UiArt.Ink;
        }
        var scroll = content != null ? content.GetComponentInParent<ScrollRect>() : null;
        var bar = scroll != null ? scroll.horizontalScrollbar : null;
        if (bar != null && UiArt.Get("back_scroll") != null)
        {
            var r = (RectTransform)bar.transform; r.sizeDelta = new Vector2(r.sizeDelta.x, 30f);
            var bg = bar.GetComponent<Image>(); if (bg != null) { bg.sprite = UiArt.Get("back_scroll"); bg.type = Image.Type.Sliced; bg.color = Color.white; }
            if (bar.handleRect != null)
            {
                var h = bar.handleRect.GetComponent<Image>();
                if (h != null) { h.sprite = UiArt.Get("scroll_handle"); h.type = Image.Type.Sliced; h.color = Color.white; }
                var area = bar.handleRect.parent as RectTransform;
                if (area != null) { area.offsetMin = new Vector2(6f, 5f); area.offsetMax = new Vector2(-6f, -5f); }
            }
            var line = UiArt.Get("line_scroll");
            if (line != null && bar.transform.Find("Frame") == null)
            {
                var f = new GameObject("Frame", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                f.transform.SetParent(bar.transform, false);
                var fr = f.rectTransform; fr.anchorMin = Vector2.zero; fr.anchorMax = Vector2.one; fr.offsetMin = fr.offsetMax = Vector2.zero;
                f.sprite = line; f.type = Image.Type.Sliced; f.raycastTarget = false;
            }
        }
    }

    void SetupContent()
    {
        var r = (RectTransform)content;
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0f, 0.5f);
        r.anchoredPosition = Vector2.zero;
        var h = content.GetComponent<HorizontalLayoutGroup>();
        if (h != null)
        {
            h.spacing = 36f; h.padding = new RectOffset(40, 40, 0, 0);
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;
        }
        var bgImg = content.GetComponentInParent<ScrollRect>()?.GetComponent<Image>();
        if (bgImg != null) bgImg.color = new Color(0.05f, 0.08f, 0.15f, 0.9f);
    }

    System.Collections.IEnumerator ScrollToStart()
    {
        yield return null;
        var scroll = content.GetComponentInParent<ScrollRect>();
        if (scroll != null) scroll.horizontalNormalizedPosition = 0f;
    }

    void Build(int chapter)
    {
        SetupContent();
        StartCoroutine(ScrollToStart());
        foreach (Transform c in content) Destroy(c.gameObject);
        var stages = GameManager.I.allStages.Where(s => s != null && s.chapter == chapter).ToArray();
        int last = Mathf.Max(StageManager.StagesPerChapter, stages.Length > 0 ? stages.Max(s => s.stage) : 0);
        // 챕터당 5칸을 모두 보여주고, 등록 안 된 스테이지는 "준비 중"으로 잠금
        for (int n = 1; n <= last; n++)
        {
            var s = stages.FirstOrDefault(x => x.stage == n);
            var card = Instantiate(cardPrefab, content);
            if (s != null) card.Setup(s);
            else card.SetupPlaceholder(chapter, n);
        }
    }
}
