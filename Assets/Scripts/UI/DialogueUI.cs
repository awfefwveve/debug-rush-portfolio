using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>대화형 스토리. 클릭/Enter로 진행, 건너뛰기 버튼으로 전체 건너뛰기.</summary>
public class DialogueUI : MonoBehaviour
{
    public GameObject root;
    public TMP_Text speakerText, bodyText;
    public Image portrait;
    public Button skipButton;
    bool skipAll, bound;

    void Awake() { Bind(); }
    public void Initialize(GameObject panel, TMP_Text speaker, TMP_Text body, Image face, Button skip)
    {
        root = panel; speakerText = speaker; bodyText = body; portrait = face; skipButton = skip;
        Bind();
    }
    void Bind()
    {
        if (root) root.SetActive(false);
        if (!bound && skipButton != null) { skipButton.onClick.AddListener(() => skipAll = true); bound = true; }
    }
    public static DialogueUI CreateRuntime()
    {
        var canvasObject = new GameObject("DialogueCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
        var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080);
        var panel = new GameObject("DialoguePanel", typeof(RectTransform), typeof(Image)); panel.transform.SetParent(canvasObject.transform, false);
        var panelRect = panel.GetComponent<RectTransform>(); panelRect.anchorMin = new Vector2(0.08f, 0.04f); panelRect.anchorMax = new Vector2(0.92f, 0.34f); panelRect.offsetMin = Vector2.zero; panelRect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0.035f, 0.055f, 0.09f, 0.96f);
        var portraitObject = new GameObject("Portrait", typeof(RectTransform), typeof(Image)); portraitObject.transform.SetParent(panel.transform, false);
        var portraitRect = portraitObject.GetComponent<RectTransform>(); portraitRect.anchorMin = new Vector2(0.02f, 0.12f); portraitRect.anchorMax = new Vector2(0.2f, 0.88f); portraitRect.offsetMin = Vector2.zero; portraitRect.offsetMax = Vector2.zero;
        var face = portraitObject.GetComponent<Image>(); face.preserveAspect = true; face.enabled = false;
        var speaker = MakeText("Speaker", panel.transform, new Vector2(0.24f, 0.66f), new Vector2(0.96f, 0.91f), 30, TextAlignmentOptions.TopLeft);
        var body = MakeText("Dialogue", panel.transform, new Vector2(0.24f, 0.18f), new Vector2(0.96f, 0.65f), 25, TextAlignmentOptions.TopLeft);
        var buttonObject = new GameObject("Skip", typeof(RectTransform), typeof(Image), typeof(Button)); buttonObject.transform.SetParent(panel.transform, false);
        var br = buttonObject.GetComponent<RectTransform>(); br.anchorMin = new Vector2(0.78f, 0.02f); br.anchorMax = new Vector2(0.97f, 0.18f); br.offsetMin = Vector2.zero; br.offsetMax = Vector2.zero;
        buttonObject.GetComponent<Image>().color = new Color(0.18f, 0.28f, 0.42f, 1f);
        var buttonLabel = MakeText("Label", buttonObject.transform, Vector2.zero, Vector2.one, 18, TextAlignmentOptions.Center); buttonLabel.text = "건너뛰기";
        var ui = canvasObject.AddComponent<DialogueUI>(); ui.Initialize(panel, speaker, body, face, buttonObject.GetComponent<Button>());
        return ui;
    }
    static TMP_Text MakeText(string name, Transform parent, Vector2 min, Vector2 max, float size, TextAlignmentOptions alignment)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>(); rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        var text = go.GetComponent<TextMeshProUGUI>(); text.font = KoreanTMPFont.Get(); text.fontSize = size; text.color = Color.white; text.alignment = alignment; text.raycastTarget = false;
        return text;
    }
    public IEnumerator Play(DialogueLine[] lines)
    {
        if (lines == null || lines.Length == 0) yield break;
        root.SetActive(true); skipAll = false;
        foreach (var line in lines)
        {
            if (skipAll) break;
            speakerText.text = line.speaker; bodyText.text = line.text;
            // 초상화: 대사에 지정된 게 없으면 "주인공"(나)은 기본 초상화 (Resources/Portraits/player)
            var face = line.portrait != null ? line.portrait : (line.speaker == "나" || line.speaker == "주인공" ? Resources.Load<Sprite>("Portraits/player") : null);
            portrait.enabled = face != null; portrait.sprite = face;
            yield return null;
            while (!skipAll && !Input.GetMouseButtonDown(0) && !Input.GetKeyDown(KeyCode.Return)) yield return null;
        }
        root.SetActive(false);
    }
}
