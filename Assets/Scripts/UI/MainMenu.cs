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
    static bool MergedStart => UiArt.Get("start_image_merged") != null;

    static readonly string[][] Credits =
    {
        new[] { "기획 · 레벨 · 사운드", "조은희" },
        new[] { "메인 프로그래밍", "양예슬비" },
        new[] { "보조 프로그래밍", "김보정" },
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
        // 시작 노래: 메인 화면 들어오자마자 (씬 로드 이벤트를 놓치는 첫 실행에도 확실히)
        SoundEffectManager.PlayBgm(Resources.Load<AudioClip>("Bgm/title"));
        BuildButtons();
        if (settingsPanel != null) settingsPanel.transform.SetAsLastSibling();
        IntroSplash.Play();   // 게임 실행 때 한 번: 팀 로고 → 학교 → 타이틀 (메뉴를 다 만든 뒤에 띄워야 메뉴가 인트로 캔버스에 붙지 않음)
    }

    void BuildIllustration()
    {
        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;
        var root = canvas.transform;
        var bg = root.Find("Background");
        var title = root.Find("Title")?.GetComponent<TMP_Text>();
        var subtitle = root.Find("Subtitle")?.GetComponent<TMP_Text>();

        // 팔 합친 시작 일러 (팔이 그림에 들어 있어 어떤 PC에서도 같은 자리). 그래픽 팀 합본이 오면 Resources/UI/start_image_merged.png 만 바꾸면 됨
        var art = UiArt.Get("start_image_merged"); if (art == null) art = UiArt.Get("start_image");
        if (art != null && bg != null && bg.TryGetComponent(out Image bgImg))
        {
            // 아트 팀 시작 일러스트 1920×1080: 그림 전체가 다 보이게 (비율 유지, 화면 비율이 다르면 남는 곳은 검정)
            var r = bgImg.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f); r.anchoredPosition = Vector2.zero; r.sizeDelta = new Vector2(1920f, 1080f);
            bgImg.sprite = art; bgImg.color = Color.white; bgImg.type = Image.Type.Simple; bgImg.preserveAspect = false;
            // 그림 영역은 항상 1920×1080 크기 그대로 두고 통째로 확대·축소 → 안에 붙인 버튼·팔·손가락이 어떤 해상도·비율에서도 그림과 같은 자리 (예전엔 영역 크기가 바뀌어 버튼만 따로 놀았음)
            var old = bg.GetComponent<AspectRatioFitter>(); if (old != null) Destroy(old);
            if (bg.GetComponent<FitScale>() == null) bg.gameObject.AddComponent<FitScale>();
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
            // 시작 그림 영역(비율 유지로 맞춘 Background) 왼쪽 위 기준, 가로 4.5%·세로 5% 안쪽 여백 → 어떤 화면 비율에서도 잘리지 않음
            var holder = art != null && bg != null ? bg : root;
            var logoImg = Img(holder, "Logo", logo, Vector2.zero, new Vector2(680f, 340f), Color.white);
            var lr = logoImg.rectTransform; lr.anchorMin = lr.anchorMax = lr.pivot = new Vector2(0f, 1f); lr.anchoredPosition = new Vector2(86f, -54f);
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
        // 버튼도 시작 그림 영역 안 오른쪽 아래 기준 (16:10 등에서 위아래 검은 띠로 내려가지 않게)
        var bgArt = UiArt.Get("start_image") != null ? root.Find("Background") : null;
        var holder = bgArt != null ? bgArt : root;
        string[] names = { "Start", "Setting", "Quit" };
        for (int i = 0; i < names.Length; i++)
        {
            var t = root.Find(names[i]);
            if (t == null || !t.TryGetComponent(out Button b)) continue;
            t.SetParent(holder, false);
            var r = (RectTransform)t;
            // 오른쪽 아래 기준. 버튼 80 높이, 간격 10, 아래 다이아 묶음과 22 여백 (버튼 간격의 약 2배)
            r.anchorMin = r.anchorMax = new Vector2(1f, 0f); r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = new Vector2(-270f, 442f - i * 90f); r.sizeDelta = new Vector2(440f, 80f);
            UiArt.StyleButton(b, 34f);
        }

        // 업적 버튼(씬) → 마름모. 리셋·크레딧은 새로 만듦
        var ach = root.Find("Achievements");
        if (ach != null) ach.SetParent(holder, false);
        if (ach != null && ach.TryGetComponent(out Button achBtn)) Diamond(achBtn, "achievements", "업적", 0);
        Diamond(NewButton(holder, "Reset", () => { if (!showCredits) confirmReset = true; }), "reset", "리셋", 1);
        Diamond(NewButton(holder, "Credit", () => { if (!confirmReset) showCredits = true; }), "credit", "크레딧", 2);

        // 시작 일러스트 분리본 (10/23): 로봇 팔(start_arm)은 버튼 뒤, 집게 손가락(start_finger)은 시작 버튼 위, 테두리(start_line)는 맨 위.
        // 위치 = 예전 합본 일러에서 팔이 있던 자리 (1920×1080 기준 왼쪽 위 픽셀). 손가락은 팔 그림 안 (1, 519) 자리와 같은 모양
        if (bgArt != null)
        {
            var arm = UiArt.Get("start_arm");
            if (arm != null && !MergedStart) Place(holder, "Arm", arm, new Vector2(1368f, -48f)).transform.SetSiblingIndex(0);
            var finger = UiArt.Get("start_finger");
            if (finger != null && !MergedStart) Place(holder, "Finger", finger, new Vector2(1369f, 471f)).transform.SetAsLastSibling();   // 합본을 쓰면 손가락은 숨김
            var line = UiArt.Get("start_line");
            if (line != null && !MergedStart) Layer(holder, "Frame", line).transform.SetAsLastSibling();   // 새 시작 일러(10/25)는 테두리·팔까지 한 장
        }
    }

    void Diamond(Button b, string art, string label, int slot)
    {
        if (b == null) return;
        var r = (RectTransform)b.transform;
        r.anchorMin = r.anchorMax = new Vector2(1f, 0f); r.pivot = new Vector2(0.5f, 0.5f);   // 오른쪽 아래 기준: 마름모 위끝 200 (버튼 아래끝 222) · 아래 글자 끝 44
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

    /// <summary>시작 그림 안 고정 자리 (1920×1080 기준 왼쪽 위 픽셀 위치·그림 원래 크기). 화면 비율이 달라 그림이 줄어도 같이 줄어듦</summary>
    static Image Place(Transform holder, string name, Sprite sprite, Vector2 topLeftPx)
    {
        var img = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        img.transform.SetParent(holder, false);
        float w = sprite.rect.width, h = sprite.rect.height;
        var r = img.rectTransform;
        r.anchorMin = new Vector2(topLeftPx.x / 1920f, 1f - (topLeftPx.y + h) / 1080f);
        r.anchorMax = new Vector2((topLeftPx.x + w) / 1920f, 1f - topLeftPx.y / 1080f);
        r.offsetMin = r.offsetMax = Vector2.zero;
        img.sprite = sprite; img.raycastTarget = false;
        return img;
    }

    /// <summary>시작 그림과 같은 크기·위치의 겹침 그림 (그림 영역 전체에 맞춤)</summary>
    static Image Layer(Transform holder, string name, Sprite sprite)
    {
        var img = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        img.transform.SetParent(holder, false);
        var r = img.rectTransform; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
        img.sprite = sprite; img.raycastTarget = false;
        return img;
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
            if (showCredits) { showCredits = false; creditOpenedAt = -1f; }
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

    // 소리 저작권 표기 (10/23 B): 크레딧 아래쪽에 이어서
    static readonly string[] SoundCredits =
    {
        "공격 소리", "SFX provided by 셀바이뮤직", "Title : Laser gun 5-short", "https://sellbuymusic.com/md/sgrnncc-efzhwbn", "",
        "공격 소리2", "SFX provided by 셀바이뮤직", "Title : Laser gun 7-short", "https://sellbuymusic.com/md/sypnncx-ufzhwbn", "",
        "공격 소리3", "SFX provided by 셀바이뮤직", "Title : Storytelling Cartoon SciFiLazer 01", "https://sellbuymusic.com/md/sppxkwf-rfzhwbn", "",
        "대쉬", "SFX provided by 셀바이뮤직", "Title : Whoosh 9-low-1", "https://sellbuymusic.com/md/suyntbh-lfzhwbn", "",
        "그 외", "PIXABAY",
    };

    float creditScroll, creditOpenedAt = -1f;

    /// <summary>크레딧: 창 안 칸에서 영화 엔딩처럼 아래→위로 천천히 자동 스크롤 (마우스 휠·방향키로 직접 움직여도 됨), 끝에서 멈춤</summary>
    void DrawCredits(float s)
    {
        SettingsWindow.Dim();
        var r = UiArt.Window("크레딧", 1100f, 650f);
        var ink = new Color(0.12f, 0.13f, 0.18f);
        var area = new Rect(r.x + r.width * 0.07f, r.y + r.height * 0.17f, r.width * 0.84f, r.height * 0.62f);   // 창 그림의 밝은 칸 안
        float nameW = r.width * 0.19f, nameH = nameW * 80f / 224f, rowH = nameH * 1.1f, lineH = 34f * s;
        float contentH = area.height * 0.5f + 70f * s + Credits.Length * rowH + 40f * s + 50f * s + SoundCredits.Length * lineH + area.height * 0.3f;
        float max = Mathf.Max(0f, contentH - area.height);

        if (creditOpenedAt < 0f) { creditOpenedAt = Time.unscaledTime; creditScroll = 0f; }
        var e = Event.current;
        if (e.type == EventType.ScrollWheel && area.Contains(e.mousePosition)) { creditScroll += e.delta.y * 30f * s; e.Use(); }
        if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.DownArrow || e.keyCode == KeyCode.UpArrow)) { creditScroll += (e.keyCode == KeyCode.DownArrow ? 60f : -60f) * s; e.Use(); }
        if (e.type == EventType.Repaint && Time.unscaledTime - creditOpenedAt > 0.8f) creditScroll += 45f * s * Time.unscaledDeltaTime;   // 자동으로 천천히
        creditScroll = Mathf.Clamp(creditScroll, 0f, max);

        GUI.BeginGroup(area);   // 칸 밖은 잘림
        float y = area.height * 0.5f - creditScroll;   // 칸 가운데쯤에서 시작해 위로 올라감
        UiArt.Label(new Rect(0f, y, area.width, 60f * s), "팀 팥없찐", 38f * s, ink, TextAnchor.MiddleCenter, true);
        y += 70f * s;
        float x = area.width * 0.18f;
        for (int i = 0; i < Credits.Length; i++, y += rowH)
        {
            var tag = new Rect(x, y, nameW, nameH);
            if (UiArt.Tex("name") != null) UiArt.Draw(tag, "name"); else GUI.Box(tag, "");
            UiArt.Label(new Rect(tag.x, tag.y, tag.width * 0.87f, tag.height), Credits[i][0], 17f * s, UiArt.Ink, TextAnchor.MiddleCenter, true);
            UiArt.Label(new Rect(tag.xMax + 24f * s, tag.y, area.width * 0.45f, tag.height), Credits[i][1], 28f * s, ink, TextAnchor.MiddleLeft, true);
        }
        y += 40f * s;
        UiArt.Label(new Rect(0f, y, area.width, 50f * s), "사운드", 32f * s, ink, TextAnchor.MiddleCenter, true);
        y += 50f * s;
        foreach (var line in SoundCredits)
        {
            bool head = line == "공격 소리" || line == "공격 소리2" || line == "공격 소리3" || line == "대쉬" || line == "그 외";
            UiArt.Label(new Rect(0f, y, area.width, lineH), line, (head ? 24f : 20f) * s, head ? ink : new Color(0.25f, 0.27f, 0.34f), TextAnchor.MiddleCenter, head);
            y += lineH;
        }
        GUI.EndGroup();

        float bw = r.width * 0.18f, bh = bw * 0.28f;
        if (UiArt.Button(new Rect(r.xMax - bw - r.width * 0.08f, r.yMax - bh - r.height * 0.06f, bw, bh), "닫기", 24f * s)) { showCredits = false; creditOpenedAt = -1f; }
    }
}
