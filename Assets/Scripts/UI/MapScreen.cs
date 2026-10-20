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
