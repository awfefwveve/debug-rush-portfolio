using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>시작화면 버튼 연동 (시작 / 설정 / 업적 / 나가기)</summary>
public class MainMenu : MonoBehaviour
{
    public GameObject settingsPanel;

    void Awake() => SoundEffectManager.EnsureInstance();
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
        if (SettingsWindow.Draw()) settingsPanel.SetActive(false);
    }
}
