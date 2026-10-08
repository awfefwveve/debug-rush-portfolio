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

    void Start()
    {
        player = stage.player;
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
        timerText.text = $"{s / 60}:{s % 60:00}";
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
            case ItemType.ContentDoc: return "기획 문서";
            case ItemType.SystemDoc: return "시스템 문서";
            case ItemType.CharacterCode: return "캐릭터 코드";
            case ItemType.CodeFix: return "코드 수정";
            case ItemType.GraphicFix: return "그래픽 수정";
            case ItemType.GameBuild: return "게임 빌드";
            case ItemType.ArtFinal: return "최종 아트";
            default: return "오류 덩어리";
        }
    }
    void OnEnded(bool cleared)
    {
        resultPanel.SetActive(true);
        resultText.text = cleared ? "STAGE CLEAR!" : "FAILED...";
    }
}
