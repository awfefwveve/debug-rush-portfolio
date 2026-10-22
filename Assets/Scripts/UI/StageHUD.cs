using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 스테이지 HUD (기획서 p.7, p.18): 상단 얇은 바 = 제목 / 하트 / 시간 10:00 / STAGE / 오류 수정 n/총,
/// 좌하단 보유 자료 2칸 + 하단 조작 안내 한 줄 (모두 맵 위에 겹침). 결과 패널.
/// 1920×1080 기준. 카메라는 상단 바 아래로 방이 화면을 꽉 채우게 맞춤.
/// </summary>
public class StageHUD : MonoBehaviour
{
    public StageManager stage;
    public TMP_Text timerText;
    public Image[] hearts;                     // 최대 3개 배치, 챕터 HP만큼 활성
    public Image[] heldSlots;                  // 소지품 슬롯 2개
    public ItemIconSet icons;

    [Header("결과")]
    public GameObject resultPanel;
    public TMP_Text resultText;
    public Button retryButton, mapButton;
    Button nextButton;
    Image failIcon;
    int parries, hits, lastHp;

    static readonly Color Band = new Color(0.04f, 0.06f, 0.12f, 0.78f);
    static readonly Color Accent = new Color(0.22f, 0.82f, 1f);
    static readonly Color Gold = new Color(1f, 0.82f, 0.25f);

    PlayerController player;
    TMP_Text stageLabel, fixLabel;
    TMP_Text[] itemNames;

    void Start()
    {
        player = stage.player;
        BuildLayout();
        stage.TimeChanged += OnTime;
        stage.StageEnded += OnEnded;
        player.Health.HpChanged += OnHp;
        player.Inventory.Changed += RefreshHeld;
        player.ParrySucceeded += () => parries++;
        player.Health.HpChanged += (hp, max) => { if (hp < lastHp) hits++; lastHp = hp; };
        lastHp = player.Health.Hp;

        OnHp(player.Health.Hp, player.Health.MaxHp);
        RefreshHeld();
        resultPanel.SetActive(false);

        retryButton.onClick.AddListener(() =>
        {
            if (GameManager.I != null) GameManager.I.StartStage(stage.Data, stage.Diff);
            else SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        });
        mapButton.onClick.AddListener(() => SceneManager.LoadScene("Map"));
    }

    void OnTime(float t)
    {
        int s = Mathf.CeilToInt(t);
        if (s == shownTime) return;
        shownTime = s;
        timerText.text = $"{s / 60:00}:{s % 60:00}";       // 기획서 p.18: 10:00 형식
        timerText.color = t <= 30f ? new Color(1f, 0.35f, 0.35f) : Color.white;
    }

    int shownStage = -1, shownDone = -1, shownTotal = -1, shownTime = -1;

    void Update()
    {
        if (failIcon != null && failIcon.gameObject.activeInHierarchy)   // 실패 그림 두 장 번갈아
            failIcon.sprite = UiArt.Get(Mathf.FloorToInt(Time.unscaledTime * 2.5f) % 2 == 0 ? "icon_failed" : "icon_2_failed");
        if (stage == null || stage.Data == null) return;
        // 글자는 값이 바뀔 때만 다시 만듦 (매 프레임 새로 만들면 메모리 정리로 끊김)
        if (stageLabel != null && shownStage != stage.Data.stage) { shownStage = stage.Data.stage; stageLabel.text = stage.Data.stage <= 0 ? "TUTORIAL" : $"STAGE {stage.Data.stage:00}"; }   // 기획서 p.7 "STAGE 02" 형식
        if (fixLabel != null)
        {
            int done = 0, total = 0;
            if (stage.terminals != null)
                foreach (var t in stage.terminals)
                    if (t != null && t.gameObject.activeInHierarchy) { done += Mathf.Min(t.Completed, t.goal); total += t.goal; }
            if (done != shownDone || total != shownTotal) { shownDone = done; shownTotal = total; fixLabel.text = $"오류 수정 <color=#FFD23F>{done}</color>/{total}"; }
        }
    }

    // ---------- 레이아웃 ----------
    void BuildLayout()
    {
        var canvas = timerText != null ? timerText.canvas : GetComponentInParent<Canvas>();
        if (canvas == null) return;
        var root = canvas.transform;
        canvas.sortingOrder = 10;                         // 화면 효과보다 위
        // 세로 1080 기준으로 맞추고 가로는 화면 비율에 따라 늘어남 → 요소는 화면 모서리에 붙임 (16:10 등에서 잘림 방지)
        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null) { scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 1f; }

        Vector2 TL = new Vector2(0, 1), TC = new Vector2(0.5f, 1), TR = new Vector2(1, 1), BL = new Vector2(0, 0), BR = new Vector2(1, 0);
        // 상단: 화면 맨 위에 붙은 얇은 반투명 바 (기획서 p.7) — 맵 위에 겹침
        const float barH = 56f, y = -28f;
        var top = MakeBand(root, "TopBand", true, barH, Band); top.transform.SetAsFirstSibling();
        var line = MakeBand(root, "TopLine", true, 2f, Accent * 0.7f, -barH); line.transform.SetSiblingIndex(1);
        var oldPanel = root.Find("HUDPanel"); if (oldPanel != null) oldPanel.gameObject.SetActive(false);

        Label(root, "청강 디버그 러시", TL, new Vector2(150f, y), new Vector2(280f, 50f), 30f, TextAlignmentOptions.Left, Accent, true);
        for (int i = 0; i < hearts.Length; i++)
        {
            Place(hearts[i].rectTransform, TL, new Vector2(330f + i * 48f, y), new Vector2(40f, 40f));
            hearts[i].preserveAspect = true;
        }
        Place(timerText.rectTransform, TC, new Vector2(0f, y), new Vector2(200f, 56f));
        timerText.fontSize = 42f; timerText.fontStyle = FontStyles.Bold; timerText.alignment = TextAlignmentOptions.Center;
        stageLabel = Label(root, "", TR, new Vector2(-420f, y), new Vector2(220f, 50f), 28f, TextAlignmentOptions.Right, Color.white, true);
        fixLabel = Label(root, "", TR, new Vector2(-150f, y), new Vector2(260f, 50f), 28f, TextAlignmentOptions.Right, Color.white, true);

        // 좌하단: 작은 보유 자료 박스 (맵 위에 겹침)
        var box = Panel(root, "HeldBox", BL, new Vector2(98f, 62f), new Vector2(180f, 112f), new Color(0.04f, 0.06f, 0.12f, 0.82f));
        var bol = box.gameObject.AddComponent<Outline>(); bol.effectColor = Accent * 0.6f; bol.effectDistance = new Vector2(2f, -2f);
        if (UiArt.Get("item_2") != null) { bol.enabled = false; box.sprite = UiArt.Get("item_2"); box.type = Image.Type.Sliced; box.pixelsPerUnitMultiplier = 2.5f; box.color = Color.white; }   // 아트 팀 아이템 창
        box.transform.SetSiblingIndex(2);
        Label(root, "보유 자료", BL, new Vector2(98f, 100f), new Vector2(170f, 28f), 20f, TextAlignmentOptions.Center, Accent, true);
        itemNames = null;
        for (int i = 0; i < heldSlots.Length; i++)
        {
            Vector2 p = new Vector2(60f + i * 76f, 48f);
            var frame = Panel(root, "SlotFrame" + i, BL, p, new Vector2(64f, 64f), new Color(0.09f, 0.13f, 0.24f, 1f));
            var ol = frame.gameObject.AddComponent<Outline>(); ol.effectColor = Accent * 0.7f; ol.effectDistance = new Vector2(2f, -2f);
            if (UiArt.Get("item") != null) { ol.enabled = false; frame.sprite = UiArt.Get("item"); frame.type = Image.Type.Sliced; frame.pixelsPerUnitMultiplier = 3f; frame.color = Color.white; }   // 아트 팀 아이템 칸
            frame.transform.SetSiblingIndex(heldSlots[i].transform.GetSiblingIndex());
            Place(heldSlots[i].rectTransform, BL, p, new Vector2(54f, 54f));
            heldSlots[i].preserveAspect = true;
            var old = heldSlots[i].transform.Find("ItemNameLabel"); if (old != null) Destroy(old.gameObject);
        }

        // 하단 가운데: 얇은 조작 안내 한 줄
        var help = Label(root, "<color=#39D0FF>WASD</color> 이동  ·  <color=#39D0FF>E</color> 제작·회수·수정  ·  <color=#39D0FF>Space</color> 점프(패링)  ·  <color=#39D0FF>Shift</color> 대쉬  ·  <color=#39D0FF>Q</color> 버리기  ·  <color=#39D0FF>Tab</color> 조합표  ·  <color=#39D0FF>ESC</color> 일시정지",
              new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(1260f, 30f), 19f, TextAlignmentOptions.Center, new Color(0.85f, 0.9f, 1f, 0.75f), false);
        help.outlineWidth = 0.2f; help.outlineColor = new Color32(5, 8, 16, 255);
        var helpBg = Panel(root, "HelpBg", new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(1300f, 30f), new Color(0.02f, 0.03f, 0.07f, 0.85f));   // 밝은 바닥 위에서도 읽히게
        helpBg.transform.SetSiblingIndex(help.transform.GetSiblingIndex());
        help.color = new Color(0.9f, 0.94f, 1f, 0.95f);

        // 카메라 자동 맞춤 + 조합표(Tab, 맵 위에 겹침)
        var cam = Camera.main;
        if (cam != null && cam.GetComponent<CameraFit>() == null) cam.gameObject.AddComponent<CameraFit>();
        gameObject.AddComponent<RecipePanel>().Build(root, icons, stage);

        StyleResult();
    }

    void StyleResult()
    {
        // [다음 스테이지] 버튼: 다시하기 버튼을 복제해서 맨 위에. 아래로 다시하기, 스테이지 선택
        if (nextButton == null && retryButton != null)
        {
            nextButton = Instantiate(retryButton, retryButton.transform.parent);
            nextButton.name = "Next";
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(() =>
            {
                var n = NextStage();
                if (n != null && GameManager.I != null) GameManager.I.StartStage(n, (Difficulty)Mathf.Clamp(n.chapter - 1, 0, 2));
            });
            var label = nextButton.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = "다음 스테이지";
            else { var ut = nextButton.GetComponentInChildren<Text>(); if (ut != null) ut.text = "다음 스테이지"; }
            nextButton.gameObject.SetActive(false);
            var rr = (RectTransform)retryButton.transform; var nr = (RectTransform)nextButton.transform;
            // 결과 창 배치: 위에 글자, 아래로 다음 스테이지 · 다시 하기 · 맵으로
            var tr = resultText.rectTransform; tr.anchoredPosition = new Vector2(0f, 140f);
            nr.anchoredPosition = new Vector2(0f, 30f);
            rr.anchoredPosition = new Vector2(0f, -50f);
            ((RectTransform)mapButton.transform).anchoredPosition = new Vector2(0f, -130f);
            var ni = nextButton.GetComponent<Image>(); if (ni != null) ni.color = new Color(0.95f, 0.75f, 0.2f);
        }
        var img = resultPanel.GetComponent<Image>();
        if (img != null) img.color = new Color(0.04f, 0.06f, 0.12f, 0.94f);
        resultText.fontSize = 72f; resultText.fontStyle = FontStyles.Bold;
        if (UiArt.Get("button_gray") != null)
        {
            // 아트 팀 버튼 틀: 다시 하기·맵으로 회색, 다음 스테이지 빨강 (마우스 올리면 빨강)
            foreach (var b in new[] { retryButton, mapButton, nextButton }) if (b != null) { UiArt.StyleButton(b, 30f); ((RectTransform)b.transform).sizeDelta = new Vector2(340f, 66f); }
            if (nextButton != null) nextButton.image.sprite = UiArt.Get("button_red");
        }
        else
            foreach (var b in new[] { retryButton, mapButton })   // (다음 스테이지는 노란색)
            {
                var bi = b.GetComponent<Image>(); if (bi != null) bi.color = new Color(0.13f, 0.45f, 0.8f);
            }
        if (UiArt.Get("back_failed") != null)
        {
            ((RectTransform)resultPanel.transform).sizeDelta = new Vector2(1000f, 591f);
            failIcon = new GameObject("FailIcon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            failIcon.transform.SetParent(resultPanel.transform, false);
            Place(failIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-250f, -10f), new Vector2(400f, 400f));
            failIcon.preserveAspect = true; failIcon.raycastTarget = false;
            failIcon.gameObject.SetActive(false);
        }
    }

    void OnHp(int hp, int max)
    {
        for (int i = 0; i < hearts.Length; i++)
        {
            hearts[i].gameObject.SetActive(i < max);
            hearts[i].color = i < hp ? Color.white : new Color(0.25f, 0.25f, 0.3f, 0.8f);
            if (UiArt.Get("heart_3") != null) { hearts[i].sprite = UiArt.Get(i < hp ? "heart_3" : "heart_4"); hearts[i].color = Color.white; }   // 아트 팀 하트 (꽉 참 / 빈 하트)
        }
    }

    void RefreshHeld()
    {
        var items = player.Inventory.items;
        for (int i = 0; i < heldSlots.Length; i++)
        {
            bool has = i < items.Count;
            heldSlots[i].enabled = has;
            if (has) heldSlots[i].sprite = icons.Get(items[i]);
            if (itemNames != null && i < itemNames.Length && itemNames[i] != null)
                itemNames[i].text = has ? ItemNames.Get(items[i]) : "<color=#5A6680>비어 있음</color>";
        }
    }

    void OnEnded(bool cleared)
    {
        // 학년 마지막 스테이지(1-5) 클리어: 엔딩 대사 뒤 "다음 학년 준비 중" 한 장 → 결과 창
        if (cleared && stage.Data != null && stage.Data.stage >= StageManager.StagesPerChapter && NextStage() == null)
        { StartCoroutine(ChapterEndThenResult()); return; }
        ShowResult(cleared);
    }

    System.Collections.IEnumerator ChapterEndThenResult()
    {
        yield return ChapterEndCard.Show(stage.Data.chapter);
        ShowResult(true);
    }

    void ShowResult(bool cleared)
    {
        resultPanel.SetActive(true);
        resultPanel.transform.SetAsLastSibling();
        resultText.text = cleared ? "<color=#FFD23F>STAGE CLEAR!</color>" + ClearRecord() : "<color=#FFFFFF>FAILED...</color>";
        // 클리어했고 같은 학년에 다음 스테이지가 있으면 [다음 스테이지] (마지막 스테이지면 숨김 → 스테이지 선택으로)
        var next = cleared ? NextStage() : null;
        if (nextButton != null) nextButton.gameObject.SetActive(next != null);

        // 실패: 아트 팀 실패 창(back_failed) + 우는 주인공(icon_failed), 글자·버튼은 오른쪽으로
        if (failIcon != null)
        {
            var img = resultPanel.GetComponent<Image>();
            if (img != null) { img.sprite = cleared ? null : UiArt.Get("back_failed"); img.type = Image.Type.Sliced; img.color = cleared ? new Color(0.04f, 0.06f, 0.12f, 0.94f) : Color.white; }
            failIcon.gameObject.SetActive(!cleared);
            float x = cleared ? 0f : 200f;
            foreach (var r in new[] { resultText.rectTransform, (RectTransform)retryButton.transform, (RectTransform)mapButton.transform, nextButton != null ? (RectTransform)nextButton.transform : null })
                if (r != null) r.anchoredPosition = new Vector2(x, r.anchoredPosition.y);
            if (!cleared) resultText.rectTransform.sizeDelta = new Vector2(520f, resultText.rectTransform.sizeDelta.y);
        }
        if (cleared)   // 제목 + 별 + 기록 세 줄이 버튼 위에 들어가게
        {
            var tr = resultText.rectTransform;
            tr.anchoredPosition = new Vector2(tr.anchoredPosition.x, 175f); tr.sizeDelta = new Vector2(900f, 220f);
            resultText.enableWordWrapping = false; resultText.alignment = TMPro.TextAlignmentOptions.Center;
        }
    }

    /// <summary>클리어 기록: 별 3개(남은 시간 30% 이상 · 무피격 · 패링 1회 이상) + 기록 한 줄</summary>
    string ClearRecord()
    {
        float limit = stage.Data != null ? stage.Data.TimeFor(stage.Diff) : 0f;
        float left = Mathf.Max(0f, stage.TimeLeft);
        bool[] stars = { limit > 0f && left >= limit * 0.3f, hits == 0, parries > 0 };
        var sb = new System.Text.StringBuilder("\n<size=56>");
        foreach (var s in stars) sb.Append(s ? "<color=#FFD23F>★</color>" : "<color=#4A5068>★</color>");
        int sec = Mathf.CeilToInt(left);
        sb.Append("</size>\n<size=26><color=#DDE3F5>남은 시간 ").Append(sec / 60).Append(':').Append((sec % 60).ToString("00"))
          .Append("  ·  피격 ").Append(hits).Append("회  ·  패링 ").Append(parries).Append("회</color></size>");
        return sb.ToString();
    }

    StageData NextStage()
    {
        var gm = GameManager.I;
        if (gm == null || gm.allStages == null || stage.Data == null) return null;
        StageData best = null;
        foreach (var s in gm.allStages)
            if (s != null && s.chapter == stage.Data.chapter && s.stage > stage.Data.stage && (best == null || s.stage < best.stage)) best = s;
        return best != null && gm.IsUnlocked(best) ? best : null;
    }

    // ---------- 헬퍼 ----------
    public static void Place(RectTransform r, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        r.anchorMin = r.anchorMax = anchor;
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos; r.sizeDelta = size;
    }

    static Image MakeBand(Transform parent, string name, bool top, float height, Color color, float offset = 0f)
    {
        var img = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        img.transform.SetParent(parent, false);
        var r = img.rectTransform;
        r.anchorMin = new Vector2(0f, top ? 1f : 0f); r.anchorMax = new Vector2(1f, top ? 1f : 0f);
        r.pivot = new Vector2(0.5f, top ? 1f : 0f);
        r.anchoredPosition = new Vector2(0f, offset); r.sizeDelta = new Vector2(0f, height);
        img.color = color; img.raycastTarget = false;
        return img;
    }

    static Image Panel(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color)
    {
        var img = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        img.transform.SetParent(parent, false);
        Place(img.rectTransform, anchor, pos, size);
        img.color = color; img.raycastTarget = false;
        return img;
    }

    public static TMP_Text Label(Transform parent, string text, Vector2 anchor, Vector2 pos, Vector2 size, float fontSize, TextAlignmentOptions align, Color color, bool bold)
    {
        var go = new GameObject("HUD_" + text, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        Place((RectTransform)go.transform, anchor, pos, size);
        var t = go.GetComponent<TextMeshProUGUI>();
        t.font = KoreanTMPFont.Get();
        t.text = text; t.fontSize = fontSize; t.alignment = align; t.color = color;
        if (bold) t.fontStyle = FontStyles.Bold;
        t.raycastTarget = false; t.enableWordWrapping = false;
        return t;
    }
}
