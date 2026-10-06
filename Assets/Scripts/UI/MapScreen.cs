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
    public TMP_Dropdown chapterDropdown;       // 1학년 / 2학년 / 3학년
    public Button backButton;

    void Start()
    {
        chapterDropdown.onValueChanged.AddListener(i => Build(i + 1));
        backButton.onClick.AddListener(() => SceneManager.LoadScene("Title"));
        Build(1);
    }

    void Build(int chapter)
    {
        foreach (Transform c in content) Destroy(c.gameObject);

        var stages = GameManager.I.allStages
            .Where(s => s.chapter == chapter).OrderBy(s => s.stage);
        foreach (var s in stages)
            Instantiate(cardPrefab, content).Setup(s);
    }
}
