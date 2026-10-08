using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AchievementScreen : MonoBehaviour
{
    public AchievementData[] data;
    public TMP_Text listText, detailText;
    int selected;
    public void Show(int index)
    {
        if (data == null || index < 0 || index >= data.Length) return;
        selected = index;
        var a = data[index];
        bool unlocked = AchievementManager.IsUnlocked(a.id);
        detailText.text = (unlocked ? "획득 완료" : "미획득") + "\n\n" + a.title + "\n\n" + AchievementManager.DescriptionFor(a);
        RefreshList();
    }
    public void BackToTitle() => SceneManager.LoadScene("Title");
    void Start() { if (data != null && data.Length > 0) Show(0); else RefreshList(); }
    void Update()
    {
        if (data == null || data.Length == 0) return;
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) Show((selected + 1) % data.Length);
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) Show((selected - 1 + data.Length) % data.Length);
    }
    void RefreshList()
    {
        if (!listText || data == null) return;
        var lines = new string[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            bool got = AchievementManager.IsUnlocked(data[i].id);
            lines[i] = (i == selected ? "▶ " : "　") + (got ? "[획득] " : "[미획득] ") + data[i].title;
        }
        listText.text = string.Join("\n\n", lines) + "\n\n↑↓ 선택";
    }
}
