using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>시작화면 버튼 연동 (시작 / 설정 / 업적 / 나가기)</summary>
public class MainMenu : MonoBehaviour
{
    public GameObject settingsPanel;
    float volume;
    int quality;
    void Awake() { volume = PlayerPrefs.GetFloat("settings_volume", AudioListener.volume); AudioListener.volume = volume; quality = Mathf.Clamp(PlayerPrefs.GetInt("settings_quality", QualitySettings.GetQualityLevel()), 0, QualitySettings.names.Length - 1); QualitySettings.SetQualityLevel(quality); Screen.fullScreen = PlayerPrefs.GetInt("settings_fullscreen", Screen.fullScreen ? 1 : 0) == 1; }
    public void OnStart() => SceneManager.LoadScene("Map");
    public void OnAchievements() => SceneManager.LoadScene("Achievements");
    public void OnSettings() { if (settingsPanel) settingsPanel.SetActive(!settingsPanel.activeSelf); }
    public void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    void OnGUI()
    {
        KoreanIMGUIFont.Apply();
        if (!settingsPanel || !settingsPanel.activeSelf) return;
        float w = 330, h = 220;
        var r = new Rect((Screen.width-w)*.5f, Screen.height*.58f, w, h);
        GUI.Box(r, "그래픽 / 음량 설정");
        GUILayout.BeginArea(new Rect(r.x+20, r.y+32, w-40, h-42));
        GUILayout.Label("전체 음량  " + Mathf.RoundToInt(volume*100) + "%");
        float newVolume = GUILayout.HorizontalSlider(volume, 0f, 1f);
        if (!Mathf.Approximately(newVolume, volume)) { volume = newVolume; AudioListener.volume = volume; PlayerPrefs.SetFloat("settings_volume", volume); PlayerPrefs.Save(); }
        GUILayout.Label("화면 모드");
        if (GUILayout.Button(Screen.fullScreen ? "전체 화면 (클릭하여 창 모드)" : "창 모드 (클릭하여 전체 화면)")) { Screen.fullScreen = !Screen.fullScreen; PlayerPrefs.SetInt("settings_fullscreen", Screen.fullScreen ? 1 : 0); PlayerPrefs.Save(); }
        GUILayout.Label("그래픽 품질: " + QualitySettings.names[quality]);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("낮음")) SetQuality(0);
        if (GUILayout.Button("보통")) SetQuality(Mathf.Min(1, QualitySettings.names.Length-1));
        if (GUILayout.Button("높음")) SetQuality(QualitySettings.names.Length-1);
        GUILayout.EndHorizontal(); GUILayout.EndArea();
    }
    void SetQuality(int i) { quality = Mathf.Clamp(i, 0, QualitySettings.names.Length-1); QualitySettings.SetQualityLevel(quality); PlayerPrefs.SetInt("settings_quality", quality); PlayerPrefs.Save(); }
}
