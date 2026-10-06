using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StageCard : MonoBehaviour
{
    public TMP_Text title, summary;
    public GameObject lockOverlay;
    public Button[] difficultyButtons;         // 쉬움, 보통, 어려움 (3개)

    public void Setup(StageData s)
    {
        bool unlocked = GameManager.I.IsUnlocked(s);
        title.text = s.displayName;
        summary.text = unlocked ? s.summary : "";
        lockOverlay.SetActive(!unlocked);

        for (int i = 0; i < difficultyButtons.Length; i++)
        {
            var d = (Difficulty)i;
            difficultyButtons[i].interactable = unlocked;
            difficultyButtons[i].onClick.AddListener(() => GameManager.I.StartStage(s, d));
        }
    }
}
