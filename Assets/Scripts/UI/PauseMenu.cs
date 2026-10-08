using UnityEngine;

/// <summary>ESC 일시정지 메뉴와 기본 그래픽/음량 설정.</summary>
public class PauseMenu : MonoBehaviour
{
    bool open, settings;
    float previousTimeScale = 1f, volume;
    int quality;
    void Awake() { volume = PlayerPrefs.GetFloat("settings_volume", AudioListener.volume); AudioListener.volume = volume; quality = Mathf.Clamp(PlayerPrefs.GetInt("settings_quality", QualitySettings.GetQualityLevel()), 0, QualitySettings.names.Length - 1); QualitySettings.SetQualityLevel(quality); Screen.fullScreen = PlayerPrefs.GetInt("settings_fullscreen", Screen.fullScreen ? 1 : 0) == 1; }
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
        float w = 360, h = settings ? 330 : 250;
        var r = new Rect((Screen.width-w)*.5f, (Screen.height-h)*.5f, w, h);
        GUI.Box(r, settings ? "설정" : "일시정지");
        GUILayout.BeginArea(new Rect(r.x+28, r.y+38, w-56, h-50));
        if (settings)
        {
            GUILayout.Label("전체 음량  " + Mathf.RoundToInt(volume*100) + "%");
            float newVolume = GUILayout.HorizontalSlider(volume, 0f, 1f);
            if (!Mathf.Approximately(newVolume, volume)) { volume = newVolume; AudioListener.volume = volume; PlayerPrefs.SetFloat("settings_volume", volume); PlayerPrefs.Save(); }
            GUILayout.Space(10); GUILayout.Label("화면 모드");
            if (GUILayout.Button(Screen.fullScreen ? "전체 화면 (클릭하여 창 모드)" : "창 모드 (클릭하여 전체 화면)")) { Screen.fullScreen = !Screen.fullScreen; PlayerPrefs.SetInt("settings_fullscreen", Screen.fullScreen ? 1 : 0); PlayerPrefs.Save(); }
            GUILayout.Space(10); GUILayout.Label("그래픽 품질: " + QualitySettings.names[quality]);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("낮음")) SetQuality(0);
            if (GUILayout.Button("보통")) SetQuality(Mathf.Min(1, QualitySettings.names.Length-1));
            if (GUILayout.Button("높음")) SetQuality(QualitySettings.names.Length-1);
            GUILayout.EndHorizontal(); GUILayout.FlexibleSpace();
            if (GUILayout.Button("돌아가기")) settings = false;
        }
        else
        {
            if (GUILayout.Button("계속하기")) SetOpen(false);
            if (GUILayout.Button("설정")) settings = true;
            if (GUILayout.Button("타이틀로 나가기")) { Time.timeScale = previousTimeScale; UnityEngine.SceneManagement.SceneManager.LoadScene("Title"); }
        }
        GUILayout.EndArea();
    }
    void SetQuality(int i) { quality = Mathf.Clamp(i, 0, QualitySettings.names.Length-1); QualitySettings.SetQualityLevel(quality); PlayerPrefs.SetInt("settings_quality", quality); PlayerPrefs.Save(); }
    void OnDestroy() { if (open) Time.timeScale = previousTimeScale; }
}
