using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StageCard : MonoBehaviour
{
    public TMP_Text title, summary;
    public GameObject lockOverlay;
    public Button[] difficultyButtons; // 기존 프리팹 호환용. UI에는 입장 버튼 하나만 표시합니다.

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
            if (label != null) label.text = "입장";
            var rect = button.transform as RectTransform;
            if (rect != null) rect.anchoredPosition = new Vector2(0f, -120f);
            button.interactable = unlocked;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => GameManager.I.StartStage(s, Difficulty.Normal));
        }
    }
}
