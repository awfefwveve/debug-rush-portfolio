using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 업적 화면 (기획서 p.15): 좌측 4x4 아이콘 그리드 + 우측 상세 + 돌아가기.
/// 획득 = 밝은 아이콘, 미획득 = 어두운 아이콘, 비밀(미획득) = 검정 아이콘 + 설명 숨김.
/// </summary>
public class AchievementScreen : MonoBehaviour
{
    public AchievementData[] data;
    public TMP_Text listText, detailText;

    [Header("그리드")]
    public int columns = 4, rows = 4;
    public float spacing = 12f;
    public Color frameColor = new Color(0.15f, 0.15f, 0.2f, 0.9f);
    public Color selectedColor = new Color(1f, 0.85f, 0.2f, 1f);
    public Color lockedTint = new Color(0.3f, 0.3f, 0.3f, 1f);

    int selected;
    Image[] frames, iconImages;
    Image detailIcon;

    public void BackToTitle() => SceneManager.LoadScene("Title");

    void Start()
    {
        StyleBack();
        BuildGrid();
        BuildDetailIcon();
        if (data != null && data.Length > 0) Show(0); else Refresh();
    }

    public void Show(int index)
    {
        if (data == null || index < 0 || index >= data.Length) return;
        selected = index;
        var a = data[index];
        bool unlocked = AchievementManager.IsUnlocked(a.id);
        if (detailText) detailText.text = a.title + "\n\n" + AchievementManager.DescriptionFor(a) + "\n\n" + (unlocked ? "획득함" : "미획득");
        if (detailIcon)
        {
            detailIcon.sprite = IconSprite(a, unlocked);
            detailIcon.color = IconTint(a, unlocked);
            detailIcon.enabled = true;
        }
        Refresh();
    }

    void Update()
    {
        if (data == null || data.Length == 0) return;
        int n = data.Length;
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) Show((selected + 1) % n);
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) Show((selected - 1 + n) % n);
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) Show(Mathf.Min(n - 1, selected + columns));
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) Show(Mathf.Max(0, selected - columns));
        if (Input.GetKeyDown(KeyCode.Escape)) BackToTitle();
    }

    Color IconColor(AchievementData a, bool unlocked)
    {
        if (unlocked) return Color.white;
        return a.secret ? Color.black : lockedTint;
    }

    // 미획득은 자물쇠 그림 (아트 팀 lock.png), 없으면 어둡게 칠한 아이콘
    Sprite IconSprite(AchievementData a, bool unlocked) => !unlocked && UiArt.Get("lock") != null ? UiArt.Get("lock") : a.icon;
    Color IconTint(AchievementData a, bool unlocked) => !unlocked && UiArt.Get("lock") != null ? Color.white : IconColor(a, unlocked);

    void Refresh()
    {
        if (frames == null) return;
        for (int i = 0; i < frames.Length; i++)
        {
            bool has = data != null && i < data.Length && data[i] != null;
            if (UiArt.Get("item") != null) { frames[i].sprite = has && i == selected ? UiArt.Get("item") : UiArt.Get("item_2"); frames[i].type = Image.Type.Sliced; frames[i].color = Color.white; }
            else frames[i].color = has && i == selected ? selectedColor : frameColor;
            iconImages[i].enabled = has;
            if (!has) continue;
            bool got = AchievementManager.IsUnlocked(data[i].id);
            iconImages[i].sprite = IconSprite(data[i], got);
            iconImages[i].color = IconTint(data[i], got);
        }
    }

    void BuildGrid()
    {
        if (listText == null) return;
        var area = listText.rectTransform;
        listText.text = "";
        foreach (Transform child in area) child.gameObject.SetActive(false);   // 예전 목록 항목 숨김
        if (area.parent != null)
            foreach (Transform sibling in area.parent)
                if (sibling.name.StartsWith("Achievement_")) sibling.gameObject.SetActive(false);

        int count = Mathf.Max(columns * rows, data != null ? data.Length : 0);
        float cell = Mathf.Min((area.rect.width - spacing * (columns - 1)) / columns,
                               (area.rect.height - spacing * (rows - 1)) / rows);
        frames = new Image[count];
        iconImages = new Image[count];

        for (int i = 0; i < count; i++)
        {
            int col = i % columns, row = i / columns;
            var frameGo = new GameObject("Cell" + i, typeof(RectTransform), typeof(Image), typeof(Button));
            frameGo.transform.SetParent(area, false);
            var r = frameGo.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0f, 1f);
            r.sizeDelta = new Vector2(cell, cell);
            r.anchoredPosition = new Vector2(col * (cell + spacing), -row * (cell + spacing));
            frames[i] = frameGo.GetComponent<Image>();
            int idx = i;
            frameGo.GetComponent<Button>().onClick.AddListener(() => Show(idx));

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(frameGo.transform, false);
            var ir = iconGo.GetComponent<RectTransform>();
            ir.anchorMin = Vector2.zero; ir.anchorMax = Vector2.one;
            ir.offsetMin = new Vector2(18f, 18f); ir.offsetMax = new Vector2(-18f, -18f);
            iconImages[i] = iconGo.GetComponent<Image>();
            iconImages[i].preserveAspect = true;
            iconImages[i].raycastTarget = false;
        }
    }

    // 시안: 왼쪽 위 "◀ 돌아가기" (back_click 삼각형), 상세 칸 오른쪽 아래 마스코트(icon.png)
    void StyleBack()
    {
        var back = GameObject.Find("Back");
        var arrow = UiArt.Get("back_click");
        if (back != null && arrow != null && back.TryGetComponent(out Button b))
        {
            if (b.image != null) b.image.color = new Color(1f, 1f, 1f, 0f);
            var icon = new GameObject("Arrow", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            icon.transform.SetParent(back.transform, false);
            var r = icon.rectTransform; r.anchorMin = r.anchorMax = new Vector2(0f, 0.5f);
            r.anchoredPosition = new Vector2(30f, 0f); r.sizeDelta = new Vector2(34f, 34f); r.localRotation = Quaternion.Euler(0f, 0f, 90f);
            icon.sprite = arrow; icon.raycastTarget = false; icon.color = new Color(0.8f, 0.82f, 0.88f);
            var label = back.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = "돌아가기"; label.color = new Color(0.8f, 0.82f, 0.88f); label.fontStyle = FontStyles.Bold; label.enableWordWrapping = false;
                label.alignment = TextAlignmentOptions.MidlineLeft;
                var lr = label.rectTransform; lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one; lr.offsetMin = new Vector2(60f, 0f); lr.offsetMax = Vector2.zero;
            }
        }
        var mascot = UiArt.Get("icon");
        if (mascot != null && detailText != null)
        {
            var m = new GameObject("Mascot", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            m.transform.SetParent(detailText.rectTransform, false);
            var r = m.rectTransform; r.anchorMin = r.anchorMax = r.pivot = new Vector2(1f, 0f);
            r.anchoredPosition = new Vector2(10f, -10f); r.sizeDelta = new Vector2(150f, 150f);
            m.sprite = mascot; m.preserveAspect = true; m.raycastTarget = false; m.color = new Color(1f, 1f, 1f, 0.85f);
        }
    }

    void BuildDetailIcon()
    {
        if (detailText == null) return;
        var go = new GameObject("DetailIcon", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(detailText.rectTransform, false);
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f);
        r.pivot = new Vector2(0.5f, 1f);
        r.sizeDelta = new Vector2(130f, 130f);
        r.anchoredPosition = new Vector2(0f, -10f);
        detailIcon = go.GetComponent<Image>();
        detailIcon.preserveAspect = true;
        detailIcon.raycastTarget = false;
        detailIcon.enabled = false;
        detailText.margin = new Vector4(detailText.margin.x, 150f, detailText.margin.z, detailText.margin.w);
    }
}
