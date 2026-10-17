using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>시작화면 (기획서 p.16): 일러스트 + 우하단 버튼 시작 / 설정 / 업적 / 나가기</summary>
public class MainMenu : MonoBehaviour
{
    public GameObject settingsPanel;
    [Header("일러스트 (아트 팀 그림)")]
    public Sprite heroSprite;
    public Sprite monsterSprite;
    [Tooltip("타이틀 로고 1024×512 (팀 규격, 사방 32px 여백)")] public Sprite logoSprite;

    RectTransform monster;

    void Awake() => SoundEffectManager.EnsureInstance();

    void Start()
    {
        if (settingsPanel != null)
        {
            // 설정창은 SettingsWindow가 그림 → 예전 패널은 화면을 어둡게 하는 배경으로만 사용
            foreach (Transform c in settingsPanel.transform) c.gameObject.SetActive(false);
            var r = (RectTransform)settingsPanel.transform;
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
            var img = settingsPanel.GetComponent<Image>();
            if (img != null) img.color = new Color(0f, 0f, 0f, 0.6f);
            settingsPanel.transform.SetAsLastSibling();
        }
        BuildIllustration();
    }

    void BuildIllustration()
    {
        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;
        var root = canvas.transform;
        var bg = root.Find("Background");
        int index = bg != null ? bg.GetSiblingIndex() + 1 : 0;

        // 바닥 그리드 느낌의 띠
        var glow = Img(root, "Glow", null, new Vector2(0f, -470f), new Vector2(1920f, 6f), new Color(0.22f, 0.82f, 1f, 0.5f));
        glow.transform.SetSiblingIndex(index);
        if (monsterSprite != null)
        {
            var m = Img(root, "IllustMonster", monsterSprite, new Vector2(300f, -60f), new Vector2(440f, 440f), Color.white);
            m.transform.SetSiblingIndex(index + 1);
            monster = m.rectTransform;
        }
        if (heroSprite != null)
        {
            var h = Img(root, "IllustHero", heroSprite, new Vector2(-560f, -260f), new Vector2(600f, 600f), Color.white);
            h.transform.SetSiblingIndex(index + 2);
        }

        var title = root.Find("Title")?.GetComponent<TMP_Text>();
        var subtitle = root.Find("Subtitle")?.GetComponent<TMP_Text>();
        if (logoSprite != null)
        {
            // 팀 로고가 있으면 글자 제목 대신 로고 (규격 1024×512 그대로, 상단 왼쪽~가운데)
            Img(root, "Logo", logoSprite, new Vector2(-380f, 290f), new Vector2(880f, 440f), Color.white);
            if (title != null) title.gameObject.SetActive(false);
            if (subtitle != null) { var r = subtitle.rectTransform; r.anchoredPosition = new Vector2(-380f, 55f); subtitle.fontSize = 30f; subtitle.color = new Color(0.85f, 0.9f, 1f); }
        }
        else if (title != null) { title.color = new Color(0.22f, 0.82f, 1f); title.fontStyle = FontStyles.Bold; title.outlineWidth = 0.15f; title.outlineColor = new Color32(255, 79, 163, 255); }
    }

    void Update()
    {
        if (monster != null) monster.anchoredPosition = new Vector2(300f, -60f + Mathf.Sin(Time.time * 2.2f) * 18f);
    }

    static Image Img(Transform parent, string name, Sprite sprite, Vector2 pos, Vector2 size, Color color)
    {
        var img = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        img.transform.SetParent(parent, false);
        var r = img.rectTransform; r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos; r.sizeDelta = size;
        img.sprite = sprite; img.color = color; img.preserveAspect = true; img.raycastTarget = false;
        return img;
    }

    public void OnStart() { if (!confirmReset) SceneManager.LoadScene("Map"); }
    public void OnAchievements() { if (!confirmReset) SceneManager.LoadScene("Achievements"); }
    public void OnSettings() { if (settingsPanel && !confirmReset) settingsPanel.SetActive(!settingsPanel.activeSelf); }
    public void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    // 진행도 초기화 (시연용: 여러 사람이 연달아 처음부터 플레이)
    bool confirmReset;
    float resetDoneUntil;

    void OnGUI()
    {
        KoreanIMGUIFont.Apply();
        if (settingsPanel && settingsPanel.activeSelf)
        {
            if (SettingsWindow.Draw()) settingsPanel.SetActive(false);
            return;
        }

        float s = Screen.height / 1080f;
        var btn = new Rect(24f * s, Screen.height - 70f * s, 200f * s, 46f * s);
        var style = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(20 * s) };
        if (!confirmReset && GUI.Button(btn, "진행도 초기화", style)) confirmReset = true;
        if (Time.unscaledTime < resetDoneUntil)
            GUI.Label(new Rect(btn.xMax + 16f * s, btn.y, 400f * s, btn.height), "초기화했어요. 처음부터 플레이할 수 있어요.", new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(20 * s), alignment = TextAnchor.MiddleLeft, normal = { textColor = new Color(0.4f, 0.95f, 0.6f) } });

        if (!confirmReset) return;
        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
        float w = 460f * s, h = 220f * s;
        var r = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
        GUI.Box(r, "");
        GUI.Box(r, "");
        var big = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(26 * s), alignment = TextAnchor.MiddleCenter, wordWrap = true, richText = true };
        string sub = "<size=" + Mathf.RoundToInt(18 * s) + ">스테이지 클리어·업적이 지워져요 (설정은 유지)</size>";
        GUI.Label(new Rect(r.x + 20f * s, r.y + 24f * s, w - 40f * s, 90f * s), "진행도를 모두 초기화할까요?\n" + sub, big);
        var b1 = new Rect(r.x + 40f * s, r.yMax - 76f * s, (w - 100f * s) * 0.5f, 50f * s);
        var b2 = new Rect(b1.xMax + 20f * s, b1.y, b1.width, b1.height);
        if (GUI.Button(b1, "초기화", style)) { GameSettings.ResetProgress(); confirmReset = false; resetDoneUntil = Time.unscaledTime + 3f; }
        if (GUI.Button(b2, "취소", style)) confirmReset = false;
    }
}
