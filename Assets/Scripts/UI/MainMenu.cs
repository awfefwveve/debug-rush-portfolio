using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 시작화면 (기획서 p.16 + 아트 팀 시안): 전체 일러스트(start_image) + 로고, 우하단 시작 / 설정 / 나가기,
/// 그 아래 마름모 아이콘 업적 / 리셋(진행도 초기화) / 크레딧.
/// </summary>
public class MainMenu : MonoBehaviour
{
    public GameObject settingsPanel;
    [Header("일러스트 (아트 팀 그림이 없을 때만 사용)")]
    public Sprite heroSprite;
    public Sprite monsterSprite;
    [Tooltip("타이틀 로고 1024×512 (팀 규격, 사방 32px 여백)")] public Sprite logoSprite;

    RectTransform monster;
    bool showCredits;

    static readonly string[][] Credits =
    {
        new[] { "기획 · 레벨 · 사운드", "조은희" },
        new[] { "메인 프로그래밍", "김보정" },
        new[] { "보조 프로그래밍", "양예슬비" },
        new[] { "그래픽", "김태은" },
        new[] { "그래픽", "강수린" },
    };

    void Awake() => SoundEffectManager.EnsureInstance();

    void Start()
    {
        if (settingsPanel != null)
        {
            // 설정창은 SettingsWindow가 그림 → 예전 패널은 화면을 어둡게 하는 배경으로만 사용
            foreach (Transform c in settingsPanel.transform) c.gameObject.SetActive(false);
            var r = (RectTransform)settingsPanel.transform;
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
            var img = settingsPanel.GetComponent<Image>();
            if (img != null) { img.color = new Color(0f, 0f, 0f, 0f); img.raycastTarget = true; }   // 어둡게는 SettingsWindow.Dim, 이 판은 뒤 버튼 클릭만 막음
        }
        BuildIllustration();
        BuildButtons();
        if (settingsPanel != null) settingsPanel.transform.SetAsLastSibling();
    }

    void BuildIllustration()
    {
        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;
        var root = canvas.transform;
        var bg = root.Find("Background");
        var title = root.Find("Title")?.GetComponent<TMP_Text>();
        var subtitle = root.Find("Subtitle")?.GetComponent<TMP_Text>();

        var art = UiArt.Get("start_image");
        if (art != null && bg != null && bg.TryGetComponent(out Image bgImg))
        {
            // 아트 팀 시작 일러스트 1920×1080: 그림 전체가 다 보이게 (비율 유지, 화면 비율이 다르면 남는 곳은 검정)
            var r = bgImg.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f); r.anchoredPosition = Vector2.zero; r.sizeDelta = new Vector2(1920f, 1080f);
            bgImg.sprite = art; bgImg.color = Color.white; bgImg.type = Image.Type.Simple; bgImg.preserveAspect = false;
            var fit = bg.GetComponent<AspectRatioFitter>(); if (fit == null) fit = bg.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent; fit.aspectRatio = 1920f / 1080f;
            if (Camera.main != null) Camera.main.backgroundColor = Color.black;   // 남는 곳 검정
        }
        else
        {
            int index = bg != null ? bg.GetSiblingIndex() + 1 : 0;
            var glow = Img(root, "Glow", null, new Vector2(0f, -470f), new Vector2(1920f, 6f), new Color(0.22f, 0.82f, 1f, 0.5f));
            glow.transform.SetSiblingIndex(index);
            if (monsterSprite != null)
            {
                var m = Img(root, "IllustMonster", monsterSprite, new Vector2(300f, -60f), new Vector2(440f, 440f), Color.white);
                m.transform.SetSiblingIndex(index + 1);
                monster = m.rectTransform;
            }
            if (heroSprite != null)
            {
                var h = Img(root, "IllustHero", heroSprite, new Vector2(-560f, -260f), new Vector2(600f, 600f), Color.white);
                h.transform.SetSiblingIndex(index + 2);
            }
        }

        var logo = UiArt.Get("title"); if (logo == null) logo = logoSprite;
        if (logo != null)
        {
            // 시안: 왼쪽 위 로고
            Img(root, "Logo", logo, new Vector2(-610f, 300f), new Vector2(760f, 380f), Color.white);
            if (title != null) title.gameObject.SetActive(false);
            if (subtitle != null) subtitle.gameObject.SetActive(false);
        }
        else if (title != null) { title.color = new Color(0.22f, 0.82f, 1f); title.fontStyle = FontStyles.Bold; title.outlineWidth = 0.15f; title.outlineColor = new Color32(255, 79, 163, 255); }
    }

    /// <summary>시안 배치: 오른쪽 아래 버튼 3개 세로, 그 아래 마름모 3개 가로</summary>
    void BuildButtons()
    {
        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;
        var root = canvas.transform;
        string[] names = { "Start", "Setting", "Quit" };
        for (int i = 0; i < names.Length; i++)
        {
            var t = root.Find(names[i]);
            if (t == null || !t.TryGetComponent(out Button b)) continue;
            var r = (RectTransform)t;
            // 화면 오른쪽 아래 기준 (16:10 등 비율이 달라도 오른쪽에 붙음). 버튼 80 높이, 간격 10, 아래 다이아 묶음과 60 여백
            r.anchorMin = r.anchorMax = new Vector2(1f, 0f); r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = new Vector2(-270f, 480f - i * 90f); r.sizeDelta = new Vector2(440f, 80f);
            UiArt.StyleButton(b, 34f);
        }

        // 업적 버튼(씬) → 마름모. 리셋·크레딧은 새로 만듦
        var ach = root.Find("Achievements");
        if (ach != null && ach.TryGetComponent(out Button achBtn)) Diamond(achBtn, "achievements", "업적", 0);
        Diamond(NewButton(root, "Reset", () => { if (!showCredits) confirmReset = true; }), "reset", "리셋", 1);
        Diamond(NewButton(root, "Credit", () => { if (!confirmReset) showCredits = true; }), "credit", "크레딧", 2);
    }

    void Diamond(Button b, string art, string label, int slot)
    {
        if (b == null) return;
        var r = (RectTransform)b.transform;
        r.anchorMin = r.anchorMax = new Vector2(1f, 0f); r.pivot = new Vector2(0.5f, 0.5f);   // 오른쪽 아래 기준: 위 칸 아래끝 200 · 아래 글자 끝 46
        r.anchoredPosition = new Vector2(-410f + slot * 140f, 140f); r.sizeDelta = new Vector2(120f, 120f);
        var sprite = UiArt.Get(art);
        if (b.image != null && sprite != null) { b.image.sprite = sprite; b.image.type = Image.Type.Simple; b.image.preserveAspect = true; b.image.color = Color.white; }
        var colors = b.colors; colors.normalColor = Color.white; colors.highlightedColor = new Color(1f, 0.75f, 0.85f); colors.pressedColor = new Color(0.8f, 0.55f, 0.65f); colors.selectedColor = Color.white;
        b.colors = colors; b.transition = Selectable.Transition.ColorTint;
        var text = b.GetComponentInChildren<TMP_Text>(true);
        if (text == null)
        {
            text = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            text.transform.SetParent(b.transform, false);
        }
        var tr = text.rectTransform;
        tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 0f); tr.pivot = new Vector2(0.5f, 1f);
        tr.anchoredPosition = new Vector2(0f, -2f); tr.sizeDelta = new Vector2(140f, 34f);
        text.font = KoreanTMPFont.Get(); text.text = label; text.fontSize = 24f; text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center; text.color = UiArt.Ink; text.raycastTarget = false;
        text.outlineWidth = 0.2f; text.outlineColor = new Color32(20, 10, 20, 255);
    }

    static Button NewButton(Transform parent, string name, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var b = go.GetComponent<Button>();
        b.onClick.AddListener(onClick);
        return b;
    }

    void Update()
    {
        if (monster != null) monster.anchoredPosition = new Vector2(300f, -60f + Mathf.Sin(Time.time * 2.2f) * 18f);
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (showCredits) showCredits = false;
            else if (settingsPanel != null && settingsPanel.activeSelf) settingsPanel.SetActive(false);
        }
    }

    static Image Img(Transform parent, string name, Sprite sprite, Vector2 pos, Vector2 size, Color color)
    {
        var img = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        img.transform.SetParent(parent, false);
        var r = img.rectTransform; r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos; r.sizeDelta = size;
        img.sprite = sprite; img.color = color; img.preserveAspect = true; img.raycastTarget = false;
        return img;
    }

    bool Busy => confirmReset || showCredits;
    public void OnStart() { if (!Busy) SceneManager.LoadScene("Map"); }
    public void OnAchievements() { if (!Busy) SceneManager.LoadScene("Achievements"); }
    public void OnSettings() { if (settingsPanel && !Busy) settingsPanel.SetActive(!settingsPanel.activeSelf); }
    public void OnQuit()
    {
        if (Busy) return;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    // 진행도 초기화 (시연용: 여러 사람이 연달아 처음부터 플레이)
    bool confirmReset;
    float resetDoneUntil;

    void OnGUI()
    {
        KoreanIMGUIFont.Apply();
        if (settingsPanel && settingsPanel.activeSelf)
        {
            if (SettingsWindow.Draw()) settingsPanel.SetActive(false);
            return;
        }

        float s = Screen.height / 1080f;
        if (Time.unscaledTime < resetDoneUntil)
            UiArt.Label(new Rect(Screen.width - 760f * s, Screen.height - 60f * s, 520f * s, 40f * s), "초기화했어요. 처음부터 플레이할 수 있어요.", 22f * s, new Color(0.4f, 0.95f, 0.6f), TextAnchor.MiddleRight, true);

        if (showCredits) { DrawCredits(s); return; }
        if (!confirmReset) return;
        SettingsWindow.Dim();
        var r = UiArt.Window("리셋", 760f, 450f);
        UiArt.Label(new Rect(r.x + r.width * 0.08f, r.y + r.height * 0.25f, r.width * 0.84f, r.height * 0.32f),
            "진행도를 모두 초기화할까요?\n<size=" + Mathf.RoundToInt(20 * s) + ">스테이지 클리어·업적이 지워져요 (설정은 유지)</size>", 30f * s, new Color(0.12f, 0.13f, 0.18f), TextAnchor.MiddleCenter, true);
        float bw = r.width * 0.3f, bh = bw * 0.24f, by = r.y + r.height * 0.68f;
        if (UiArt.Button(new Rect(r.center.x - bw - 12f * s, by, bw, bh), "초기화", 26f * s)) { GameSettings.ResetProgress(); confirmReset = false; resetDoneUntil = Time.unscaledTime + 3f; }
        if (UiArt.Button(new Rect(r.center.x + 12f * s, by, bw, bh), "취소", 26f * s)) confirmReset = false;
    }

    void DrawCredits(float s)
    {
        SettingsWindow.Dim();
        var r = UiArt.Window("크레딧", 1100f, 650f);
        var ink = new Color(0.12f, 0.13f, 0.18f);
        UiArt.Label(new Rect(r.x, r.y + r.height * 0.16f, r.width, r.height * 0.1f), "팀 팥없찐", 38f * s, ink, TextAnchor.MiddleCenter, true);
        float nameW = r.width * 0.19f, nameH = nameW * 80f / 224f, rowH = nameH * 1.1f;
        float x = r.x + r.width * 0.22f, y = r.y + r.height * 0.29f;
        for (int i = 0; i < Credits.Length; i++)
        {
            var tag = new Rect(x, y + i * rowH, nameW, nameH);
            if (UiArt.Tex("name") != null) UiArt.Draw(tag, "name"); else GUI.Box(tag, "");
            UiArt.Label(new Rect(tag.x, tag.y, tag.width * 0.87f, tag.height), Credits[i][0], 17f * s, UiArt.Ink, TextAnchor.MiddleCenter, true);
            UiArt.Label(new Rect(tag.xMax + 24f * s, tag.y, r.width * 0.35f, tag.height), Credits[i][1], 28f * s, ink, TextAnchor.MiddleLeft, true);
        }
        float bw = r.width * 0.18f, bh = bw * 0.28f;
        if (UiArt.Button(new Rect(r.xMax - bw - r.width * 0.08f, r.yMax - bh - r.height * 0.06f, bw, bh), "닫기", 24f * s)) showCredits = false;
    }
}
