using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// QA용 자동 스크린샷. 실행 인자 -autoshot &lt;폴더&gt; 일 때만 동작.
/// 타이틀 → 설정 → 업적 → 맵 → 스테이지 1-1(인트로/플레이) 순서로 캡처 후 종료.
/// </summary>
public class AutoShot : MonoBehaviour
{
    string dir;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        var args = System.Environment.GetCommandLineArgs();
        int i = System.Array.IndexOf(args, "-autoshot");
        if (i < 0 || i + 1 >= args.Length) return;
        var go = new GameObject("AutoShot");
        DontDestroyOnLoad(go);
        go.AddComponent<AutoShot>().dir = args[i + 1];
    }

    IEnumerator Start()
    {
        Directory.CreateDirectory(dir);
        yield return new WaitForSecondsRealtime(2f);
        yield return Shot("01_title");

        var menu = FindAnyObjectByType<MainMenu>();
        if (menu != null) { menu.OnSettings(); yield return new WaitForSecondsRealtime(0.5f); yield return Shot("02_settings"); menu.OnSettings(); }

        SceneManager.LoadScene("Achievements");
        yield return new WaitForSecondsRealtime(1.5f);
        yield return Shot("03_achievements");

        SceneManager.LoadScene("Map");
        yield return new WaitForSecondsRealtime(1.5f);
        yield return Shot("04_map");

        var gm = GameManager.I;
        if (gm != null && gm.allStages != null && gm.allStages.Length > 0)
        {
            gm.StartStage(gm.allStages[0], Difficulty.Normal);
            yield return new WaitForSecondsRealtime(1.5f);
            yield return Shot("05_stage_intro");
            var dlg = FindAnyObjectByType<DialogueUI>();
            if (dlg != null && dlg.skipButton != null) dlg.skipButton.onClick.Invoke();
            yield return new WaitForSecondsRealtime(1.0f);
            // 플레이어를 왼쪽으로 이동시키며 연속 캡처 (달리기 프레임 확인)
            var pc = FindAnyObjectByType<PlayerController>();
            if (pc != null)
            {
                Vector3 start = pc.transform.position;
                for (int f = 0; f < 4; f++)
                {
                    float e = 0f;
                    var rb = pc.GetComponent<Rigidbody2D>(); while (e < 0.12f) { e += Time.deltaTime; rb.position += Vector2.left * 3f * Time.deltaTime; yield return null; }
                    yield return Shot("06_run_" + f);
                }
            }
            yield return new WaitForSecondsRealtime(2.5f);
            yield return Shot("07_stage_play");
        }
        Application.Quit();
    }

    IEnumerator Shot(string name)
    {
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.Combine(dir, name + ".png"));
        yield return new WaitForSecondsRealtime(0.3f);
    }
}
