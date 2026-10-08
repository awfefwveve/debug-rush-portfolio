using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>대화형 스토리. 클릭/Enter로 진행, SKIP 버튼으로 전체 건너뛰기.</summary>
public class DialogueUI : MonoBehaviour
{
    public GameObject root;
    public TMP_Text speakerText, bodyText;
    public Image portrait;
    public Button skipButton;

    bool skipAll;

    void Awake()
    {
        if (root) root.SetActive(false);
        if (skipButton) skipButton.onClick.AddListener(() => skipAll = true);
    }

    public IEnumerator Play(DialogueLine[] lines)
    {
        if (lines == null || lines.Length == 0) yield break;
        root.SetActive(true);
        skipAll = false;

        foreach (var l in lines)
        {
            if (skipAll) break;
            speakerText.text = l.speaker;
            bodyText.text = l.text;
            portrait.enabled = l.portrait != null;
            portrait.sprite = l.portrait;

            yield return null; // 같은 프레임 입력 방지
            while (!skipAll && !Input.GetMouseButtonDown(0) && !Input.GetKeyDown(KeyCode.Return))
                yield return null;
        }
        root.SetActive(false);
    }
}
