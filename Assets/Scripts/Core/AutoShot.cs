using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// QA용 자동 캡처 + 자동 플레이. 실행 인자 -autoshot &lt;폴더&gt; 일 때만 동작 (일반 플레이에는 영향 없음).
/// 타이틀 → 설정 → 업적 → 맵 → 스테이지 1-1을 봇이 실제로 플레이(재료 집기·제작·전달·점프)하며 캡처.
/// </summary>
public class AutoShot : MonoBehaviour
{
    string dir;
    float playSeconds = 40f;
    bool quick;
    string stageName = "";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        var args = System.Environment.GetCommandLineArgs();
        int sc = System.Array.IndexOf(args, "-showcase");   // 플레이 영상용 자동 시연 (캡처 파일 없음). 예: -showcase 8 (8초 기다렸다 시작)
        if (sc >= 0)
        {
            var g = new GameObject("AutoShot"); DontDestroyOnLoad(g);
            var sh = g.AddComponent<AutoShot>(); sh.showcase = true;
            sh.showcaseDelay = sc + 1 < args.Length && float.TryParse(args[sc + 1], out var dl) ? dl : 5f;
            sh.dir = Path.Combine(Application.temporaryCachePath, "showcase");
            return;
        }
        int i = System.Array.IndexOf(args, "-autoshot");
        if (i < 0 || i + 1 >= args.Length) return;
        var go = new GameObject("AutoShot");
        DontDestroyOnLoad(go);
        var a = go.AddComponent<AutoShot>(); a.dir = args[i + 1];
        a.quick = System.Array.IndexOf(args, "-autoshot-quick") >= 0;   // 해상도 확인용: 타이틀 + 스테이지만
        int s = System.Array.IndexOf(args, "-autoshot-stage"); if (s >= 0 && s + 1 < args.Length) a.stageName = args[s + 1];   // 예: 1-6
    }

    IEnumerator Start()
    {
        if (showcase) { yield return Showcase(); yield break; }
        Directory.CreateDirectory(dir);
        yield return new WaitForSecondsRealtime(2f);
        yield return Shot("01_title");
        yield return FrameStatsFor("title", 3f);

        if (quick) playSeconds = 4f;
        var menu = quick ? null : FindAnyObjectByType<MainMenu>();
        if (menu != null) { menu.OnSettings(); yield return new WaitForSecondsRealtime(0.5f); yield return Shot("02_settings"); menu.OnSettings(); }

        if (!quick)
        {
            SceneManager.LoadScene("Achievements");
            yield return new WaitForSecondsRealtime(1.5f);
            yield return Shot("03_achievements");
        }

        SceneManager.LoadScene("Map");
        yield return new WaitForSecondsRealtime(1.5f);
        yield return Shot("04_map");

        var gm = GameManager.I;
        if (gm != null && gm.allStages != null && gm.allStages.Length > 0)
        {
            var pick = gm.allStages.FirstOrDefault(x => x != null && $"{x.chapter}-{x.stage}" == stageName) ?? gm.allStages[0];
            gm.StartStage(pick, Difficulty.Easy);   // 1학년 = Easy
            yield return new WaitForSecondsRealtime(1.5f);
            yield return Shot("05_stage_intro");
            var dlg = FindAnyObjectByType<DialogueUI>();
            if (dlg != null && dlg.skipButton != null) dlg.skipButton.onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.6f);
            yield return Play();
        }
        Application.Quit();
    }

    bool showcase;
    float showcaseDelay = 5f;

    /// <summary>플레이 영상용: 타이틀 → 스테이지 선택 → 1-1 자동 플레이 → 1-3 → 1-5 보스 → 클리어 화면 → 종료 (약 60초)</summary>
    IEnumerator Showcase()
    {
        yield return new WaitForSecondsRealtime(showcaseDelay);   // 녹화 시작할 시간
        yield return new WaitForSecondsRealtime(4f);              // 타이틀
        SceneManager.LoadScene("Map");
        yield return new WaitForSecondsRealtime(3f);
        yield return PlayStage("1-1", 28f, false);
        yield return PlayStage("1-3", 9f, false);
        yield return PlayStage("1-5", 11f, true);
        yield return new WaitForSecondsRealtime(1f);
        Application.Quit();
    }

    IEnumerator PlayStage(string name, float seconds, bool clearAtEnd)
    {
        var gm = GameManager.I;
        var s = gm != null ? gm.allStages.FirstOrDefault(x => x != null && $"{x.chapter}-{x.stage}" == name) : null;
        if (s == null) yield break;
        gm.StartStage(s, Difficulty.Easy);
        yield return new WaitForSecondsRealtime(1.2f);
        for (int i = 0; i < 30; i++)   // 시작 대사 넘기기
        {
            var dlg = FindAnyObjectByType<DialogueUI>();
            if (dlg != null && dlg.skipButton != null && dlg.skipButton.gameObject.activeInHierarchy) dlg.skipButton.onClick.Invoke();
            if (StageManager.I != null && StageManager.I.Running) break;
            yield return new WaitForSecondsRealtime(0.1f);
        }
        player = FindAnyObjectByType<PlayerController>();
        if (player == null) yield break;
        StartCoroutine(Brain());
        yield return new WaitForSeconds(seconds);
        if (clearAtEnd && StageManager.I != null && StageManager.I.Running)
        {
            StageManager.I.DebugClear();
            for (int i = 0; i < 40; i++)
            {
                var dlg = FindAnyObjectByType<DialogueUI>();
                if (dlg != null && dlg.skipButton != null && dlg.skipButton.gameObject.activeInHierarchy) dlg.skipButton.onClick.Invoke();
                yield return new WaitForSecondsRealtime(0.1f);
            }
            // 학년 클리어 화면 잠깐 보여 주고 닫기
            yield return new WaitForSecondsRealtime(2.5f);
            var card = FindAnyObjectByType<ChapterEndCard>(); if (card != null) card.Close();
            yield return new WaitForSecondsRealtime(5f);
        }
    }

    // ---------- 자동 플레이 ----------
    PlayerController player;
    float runStripUntil = -1f;
    int stripIndex;

    IEnumerator Play()
    {
        player = FindAnyObjectByType<PlayerController>();
        if (player == null) yield break;
        StartCoroutine(Brain());
        StartCoroutine(FrameStats("play"));
        StartCoroutine(Watcher());
        WallCheck();
        StartCoroutine(RecipeSlide());

        // 달리기 프레임 연속 캡처 (0.09초 간격 12장)
        yield return new WaitForSeconds(0.4f);
        for (int i = 0; i < 12; i++) { yield return Shot($"10_run_{i:00}"); yield return new WaitForSeconds(0.09f); }

        float end = Time.time + playSeconds;
        int n = 0;
        while (Time.time < end && StageManager.I != null && StageManager.I.Running)
        {
            yield return new WaitForSeconds(3f);
            yield return Shot($"20_play_{n++:00}");
        }
        yield return new WaitForSeconds(1.5f);
        yield return Shot("29_end");
        // 결과 화면 확인: 바로 클리어 → 엔딩 대사 넘기기 → 결과 화면 캡처
        if (StageManager.I != null && StageManager.I.Running)
        {
            StageManager.I.DebugClear();
            for (int i = 0; i < 40; i++)
            {
                var dlg = FindAnyObjectByType<DialogueUI>();
                if (dlg != null && dlg.skipButton != null && dlg.skipButton.gameObject.activeInHierarchy) dlg.skipButton.onClick.Invoke();
                yield return new WaitForSecondsRealtime(0.1f);
            }
            yield return Shot("50_result");
        }
        player.BotInput = null;
    }

    IEnumerator Watcher()
    {
        bool windup = false, bullets = false, fx = false;
        while (StageManager.I != null && player != null)
        {
            var m = FindAnyObjectByType<MonsterBase>();
            if (!windup && m != null && m.CurrentPhase == MonsterBase.Phase.Windup)
            { windup = true; yield return new WaitForSeconds(0.35f); yield return Shot("30_telegraph"); }
            if (!bullets && FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length >= 2)
            {
                bullets = true;
                for (int i = 0; i < 4; i++) { yield return Shot($"31_bullets_{i}"); yield return new WaitForSeconds(0.12f); }
            }
            if (!fx && FindAnyObjectByType<MagneticField>() != null) { fx = true; yield return new WaitForSeconds(0.3f); yield return Shot("32_magnetic"); }
            yield return null;
        }
    }

    IEnumerator Brain()
    {
        var sm = StageManager.I;
        var table = FindAnyObjectByType<CraftingTable>();
        var stations = FindObjectsByType<ResourceStation>(FindObjectsSortMode.None);
        float nextJump = 0f;
        while (sm != null && sm.Running && player != null)
        {
            // 탄 피하기: 가까운 탄이 있으면 점프 (패링 가능 탄이면 패링)
            if (Time.time > nextJump && FindObjectsByType<Projectile>(FindObjectsSortMode.None).Any(p => Vector2.Distance(p.transform.position, (Vector2)player.transform.position + Vector2.up * 0.6f) < 1.6f))
            { player.Jump(); nextJump = Time.time + 0.7f; }

            var term = sm.terminals.FirstOrDefault(t => t != null && !t.IsDone && t.Remaining.Count > 0);
            if (term == null) { player.BotInput = Vector2.zero; yield return null; continue; }
            var need = term.Remaining[0];
            var inv = player.Inventory.items;

            Component goal; Vector2 approach;
            if (inv.Contains(need)) { goal = term; approach = Side(term.transform.position, 1.95f); }
            else if (need.IsMaterial()) { goal = Station(stations, need); approach = Near(goal.transform.position, 1.75f); if (inv.Count >= PlayerInventory.Max) { player.DropLastItem(); yield return null; continue; } }
            else if (table.HasOutput && table.Output == need) { goal = table; approach = (Vector2)table.transform.position + Vector2.down * 2.0f; }
            else if (table.IsCrafting) { player.BotInput = Vector2.zero; yield return null; continue; }
            else
            {
                var (a, b) = Recipe(need);
                bool hasA = inv.Contains(a), hasB = a == b ? inv.Count(x => x == a) >= 2 : inv.Contains(b);
                if (inv.Any(x => x != a && x != b)) { player.DropLastItem(); yield return null; continue; }
                if (hasA && hasB) { goal = table; approach = (Vector2)table.transform.position + Vector2.down * 2.0f; }
                else { var m = !hasA ? a : b; goal = Station(stations, m); approach = Near(goal.transform.position, 1.75f); }
            }

            // 이동: 아래 통로 / 위 통로, 가운데 작업대 라인은 x=±9.5에서 건넘
            Vector2 pos = player.transform.position;
            Vector2 wp = Nav(pos, approach);
            Vector2 d = wp - pos;
            float distGoal = Vector2.Distance(pos, approach);
            player.BotInput = d.magnitude > 0.15f ? d.normalized : Vector2.zero;

            if (player.FocusTarget != null && player.FocusTarget.gameObject == goal.gameObject && distGoal < 1.2f)
            {
                player.BotInput = Vector2.zero;
                player.Interact();
                yield return new WaitForSeconds(0.25f);
            }
            yield return null;
        }
    }

    const float LaneBottom = -2.6f, LaneTop = 2.4f, CrossX = 9.5f;
    static Vector2 Near(Vector3 p, float dy) => new Vector2(p.x, p.y > 0 ? LaneTop : LaneBottom);
    static Vector2 Side(Vector3 p, float dx) => new Vector2(p.x + (p.x > 0 ? -1.9f : 1.9f), p.y > 0 ? LaneTop : LaneBottom);

    static Vector2 Nav(Vector2 pos, Vector2 goal)
    {
        bool topNow = pos.y > 0f, topGoal = goal.y > 0f;
        float lane = topNow ? LaneTop : LaneBottom;
        if (topNow == topGoal)
        {
            if (Mathf.Abs(pos.y - lane) > 0.25f && Mathf.Abs(pos.x - goal.x) > 0.3f) return new Vector2(pos.x, lane);
            return goal;
        }
        float cx = Mathf.Abs(pos.x + CrossX) < Mathf.Abs(pos.x - CrossX) ? -CrossX : CrossX;
        if (Mathf.Abs(pos.x - cx) > 0.25f) return Mathf.Abs(pos.y - lane) > 0.25f ? new Vector2(pos.x, lane) : new Vector2(cx, lane);
        return new Vector2(cx, topGoal ? LaneTop : LaneBottom);
    }
    static ResourceStation Station(ResourceStation[] all, ItemType m) => all.First(s => s.material == m);

    static (ItemType, ItemType) Recipe(ItemType result)
    {
        var mats = new[] { ItemType.Planning, ItemType.Programming, ItemType.Graphic, ItemType.QA };
        foreach (var a in mats) foreach (var b in mats) if (ItemRules.Combine(a, b) == result) return (a, b);
        return (ItemType.Planning, ItemType.QA);
    }

    /// <summary>"보이는 벽 = 막히는 벽" 검사: 막히는 콜라이더 칸마다 그 자리에 보이는 그림이 있는지</summary>
    void WallCheck()
    {
        var renderers = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Where(r => r.enabled && r.gameObject.activeInHierarchy && r.sprite != null && r.color.a > 0.1f).ToArray();
        int cells = 0, bad = 0;
        foreach (var c in FindObjectsByType<Collider2D>(FindObjectsSortMode.None))
        {
            if (c.name == "Void" || c.isTrigger || !c.enabled || c.attachedRigidbody != null && c.attachedRigidbody.bodyType == RigidbodyType2D.Dynamic) continue;
            var b = c.bounds;
            for (float x = b.min.x + 0.5f; x < b.max.x; x += 1f)
                for (float y = b.min.y + 0.5f; y < b.max.y; y += 1f)
                {
                    var p = new Vector3(x, y, 0f);
                    if (!c.OverlapPoint(p)) continue;
                    cells++;
                    if (!renderers.Any(r => { var rb = r.bounds; rb.extents += new Vector3(0.05f, 0.05f, 10f); return rb.Contains(p); }))
                    { bad++; Debug.Log($"[WallCheck] invisible blocker {c.name} at {p}"); }
                }
        }
        Debug.Log($"[WallCheck] stage={StageManager.I?.Data?.displayName} blockingCells={cells} invisible={bad}");
    }

    IEnumerator RecipeSlide()
    {
        var panel = FindAnyObjectByType<RecipePanel>();
        if (panel == null) yield break;
        yield return new WaitForSecondsRealtime(6f);
        var cam = Camera.main;
        float size0 = cam.orthographicSize; Vector3 pos0 = cam.transform.position;
        panel.Toggle();
        yield return new WaitForSecondsRealtime(0.1f); yield return Shot("40_recipe_toggling");
        yield return new WaitForSecondsRealtime(0.4f); yield return Shot("41_recipe_toggled");
        Debug.Log($"[RecipeCheck] camera size {size0}->{cam.orthographicSize} pos {pos0}->{cam.transform.position}");
        panel.Toggle();
        yield return new WaitForSecondsRealtime(0.5f); yield return Shot("42_recipe_back");
    }

    /// <summary>프레임 시간 기록: 평균·상위 1%·최대 (렉 확인용)</summary>
    IEnumerator FrameStatsFor(string label, float seconds)
    {
        var list = new System.Collections.Generic.List<float>();
        float end = Time.realtimeSinceStartup + seconds;
        while (Time.realtimeSinceStartup < end) { list.Add(Time.unscaledDeltaTime * 1000f); yield return null; }
        list.Sort(); float avg = 0f; foreach (var v in list) avg += v; avg /= Mathf.Max(1, list.Count);
        Debug.Log($"[Frame:{label}] n={list.Count} avg={avg:F1}ms p99={list[(int)(list.Count * 0.99f)]:F1}ms max={list[list.Count - 1]:F1}ms q={QualitySettings.GetQualityLevel()} {QualitySettings.names[QualitySettings.GetQualityLevel()]} hz={Screen.currentResolution.refreshRateRatio.value:F0}");
    }

    IEnumerator FrameStats(string label = "play")
    {
        var list = new System.Collections.Generic.List<float>();
        float end = Time.realtimeSinceStartup + 12f;
        while (Time.realtimeSinceStartup < end) { list.Add(Time.unscaledDeltaTime * 1000f); yield return null; }
        list.Sort();
        float avg = 0f; foreach (var v in list) avg += v; avg /= Mathf.Max(1, list.Count);
        Debug.Log($"[Frame:{label}] n={list.Count} avg={avg:F1}ms p99={list[(int)(list.Count * 0.99f)]:F1}ms max={list[list.Count - 1]:F1}ms gc={System.GC.CollectionCount(0)} vsync={QualitySettings.vSyncCount} target={Application.targetFrameRate}");
    }

    IEnumerator Shot(string name)
    {
        if (showcase) yield break;
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.Combine(dir, name + ".png"));
    }
}
