using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 스테이지 HUD (기획서 p.7, p.18): 상단 띠 = 제목 / 하트 / 시간 10:00 / STAGE / 오류 수정 n/총,
/// 하단 띠 = 보유 자료 2칸(좌하단) + 조작 안내. 결과 패널.
/// 1920×1080 기준. 카메라는 방 위아래로 140px 띠가 남도록 맞춰져 있음.
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

    static readonly Color Band = new Color(0.04f, 0.06f, 0.12f, 0.96f);
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
        timerText.text = $"{s / 60:00}:{s % 60:00}";       // 기획서 p.18: 10:00 형식
        timerText.color = t <= 30f ? new Color(1f, 0.35f, 0.35f) : Color.white;
    }

    void Update()
    {
        if (stage == null || stage.Data == null) return;
        if (stageLabel != null) stageLabel.text = $"STAGE {stage.Data.chapter}-{stage.Data.stage:00}";
        if (fixLabel != null)
        {
            int done = 0, total = 0;
            if (stage.terminals != null)
                foreach (var t in stage.terminals)
                    if (t != null && t.gameObject.activeInHierarchy) { done += Mathf.Min(t.Completed, t.goal); total += t.goal; }
            fixLabel.text = $"오류 수정 <color=#FFD23F>{done}</color>/{total}";
        }
    }

    // ---------- 레이아웃 ----------
    void BuildLayout()
    {
        var canvas = timerText != null ? timerText.canvas : GetComponentInParent<Canvas>();
        if (canvas == null) return;
        var root = canvas.transform;

        // 상단 띠
        var top = Panel(root, "TopBand", new Vector2(0f, 470f), new Vector2(1920f, 140f), Band);
        top.transform.SetAsFirstSibling();
        var line = Panel(root, "TopLine", new Vector2(0f, 400f), new Vector2(1920f, 3f), Accent * 0.8f);
        line.transform.SetSiblingIndex(1);
        var oldPanel = root.Find("HUDPanel"); if (oldPanel != null) oldPanel.gameObject.SetActive(false);

        Label(root, "청강 디버그 러시", new Vector2(-760f, 470f), new Vector2(360f, 60f), 36f, TextAlignmentOptions.Left, Accent, true);
        for (int i = 0; i < hearts.Length; i++)
        {
            Place(hearts[i].rectTransform, new Vector2(-500f + i * 68f, 470f), new Vector2(58f, 58f));
            hearts[i].preserveAspect = true;
        }
        Place(timerText.rectTransform, new Vector2(0f, 470f), new Vector2(300f, 90f));
        timerText.fontSize = 66f; timerText.fontStyle = FontStyles.Bold; timerText.alignment = TextAlignmentOptions.Center;
        stageLabel = Label(root, "", new Vector2(470f, 470f), new Vector2(300f, 60f), 34f, TextAlignmentOptions.Right, Color.white, true);
        fixLabel = Label(root, "", new Vector2(780f, 470f), new Vector2(300f, 60f), 34f, TextAlignmentOptions.Right, Color.white, true);

        // 하단 띠
        var bottom = Panel(root, "BottomBand", new Vector2(0f, -470f), new Vector2(1920f, 140f), Band);
        bottom.transform.SetSiblingIndex(2);
        var line2 = Panel(root, "BottomLine", new Vector2(0f, -400f), new Vector2(1920f, 3f), Accent * 0.8f);
        line2.transform.SetSiblingIndex(3);

        Label(root, "보유 자료", new Vector2(-790f, -428f), new Vector2(260f, 36f), 24f, TextAlignmentOptions.Left, Accent, true);
        itemNames = new TMP_Text[heldSlots.Length];
        for (int i = 0; i < heldSlots.Length; i++)
        {
            Vector2 p = new Vector2(-860f + i * 300f, -488f);
            var frame = Panel(root, "SlotFrame" + i, p, new Vector2(86f, 86f), new Color(0.09f, 0.13f, 0.24f, 1f));
            var ol = frame.gameObject.AddComponent<Outline>(); ol.effectColor = Accent * 0.7f; ol.effectDistance = new Vector2(3f, -3f);
            frame.transform.SetSiblingIndex(heldSlots[i].transform.GetSiblingIndex());
            Place(heldSlots[i].rectTransform, p, new Vector2(72f, 72f));
            heldSlots[i].preserveAspect = true;
            var old = heldSlots[i].transform.Find("ItemNameLabel"); if (old != null) Destroy(old.gameObject);
            itemNames[i] = Label(root, "", p + new Vector2(150f, 0f), new Vector2(200f, 60f), 24f, TextAlignmentOptions.Left, Color.white, false);
        }

        Label(root, "<color=#39D0FF>WASD</color> 이동    <color=#39D0FF>E</color> 제작·회수·수정    <color=#39D0FF>Space</color> 점프(패링)    <color=#39D0FF>Q</color> 버리기    <color=#39D0FF>ESC</color> 일시정지",
              new Vector2(420f, -470f), new Vector2(1000f, 50f), 24f, TextAlignmentOptions.Center, new Color(0.85f, 0.9f, 1f), false);

        StyleResult();
    }

    void StyleResult()
    {
        var img = resultPanel.GetComponent<Image>();
        if (img != null) img.color = new Color(0.04f, 0.06f, 0.12f, 0.94f);
        resultText.fontSize = 72f; resultText.fontStyle = FontStyles.Bold;
        foreach (var b in new[] { retryButton, mapButton })
        {
            var bi = b.GetComponent<Image>(); if (bi != null) bi.color = new Color(0.13f, 0.45f, 0.8f);
        }
    }

    void OnHp(int hp, int max)
    {
        for (int i = 0; i < hearts.Length; i++)
        {
            hearts[i].gameObject.SetActive(i < max);
            hearts[i].color = i < hp ? Color.white : new Color(0.25f, 0.25f, 0.3f, 0.8f);
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
        resultPanel.SetActive(true);
        resultPanel.transform.SetAsLastSibling();
        resultText.text = cleared ? "<color=#FFD23F>STAGE CLEAR!</color>" : "<color=#FF4757>FAILED...</color>";
    }

    // ---------- 헬퍼 ----------
    static void Place(RectTransform r, Vector2 pos, Vector2 size)
    {
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos; r.sizeDelta = size;
    }

    static Image Panel(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
    {
        var img = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        img.transform.SetParent(parent, false);
        Place(img.rectTransform, pos, size);
        img.color = color; img.raycastTarget = false;
        return img;
    }

    static TMP_Text Label(Transform parent, string text, Vector2 pos, Vector2 size, float fontSize, TextAlignmentOptions align, Color color, bool bold)
    {
        var go = new GameObject("HUD_" + text, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        Place((RectTransform)go.transform, pos, size);
        var t = go.GetComponent<TextMeshProUGUI>();
        t.font = KoreanTMPFont.Get();
        t.text = text; t.fontSize = fontSize; t.alignment = align; t.color = color;
        if (bold) t.fontStyle = FontStyles.Bold;
        t.raycastTarget = false; t.enableWordWrapping = false;
        return t;
    }
}
