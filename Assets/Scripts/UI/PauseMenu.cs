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
        var r = UiArt.Window("일시정지", 760f, 450f);
        float bw = r.width * 0.46f, bh = bw * 0.2f, x = r.center.x - bw * 0.5f, y = r.y + r.height * 0.26f, gap = bh * 1.25f;
        if (UiArt.Button(new Rect(x, y, bw, bh), "계속하기", 28f * s)) SetOpen(false);
        if (UiArt.Button(new Rect(x, y + gap, bw, bh), "설정", 28f * s)) settings = true;
        if (UiArt.Button(new Rect(x, y + gap * 2f, bw, bh), "타이틀로 나가기", 28f * s)) { Time.timeScale = previousTimeScale; UnityEngine.SceneManagement.SceneManager.LoadScene("Title"); }
    }
    void OnDestroy() { if (open) Time.timeScale = previousTimeScale; }
}
