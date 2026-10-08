using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>맵 화면: 챕터(1~3학년) 드롭다운 + 스테이지 카드 목록</summary>
public class MapScreen : MonoBehaviour
{
    public StageCard cardPrefab;
    public Transform content;
    public TMP_Dropdown chapterDropdown;
    public Button backButton;
    void Start()
    {
        chapterDropdown.onValueChanged.AddListener(i => Build(i + 1));
        backButton.onClick.AddListener(() => SceneManager.LoadScene("Title"));
        Build(chapterDropdown != null ? chapterDropdown.value + 1 : 1);
    }
    void Build(int chapter)
    {
        foreach (Transform c in content) Destroy(c.gameObject);
        var stages = GameManager.I.allStages.Where(s => s.chapter == chapter).OrderBy(s => s.stage).ToArray();
        foreach (var s in stages) Instantiate(cardPrefab, content).Setup(s);
        if (stages.Length == 0)
        {
            var empty = new GameObject("NoStagesMessage", typeof(RectTransform), typeof(TextMeshProUGUI));
            empty.transform.SetParent(content, false);
            var label = empty.GetComponent<TextMeshProUGUI>();
            label.text = $"{chapter}학년 스테이지는 아직 등록되지 않았습니다.";
            label.font = KoreanTMPFont.Get(); label.fontSize = 28; label.alignment = TextAlignmentOptions.Center;
        }
    }
}
