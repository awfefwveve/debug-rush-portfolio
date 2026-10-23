using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>맵 화면 스테이지 카드 (기획서 p.17): 제목 + 대충 스토리 + 빨간 플레이 버튼, 잠긴 스테이지는 어둡게.</summary>
public class StageCard : MonoBehaviour
{
    public TMP_Text title, summary;
    public GameObject lockOverlay;
    public Button[] difficultyButtons; // 기존 프리팹 호환용. [0]만 플레이 버튼으로 사용.

    // 디자이너 카드 바탕 규격 320×640 (PNG 1280×2560) → Resources/UI/stage_card.png 로 넣으면 그 그림을 씀 (글자·플레이 버튼은 코드)
    public const float Width = 320f, Height = 640f;
    static readonly Color PlayRed = new Color(0.86f, 0.2f, 0.24f, 1f);
    static readonly Color CardColor = new Color(0.09f, 0.13f, 0.24f, 1f);
    static readonly Color Accent = new Color(0.22f, 0.82f, 1f);

    public void Setup(StageData s)
    {
        cardArt = CardArt(s);
        bool unlocked = GameManager.I.IsUnlocked(s);
        bool cleared = GameManager.I.IsCleared(s);
        Layout(s.displayName, unlocked ? s.summary : "", !unlocked,
               unlocked ? "" : "잠김\n<size=22>이전 스테이지를 클리어하세요</size>", cleared);

        StarRow(cleared ? GameManager.Stars(s) : -1);

        var play = PlayButton();
        if (play == null) return;
        play.gameObject.SetActive(unlocked);
        play.onClick.RemoveAllListeners();
        // 기획서 p.12: 챕터마다 난이도 고정 (1학년 Easy · 2학년 Normal · 3학년 Hard)
        var diff = (Difficulty)Mathf.Clamp(s.chapter - 1, 0, 2);
        play.onClick.AddListener(() => GameManager.I.StartStage(s, diff));
    }

    /// <summary>아직 기획되지 않은 스테이지 슬롯: 잠금 + "준비 중"</summary>
    // 아트 팀 스테이지 창 (stage_1·2·3 → Resources/UI/stage_card_1~3): 나오는 몬스터로 고름 (벌 / 몬스터2 / 보스)
    string cardArt;
    static string CardArt(StageData s)
    {
        int kind = 1;
        if (s.monsters != null)
            foreach (var m in s.monsters)
            {
                if (m == null || m.prefab == null) continue;
                if (m.prefab.GetComponent<Monster1C>() != null) kind = 3;
                else if (m.prefab.GetComponent<Monster1B>() != null) kind = Mathf.Max(kind, 2);
            }
        return "stage_card_" + kind;
    }

    public void SetupPlaceholder(int chapter, int stage)
    {
        Layout($"{chapter}-{stage}", "", true, "준비 중", false);
        var play = PlayButton();
        if (play != null) play.gameObject.SetActive(false);
    }

    void Layout(string titleText, string summaryText, bool locked, string lockText, bool cleared)
    {
        var rt = (RectTransform)transform;
        rt.sizeDelta = new Vector2(Width, Height);
        var le = GetComponent<LayoutElement>();
        if (le != null) { le.preferredWidth = Width; le.preferredHeight = Height; le.minWidth = Width; }

        var bg = GetComponent<Image>();
        var outline = GetComponent<Outline>();
        if (UiArt.Get("item") != null && bg != null)
        {
            // 아트 팀 칸 틀: 기본 item_2, 클리어 item(이중 테두리) + CLEAR 도장(clear_2)
            var card = cardArt != null ? UiArt.Get(cardArt) : null;
            bg.sprite = card != null ? card : cleared ? UiArt.Get("item") : UiArt.Get("item_2"); bg.type = card != null ? Image.Type.Simple : Image.Type.Sliced; bg.color = Color.white;
            if (outline != null) outline.enabled = false;
            Stamp(false);   // (클리어 도장 clear_2는 업적 그림이라 카드엔 안 씀, 대신 카드 아래 별)
        }
        else
        {
            if (bg != null) bg.color = CardColor;
            if (outline == null) outline = gameObject.AddComponent<Outline>();
            outline.effectColor = cleared ? new Color(1f, 0.82f, 0.25f) : Accent * 0.6f;
            outline.effectDistance = new Vector2(3f, -3f);
        }

        Place(title.rectTransform, new Vector2(0f, 250f), new Vector2(290f, 90f));
        title.text = titleText + (cleared && UiArt.Get("clear_2") == null ? " <size=28><color=#FFD23F>CLEAR</color></size>" : "");
        title.fontSize = 60f; title.fontStyle = FontStyles.Bold; title.alignment = TextAlignmentOptions.Center; title.color = Color.white;

        Place(summary.rectTransform, new Vector2(0f, 90f), new Vector2(270f, 220f));
        summary.text = summaryText;
        summary.fontSize = 24f; summary.alignment = TextAlignmentOptions.Top; summary.color = new Color(0.8f, 0.86f, 1f);
        summary.enableWordWrapping = true;

        lockOverlay.SetActive(locked);
        var lockRect = (RectTransform)lockOverlay.transform;
        lockRect.anchorMin = Vector2.zero; lockRect.anchorMax = Vector2.one; lockRect.offsetMin = lockRect.offsetMax = Vector2.zero;
        var lockImg = lockOverlay.GetComponent<Image>();
        if (lockImg != null) lockImg.color = new Color(0.02f, 0.03f, 0.06f, 0.72f);
        LockIcon(locked);
        var lockLabel = lockOverlay.GetComponentInChildren<TMP_Text>(true);
        if (lockLabel != null)
        {
            Place(lockLabel.rectTransform, new Vector2(0f, -90f), new Vector2(290f, 140f));
            lockLabel.text = lockText; lockLabel.fontSize = 34f; lockLabel.alignment = TextAlignmentOptions.Center;
            lockLabel.color = new Color(0.75f, 0.8f, 0.92f);
        }
        lockOverlay.transform.SetAsLastSibling();

        if (difficultyButtons != null)
            for (int i = 1; i < difficultyButtons.Length; i++)
                if (difficultyButtons[i] != null) difficultyButtons[i].gameObject.SetActive(false);
    }

    Button PlayButton()
    {
        if (difficultyButtons == null || difficultyButtons.Length == 0 || difficultyButtons[0] == null) return null;
        var b = difficultyButtons[0];
        // 아트 팀 버튼 바탕(320×96, 글자 없음)이 Resources/UI/play_button.png 에 있으면 그걸 사용
        var art = Resources.Load<Sprite>("UI/play_button");
        Place((RectTransform)b.transform, new Vector2(0f, -250f), art != null ? new Vector2(250f, 76f) : new Vector2(250f, 76f));
        if (art == null && UiArt.Get("button_red") != null) UiArt.StyleButton(b, 34f);   // 아트 팀 공통 버튼 틀 (연핑크, 마우스 올리면 빨강)
        else if (b.image != null)
        {
            if (art != null) { b.image.sprite = art; b.image.type = Image.Type.Simple; b.image.preserveAspect = true; b.image.color = Color.white; }
            else b.image.color = PlayRed;                 // 기획서 p.17: 빨간 플레이 버튼
        }
        var label = b.GetComponentInChildren<TMP_Text>();
        if (label != null) { label.text = "플레이"; label.fontSize = 34f; label.fontStyle = FontStyles.Bold; label.color = Color.white; }
        b.interactable = true;
        return b;
    }

    /// <summary>클리어한 스테이지 카드 아래 얻은 별 (최고 기록 0~3). -1이면 숨김</summary>
    void StarRow(int stars)
    {
        var t = transform.Find("Stars");
        TMP_Text label;
        if (t == null)
        {
            label = new GameObject("Stars", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            label.transform.SetParent(transform, false);
            Place(label.rectTransform, new Vector2(0f, -Height / 2f - 30f), new Vector2(300f, 48f));
            label.font = KoreanTMPFont.Get(); label.fontSize = 40f; label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
            var le = label.gameObject.AddComponent<LayoutElement>(); le.ignoreLayout = true;
        }
        else label = t.GetComponent<TMP_Text>();
        label.gameObject.SetActive(stars >= 0);
        if (stars < 0) return;
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < 3; i++) sb.Append(i < stars ? "<color=#FFD23F>★</color>" : "<color=#4A5068>★</color>");
        label.text = sb.ToString();
    }

    void Stamp(bool cleared)
    {
        var t = transform.Find("ClearStamp");
        var art = UiArt.Get("clear_2");
        if (art == null) return;
        if (t == null)
        {
            var img = new GameObject("ClearStamp", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            img.transform.SetParent(transform, false);
            img.sprite = art; img.preserveAspect = true; img.raycastTarget = false;
            Place(img.rectTransform, new Vector2(105f, 280f), new Vector2(96f, 96f));
            img.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -12f);
            t = img.transform;
        }
        t.gameObject.SetActive(cleared);
    }

    void LockIcon(bool locked)
    {
        var art = UiArt.Get("lock");
        if (art == null) return;
        var t = lockOverlay.transform.Find("LockIcon");
        if (t == null)
        {
            var img = new GameObject("LockIcon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            img.transform.SetParent(lockOverlay.transform, false);
            img.sprite = art; img.preserveAspect = true; img.raycastTarget = false;
            Place(img.rectTransform, new Vector2(0f, 70f), new Vector2(150f, 150f));
        }
    }

    static void Place(RectTransform r, Vector2 pos, Vector2 size)
    {
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos; r.sizeDelta = size;
    }
}
