using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AchievementScreen : MonoBehaviour
{
    public AchievementData[] data;
    public TMP_Text listText, detailText;

    public void Show(int index)
    {
        if (data == null || index < 0 || index >= data.Length) return;
        var item = data[index];
        detailText.text = item.title + "\n\n" + AchievementManager.DescriptionFor(item);
    }

    public void BackToTitle() => SceneManager.LoadScene("Title");

    void Start()
    {
        if (listText && data != null)
            listText.text = string.Join("\n\n", System.Array.ConvertAll(data, a => "• " + a.title));
    }
}
