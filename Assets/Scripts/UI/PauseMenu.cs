using UnityEngine;

/// <summary>ESC 일시정지 메뉴와 설정창.</summary>
public class PauseMenu : MonoBehaviour
{
    bool open, settings;
    float previousTimeScale = 1f;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (settings) settings = false; else SetOpen(!open);
        }
    }
    void SetOpen(bool value)
    {
        open = value;
        if (value) { previousTimeScale = Time.timeScale; Time.timeScale = 0f; }
        else Time.timeScale = previousTimeScale;
    }
    void OnGUI()
    {
        KoreanIMGUIFont.Apply();
        if (!open) return;
        if (settings)
        {
            if (SettingsWindow.Draw()) settings = false;
            return;
        }
        SettingsWindow.Dim();
        float s = Screen.height / 1080f;
        var r = UiArt.Window("일시정지", 900f, 532f);
        // 창 그림(setting.png)의 밝은 칸 안: 가로 5%~93%, 세로 18%~97% → 그 가운데에 버튼 3개를 세로로
        float cx = r.x + r.width * 0.49f, top = r.y + r.height * 0.18f, bottom = r.y + r.height * 0.97f;
        float bw = r.width * 0.42f, bh = bw * 0.22f, step = bh * 1.35f;
        float x = cx - bw * 0.5f, y = (top + bottom) * 0.5f - (bh + step * 2f) * 0.5f, gap = step;
        if (UiArt.Button(new Rect(x, y, bw, bh), "계속하기", 24f * s)) SetOpen(false);
        if (UiArt.Button(new Rect(x, y + gap, bw, bh), "설정", 24f * s)) settings = true;
        if (UiArt.Button(new Rect(x, y + gap * 2f, bw, bh), "타이틀로 나가기", 24f * s)) { Time.timeScale = previousTimeScale; UnityEngine.SceneManagement.SceneManager.LoadScene("Title"); }
    }
    void OnDestroy() { if (open) Time.timeScale = previousTimeScale; }
}
