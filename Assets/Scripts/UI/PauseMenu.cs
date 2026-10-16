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
        float w = 360, h = 250;
        var r = new Rect((Screen.width-w)*.5f, (Screen.height-h)*.5f, w, h);
        GUI.Box(r, "일시정지");
        GUILayout.BeginArea(new Rect(r.x+28, r.y+38, w-56, h-50));
        if (GUILayout.Button("계속하기")) SetOpen(false);
        if (GUILayout.Button("설정")) settings = true;
        if (GUILayout.Button("타이틀로 나가기")) { Time.timeScale = previousTimeScale; UnityEngine.SceneManagement.SceneManager.LoadScene("Title"); }
        GUILayout.EndArea();
    }
    void OnDestroy() { if (open) Time.timeScale = previousTimeScale; }
}
