using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>개발 빌드에서 F1로 여는 진행/업적 해금 도구.</summary>
public class DebugMenu : MonoBehaviour
{
    bool visible;
    void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Input.GetKeyDown(KeyCode.F1)) visible = !visible;
#endif
    }
    void OnGUI()
    {
        KoreanIMGUIFont.Apply();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (!visible) return;
        GUILayout.BeginArea(new Rect(12, 12, 300, 190), "디버그 모드 (F1)", GUI.skin.window);
        if (GUILayout.Button("전체 스테이지 난이도 해금"))
        {
            var gm = GameManager.I;
            if (gm != null) foreach (var s in gm.allStages)
                for (int d = 0; d < 3; d++) PlayerPrefs.SetInt($"clear_{s.chapter}_{s.stage}_{d}", 1);
            PlayerPrefs.Save();
        }
        if (GUILayout.Button("전체 업적 해금"))
        {
            AchievementManager.Unlock("first_clear");
            AchievementManager.Unlock("no_damage");
        }
        if (GUILayout.Button("타이틀 씬 열기")) SceneManager.LoadScene("Title");
        GUILayout.EndArea();
#endif
    }
}
