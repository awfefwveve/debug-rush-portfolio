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
        var stages = GameManager.I.allStages.Where(s => s != null && s.chapter == chapter).ToArray();
        int last = Mathf.Max(StageManager.StagesPerChapter, stages.Length > 0 ? stages.Max(s => s.stage) : 0);
        // 챕터당 10칸을 모두 보여주고, 등록 안 된 스테이지는 "준비 중"으로 잠금
        for (int n = 1; n <= last; n++)
        {
            var s = stages.FirstOrDefault(x => x.stage == n);
            var card = Instantiate(cardPrefab, content);
            if (s != null) card.Setup(s);
            else card.SetupPlaceholder(chapter, n);
        }
    }
}
