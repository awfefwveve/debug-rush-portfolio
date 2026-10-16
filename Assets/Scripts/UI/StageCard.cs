using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StageCard : MonoBehaviour
{
    public TMP_Text title, summary;
    public GameObject lockOverlay;
    public Button[] difficultyButtons; // 기존 프리팹 호환용. UI에는 플레이 버튼 하나만 표시합니다.
    static readonly Color PlayRed = new Color(0.8f, 0.18f, 0.18f, 1f);

    public void Setup(StageData s)
    {
        bool unlocked = GameManager.I.IsUnlocked(s);
        title.text = s.displayName;
        summary.text = unlocked ? s.summary : "";
        lockOverlay.SetActive(!unlocked);

        if (difficultyButtons == null || difficultyButtons.Length == 0) return;
        for (int i = 0; i < difficultyButtons.Length; i++)
        {
            var button = difficultyButtons[i];
            if (button == null) continue;
            bool isEntry = i == 0;
            button.gameObject.SetActive(isEntry);
            if (!isEntry) continue;

            var label = button.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = "플레이";
            if (button.image != null) button.image.color = PlayRed;   // 기획서 p.17: 빨간 플레이 버튼
            var rect = button.transform as RectTransform;
            if (rect != null) rect.anchoredPosition = new Vector2(0f, -120f);
            button.interactable = unlocked;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => GameManager.I.StartStage(s, Difficulty.Normal));
        }
    }

    /// <summary>아직 기획되지 않은 스테이지 슬롯: 잠금 + "준비 중"</summary>
    public void SetupPlaceholder(int chapter, int stage)
    {
        title.text = $"{chapter}-{stage}";
        summary.text = "준비 중";
        lockOverlay.SetActive(true);

        if (difficultyButtons == null) return;
        for (int i = 0; i < difficultyButtons.Length; i++)
        {
            var button = difficultyButtons[i];
            if (button == null) continue;
            button.gameObject.SetActive(i == 0);
            button.interactable = false;
            button.onClick.RemoveAllListeners();
            var label = button.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = "준비 중";
        }
    }
}
