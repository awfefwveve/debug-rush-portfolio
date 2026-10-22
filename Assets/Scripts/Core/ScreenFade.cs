using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>화면 전환: 새 화면이 열릴 때 검은 화면에서 0.3초 동안 밝아짐. 모든 씬 공통 (씬 배치 없이 자동).</summary>
public class ScreenFade : MonoBehaviour
{
    const float Duration = 0.3f;
    static ScreenFade instance;
    float start;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;
        if (instance == null)
        {
            var go = new GameObject("ScreenFade");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<ScreenFade>();
        }
        instance.start = Time.unscaledTime;
        instance.enabled = true;
    }

    void OnGUI()
    {
        float a = 1f - Mathf.Clamp01((Time.unscaledTime - start) / Duration);
        if (a <= 0f) { enabled = false; return; }
        GUI.depth = -1000;   // 다른 OnGUI 창보다 위
        var prev = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, a);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = prev;
    }
}
