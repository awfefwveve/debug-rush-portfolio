using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>스테이지 HUD: 시간, HP, 소지품, 결과 패널</summary>
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

    PlayerController player;
    TMP_Text stageLabel, fixLabel;

    void Start()
    {
        player = stage.player;
        BuildDesignLabels();
        stage.TimeChanged += OnTime;
        stage.StageEnded += OnEnded;
        player.Health.HpChanged += OnHp;
        player.Inventory.Changed += RefreshHeld;

        OnHp(player.Health.Hp, player.Health.MaxHp);
        RefreshHeld();
        resultPanel.SetActive(false);

        retryButton.onClick.AddListener(() => GameManager.I.StartStage(stage.Data, stage.Diff));
        mapButton.onClick.AddListener(() => SceneManager.LoadScene("Map"));
    }

    void OnTime(float t)
    {
        int s = Mathf.CeilToInt(t);
        timerText.text = $"{s / 60:00}:{s % 60:00}";       // 기획서 p.18: 10:00 형식
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
            fixLabel.text = $"오류 수정 {done}/{total}";
        }
    }

    // ---------- 기획서 p.7 HUD: 상단 제목/스테이지/오류 수정, 좌하단 보유 자료, 하단 조작 안내 ----------
    void BuildDesignLabels()
    {
        var canvas = timerText != null ? timerText.canvas : GetComponentInParent<Canvas>();
        if (canvas == null) return;
        var root = canvas.transform;

        Label(root, "청강 디버그 러시", new Vector2(-790f, 475f), new Vector2(300f, 60f), 30f, TextAlignmentOptions.Left);
        stageLabel = Label(root, "", new Vector2(520f, 475f), new Vector2(260f, 60f), 30f, TextAlignmentOptions.Right);
        fixLabel = Label(root, "", new Vector2(790f, 475f), new Vector2(260f, 60f), 30f, TextAlignmentOptions.Right);
        Label(root, "보유 자료", new Vector2(-770f, -385f), new Vector2(300f, 40f), 24f, TextAlignmentOptions.Center);
        Label(root, "WASD 이동  /  E 제작·회수·수정  /  Space 점프  /  Q 버리기  /  ESC 일시정지",
              new Vector2(0f, -510f), new Vector2(1200f, 40f), 22f, TextAlignmentOptions.Center);
    }

    static TMP_Text Label(Transform parent, string text, Vector2 pos, Vector2 size, float fontSize, TextAlignmentOptions align)
    {
        var go = new GameObject("HUD_" + text, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        var t = go.GetComponent<TextMeshProUGUI>();
        t.font = KoreanTMPFont.Get();
        t.text = text;
        t.fontSize = fontSize;
        t.alignment = align;
        t.color = Color.white;
        t.raycastTarget = false;
        return t;
    }

    void OnHp(int hp, int max)
    {
        for (int i = 0; i < hearts.Length; i++)
        {
            hearts[i].gameObject.SetActive(i < max);
            hearts[i].color = i < hp ? Color.white : new Color(1, 1, 1, 0.25f);
        }
    }

    void RefreshHeld()
    {
        var items = player.Inventory.items;
        EnsureItemLabels();
        for (int i = 0; i < heldSlots.Length; i++)
        {
            bool has = i < items.Count;
            heldSlots[i].enabled = has;
            var label = heldSlots[i].transform.Find("ItemNameLabel")?.GetComponent<TMP_Text>();
            if (has) heldSlots[i].sprite = icons.Get(items[i]);
            if (label != null) label.text = has ? ItemName(items[i]) : "";
        }
    }

    void EnsureItemLabels()
    {
        foreach (var slot in heldSlots)
        {
            if (slot == null || slot.transform.Find("ItemNameLabel") != null) continue;
            var go = new GameObject("ItemNameLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(slot.transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f); rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 1f); rect.anchoredPosition = new Vector2(0f, -4f);
            rect.sizeDelta = new Vector2(150f, 28f);
            var text = go.GetComponent<TextMeshProUGUI>(); text.font = KoreanTMPFont.Get(); text.fontSize = 14f;
            text.alignment = TextAlignmentOptions.Center; text.color = Color.white; text.raycastTarget = false;
        }
    }
    string ItemName(ItemType item)
    {
        switch (item)
        {
            case ItemType.Planning: return "기획";
            case ItemType.Programming: return "프로그래밍";
            case ItemType.Graphic: return "그래픽";
            case ItemType.QA: return "QA";
            // 기획서 p.10 표 명칭
            case ItemType.ContentDoc: return "컨텐츠 기획서";
            case ItemType.SystemDoc: return "시스템 기획서";
            case ItemType.CharacterCode: return "캐릭터 코드";
            case ItemType.CodeFix: return "코드 수정본";
            case ItemType.GraphicFix: return "그래픽 수정본";
            case ItemType.GameBuild: return "게임 빌딩";
            case ItemType.ArtFinal: return "아트 작업물 완성본";
            default: return "오류뭉텅이";
        }
    }
    void OnEnded(bool cleared)
    {
        resultPanel.SetActive(true);
        resultText.text = cleared ? "STAGE CLEAR!" : "FAILED...";
    }
}
