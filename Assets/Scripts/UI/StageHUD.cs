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
        for (int i = 0; i < heldSlots.Length; i++)
        {
            bool has = i < items.Count;
            heldSlots[i].enabled = has;
            if (has) heldSlots[i].sprite = icons.Get(items[i]);
        }
    }

    void OnEnded(bool cleared)
    {
        resultPanel.SetActive(true);
        resultText.text = cleared ? "STAGE CLEAR!" : "FAILED...";
    }
}
