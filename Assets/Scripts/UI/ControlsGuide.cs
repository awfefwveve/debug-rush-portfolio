using System.Collections;
using UnityEngine;

/// <summary>1-1 시작 대사 뒤 조작 안내 한 장. 아무 키(또는 클릭)나 누르면 닫힘. 한 번 보면 안 나오고, 진행도 리셋하면 다시 나옴.</summary>
public class ControlsGuide : MonoBehaviour
{
    public const string SeenKey = "tutorial_controls_seen";

    static readonly string[][] Rows =
    {
        new[] { "WASD / 방향키", "이동" },
        new[] { "Shift", "대쉬" },
        new[] { "Space", "점프 (탄에 맞춰 뛰면 패링)" },
        new[] { "E", "재료 집기 · 제작 · 결과물 회수 · 단말기 수정" },
        new[] { "Q", "들고 있는 자료 버리기" },
        new[] { "Tab", "조합표 보기" },
        new[] { "ESC", "일시정지" },
    };

    bool done;
    float openedAt;

    public static bool ShouldShow(StageData s) => s != null && s.chapter == 1 && s.stage == 1 && PlayerPrefs.GetInt(SeenKey, 0) == 0
        && FindAnyObjectByType<AutoShot>() == null;   // 자동 캡처 중엔 안 띄움

    public static IEnumerator Show()
    {
        var g = new GameObject("ControlsGuide").AddComponent<ControlsGuide>();
        g.openedAt = Time.unscaledTime;
        while (!g.done) yield return null;
        PlayerPrefs.SetInt(SeenKey, 1); PlayerPrefs.Save();
        Destroy(g.gameObject);
    }

    void Update()
    {
        // 대사 넘기던 키가 바로 닫지 않게 잠깐 뒤부터
        if (Time.unscaledTime - openedAt > 0.4f && (Input.anyKeyDown || Input.GetMouseButtonDown(0))) done = true;
    }

    void OnGUI()
    {
        KoreanIMGUIFont.Apply();
        SettingsWindow.Dim();
        float s = Screen.height / 1080f;
        var r = UiArt.Window("조작 방법", 1100f, 650f);
        var ink = new Color(0.12f, 0.13f, 0.18f);
        float nameW = r.width * 0.15f, nameH = nameW * 80f / 224f, rowH = nameH * 1.08f;
        float x = r.x + r.width * 0.12f, y = r.y + r.height * 0.19f;
        for (int i = 0; i < Rows.Length; i++)
        {
            var tag = new Rect(x, y + i * rowH, nameW, nameH);
            if (UiArt.Tex("name") != null) UiArt.Draw(tag, "name"); else GUI.Box(tag, "");
            UiArt.Label(new Rect(tag.x, tag.y, tag.width * 0.87f, tag.height), Rows[i][0], 17f * s, UiArt.Ink, TextAnchor.MiddleCenter, true);
            UiArt.Label(new Rect(tag.xMax + 24f * s, tag.y, r.width * 0.55f, tag.height), Rows[i][1], 24f * s, ink, TextAnchor.MiddleLeft, true);
        }
        UiArt.Label(new Rect(r.x, r.yMax - r.height * 0.12f, r.width * 0.93f, r.height * 0.08f), "아무 키나 누르면 시작해요", 24f * s, UiArt.Pink, TextAnchor.MiddleRight, true);
    }
}
