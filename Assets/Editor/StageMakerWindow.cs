using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 기획자용 맵 그리기 툴. 메뉴: DebugRush > Stage Maker
/// 한 창 안에서 끝남: 왼쪽 스테이지 목록 / 가운데 팔레트+방 격자 / 오른쪽 정보 입력 / 위 툴바.
/// 씬을 열거나 오브젝트를 찾을 필요 없음. 스테이지별 데이터(StageData 에셋)에 저장.
/// </summary>
public class StageMakerWindow : EditorWindow
{
    const string DataFolder = "Assets/DebugRush/Data";
    const string TitleScenePath = "Assets/Scenes/Title.unity";
    const string StageScenePath = "Assets/Scenes/Stage.unity";
    const string BeePrefab = "Assets/DebugRush/Prefabs/Monster1A.prefab";
    const int Chapters = 3, PerChapter = 10;

    enum Tool { Move, Wall, Erase, Planning, Programming, Graphic, QA, Crafter, Terminal, Player, Bee }

    static readonly (Tool tool, string label, string tip)[] Palette =
    {
        (Tool.Move, "이동", "물체를 끌어서 옮기기"),
        (Tool.Wall, "벽", "클릭·드래그로 벽 칠하기"),
        (Tool.Erase, "지우개", "벽·단말기·벌 지우기 (우클릭도 지우기)"),
        (Tool.Planning, "기획", "기획 재료대 놓기"),
        (Tool.Programming, "프로그래밍", "프로그래밍 재료대 놓기"),
        (Tool.Graphic, "그래픽", "그래픽 재료대 놓기"),
        (Tool.QA, "QA", "QA 재료대 놓기"),
        (Tool.Crafter, "제작대", "제작대 놓기"),
        (Tool.Terminal, "단말기", "단말기 추가 (최대 4)"),
        (Tool.Player, "시작점", "플레이어 시작 위치"),
        (Tool.Bee, "벌", "몬스터(벌) 위치. 최대 2마리"),
    };

    StageData selected;
    string selectedPath;
    int chapterTab;
    Tool tool = Tool.Wall;
    Vector2 rightScroll, leftScroll;
    int autoSeed = 1;

    // 드래그 상태
    string dragging;           // "planning", "crafter", "terminal:1", "player", "bee:0" …
    bool painting, erasing;

    // 그림
    readonly Dictionary<string, Texture2D> thumbs = new Dictionary<string, Texture2D>();
    GUIStyle big, bigBold, title, warn, ok, cardOn, cardOff, cardSel;

    [MenuItem("DebugRush/Stage Maker")]
    public static void Open()
    {
        var w = GetWindow<StageMakerWindow>("Stage Maker");
        w.minSize = new Vector2(1280, 720);
    }

    void OnEnable() => Undo.undoRedoPerformed += Repaint;
    void OnDisable() => Undo.undoRedoPerformed -= Repaint;

    // =====================================================================================
    void OnGUI()
    {
        Styles();
        if (selected == null && !string.IsNullOrEmpty(selectedPath)) selected = AssetDatabase.LoadAssetAtPath<StageData>(selectedPath);

        DrawToolbar();
        var r = new Rect(0, 44, position.width, position.height - 44);
        float leftW = 230f, rightW = 380f;
        DrawStageList(new Rect(r.x, r.y, leftW, r.height));
        DrawCenter(new Rect(r.x + leftW, r.y, r.width - leftW - rightW, r.height));
        DrawRight(new Rect(r.xMax - rightW, r.y, rightW, r.height));
    }

    // ---------------- 위 툴바 ----------------
    void DrawToolbar()
    {
        GUILayout.BeginArea(new Rect(6, 6, position.width - 12, 34));
        GUILayout.BeginHorizontal();
        if (Btn("＋ 새 스테이지", 120)) NewStage(false);
        GUI.enabled = selected != null;
        if (Btn("복제", 70)) NewStage(true);
        if (Btn("삭제", 70)) DeleteStage();
        GUILayout.Space(16);
        if (Btn("자동 생성", 90)) AutoGenerate();
        if (Btn("기본 배치", 90)) { Record("기본 배치"); selected.layout = StageLayoutDefaults.Default(); SetBee(0, new Vector2(3f, 3f)); }
        if (Btn("되돌리기 (Ctrl+Z)", 140)) Undo.PerformUndo();
        GUILayout.Space(16);
        if (Btn("저장", 80)) Save(selected);
        if (Btn("▶ 플레이", 100)) Play(selected);
        GUI.enabled = true;
        GUILayout.FlexibleSpace();
        GUILayout.Label(selected != null ? $"{selected.chapter}-{selected.stage}  {selected.displayName}" : "스테이지를 고르거나 새로 만드세요", bigBold);
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    bool Btn(string text, float w) => GUILayout.Button(text, GUILayout.Width(w), GUILayout.Height(30));

    // ---------------- 왼쪽 스테이지 목록 ----------------
    void DrawStageList(Rect r)
    {
        GUILayout.BeginArea(new Rect(r.x + 6, r.y, r.width - 10, r.height - 6));
        chapterTab = GUILayout.Toolbar(chapterTab, new[] { "1학년", "2학년", "3학년" }, GUILayout.Height(30));
        GUILayout.Space(6);
        leftScroll = GUILayout.BeginScrollView(leftScroll);
        int c = chapterTab + 1;
        var all = LoadAll();
        var reg = Registered();
        for (int n = 1; n <= PerChapter; n++)
        {
            var s = all.FirstOrDefault(x => x.chapter == c && x.stage == n);
            string label = s != null ? $"{c}-{n}   {s.displayName}\n<size=11>{(reg.Contains(s) ? "완성 · 맵에 표시됨" : "미등록")}</size>" : $"{c}-{n}   비어 있음\n<size=11>눌러서 새로 만들기</size>";
            var style = s == null ? cardOff : s == selected ? cardSel : cardOn;
            var prevBg = GUI.backgroundColor;
            GUI.backgroundColor = s == null ? new Color(0.55f, 0.55f, 0.6f) : s == selected ? new Color(0.4f, 0.85f, 1f) : new Color(0.45f, 1f, 0.55f);
            bool clicked = GUILayout.Button(label, style, GUILayout.Height(52));
            GUI.backgroundColor = prevBg;
            if (clicked)
            {
                if (s != null) Select(s);
                else CreateAt(c, n, null);
            }
        }
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    // ---------------- 가운데: 팔레트 + 격자 ----------------
    void DrawCenter(Rect r)
    {
        // 팔레트
        var pal = new Rect(r.x + 6, r.y, r.width - 12, 64);
        float bw = (pal.width - 4) / Palette.Length;
        for (int i = 0; i < Palette.Length; i++)
        {
            var b = new Rect(pal.x + i * bw, pal.y, bw - 4, 60);
            bool on = tool == Palette[i].tool;
            var prev = GUI.backgroundColor;
            GUI.backgroundColor = on ? new Color(0.35f, 0.85f, 1f) : Color.white;
            if (GUI.Button(b, new GUIContent("", Palette[i].tip))) tool = Palette[i].tool;
            GUI.backgroundColor = prev;
            var tex = ToolThumb(Palette[i].tool);
            if (tex != null) GUI.DrawTexture(new Rect(b.x + 4, b.y + 3, b.width - 8, 34), tex, ScaleMode.ScaleToFit);
            GUI.Label(new Rect(b.x, b.y + 38, b.width, 20), Palette[i].label, title);
        }
        GUI.Label(new Rect(r.x + 8, r.y + 66, r.width, 20), Palette.First(p => p.tool == tool).tip + "   ·   우클릭 = 지우기   ·   이동 도구로 물체를 끌기", EditorStyles.miniLabel);

        if (selected == null)
        {
            GUI.Label(new Rect(r.x, r.y + 200, r.width, 40), "왼쪽에서 스테이지를 고르거나 [＋ 새 스테이지]를 누르세요", title);
            return;
        }
        EnsureLayout(selected);

        // 격자 (방 크기 = 스테이지별, 기본 30×16)
        GW = selected.layout.width; GH = selected.layout.height;
        var area = new Rect(r.x + 8, r.y + 90, r.width - 16, r.height - 170);
        float cs = Mathf.Floor(Mathf.Min(area.width / GW, area.height / GH));
        var grid = new Rect(area.x + (area.width - cs * GW) / 2f, area.y, cs * GW, cs * GH);
        EditorGUI.DrawRect(grid, new Color(0.09f, 0.11f, 0.17f));
        for (int x = 0; x <= GW; x++) EditorGUI.DrawRect(new Rect(grid.x + x * cs, grid.y, 1, grid.height), new Color(1, 1, 1, 0.05f));
        for (int y = 0; y <= GH; y++) EditorGUI.DrawRect(new Rect(grid.x, grid.y + y * cs, grid.width, 1), new Color(1, 1, 1, 0.05f));

        var L = selected.layout;
        var wallSet = new HashSet<Vector2Int>(L.walls);
        var fx = FxSet.Get();
        foreach (var c in wallSet)
        {
            WallBuilder.Piece(wallSet, c, out var spr, out float ang, fx);
            var cell = CellRect(grid, cs, c);
            if (spr != null) DrawSprite(cell, spr, ang); else EditorGUI.DrawRect(cell, new Color(0.35f, 0.42f, 0.58f));
        }

        // 보유 자료 칸에 가려지는 구역 (물체 금지)
        var hz = StageLayoutDefaults.HudZone(GW, GH);
        var hzr = WorldRect(grid, cs, hz.center, hz.size);
        EditorGUI.DrawRect(hzr, new Color(1f, 0.35f, 0.35f, 0.22f));
        GUI.Label(hzr, "보유 자료\n(놓지 말 것)", EditorStyles.centeredGreyMiniLabel);

        // 물체
        foreach (var o in Objects()) DrawObject(grid, cs, o);

        HandleMouse(grid, cs);

        // 경고
        var y0 = grid.yMax + 8;
        foreach (var (msg, bad) in Checks())
        {
            GUI.Label(new Rect(r.x + 12, y0, r.width - 24, 20), (bad ? "⚠ " : "✓ ") + msg, bad ? warn : ok);
            y0 += 20;
        }
    }

    struct Obj { public string id, label; public Vector2 pos, size; public Texture2D tex; public bool feet; }

    IEnumerable<Obj> Objects()
    {
        var L = selected.layout;
        yield return O("planning", "기획", L.planning, StageLayoutDefaults.StationSize, "plan");
        yield return O("programming", "프로그래밍", L.programming, StageLayoutDefaults.StationSize, "programming");
        yield return O("graphic", "그래픽", L.graphic, StageLayoutDefaults.StationSize, "graphic");
        yield return O("qa", "QA", L.qa, StageLayoutDefaults.StationSize, "qa");
        yield return O("crafter", "제작대", L.craftingTable, StageLayoutDefaults.CrafterSize, "crafter");
        for (int i = 0; i < L.terminals.Length; i++) yield return O("terminal:" + i, $"단말기 {i + 1}", L.terminals[i], StageLayoutDefaults.TerminalSize, "terminal");
        var p = O("player", "시작점", L.playerStart + new Vector2(0f, 1f), new Vector2(2f, 2f), "player"); p.feet = true; yield return p;
        if (selected.monsters != null)
            for (int i = 0; i < selected.monsters.Length; i++) yield return O("bee:" + i, "벌", selected.monsters[i].spawnPos, new Vector2(2f, 2f), "bee");
    }

    Obj O(string id, string label, Vector2 pos, Vector2 size, string thumb) => new Obj { id = id, label = label, pos = pos, size = size, tex = Thumb(thumb) };

    void DrawObject(Rect grid, float cs, Obj o)
    {
        var rect = WorldRect(grid, cs, o.pos, o.size);
        if (dragging == o.id) EditorGUI.DrawRect(rect, new Color(0.3f, 0.9f, 1f, 0.25f));
        if (o.tex != null) GUI.DrawTexture(rect, o.tex, ScaleMode.ScaleToFit);
        else EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.6f, 0.6f));
        var lr = new Rect(rect.x - 20, rect.yMax - 2, rect.width + 40, 18);
        EditorGUI.DrawRect(new Rect(lr.center.x - 40, lr.y, 80, 18), new Color(0, 0, 0, 0.55f));
        GUI.Label(lr, o.label, title);
    }

    // ---------------- 마우스 ----------------
    void HandleMouse(Rect grid, float cs)
    {
        var e = Event.current;
        if (!grid.Contains(e.mousePosition) && e.type != EventType.MouseUp) return;
        Vector2 world = new Vector2(WallBuilder.MinX(GW) + (e.mousePosition.x - grid.x) / cs, WallBuilder.MaxY(GH) + 1 - (e.mousePosition.y - grid.y) / cs);
        var cell = new Vector2Int(Mathf.FloorToInt(world.x), Mathf.FloorToInt(world.y));
        Vector2 snap = new Vector2(StageLayoutDefaults.Snap(world.x), StageLayoutDefaults.Snap(world.y));
        var L = selected.layout;

        if (e.type == EventType.MouseDown)
        {
            if (e.button == 1) { erasing = true; EraseAt(world, cell); e.Use(); return; }
            if (e.button != 0) return;
            switch (tool)
            {
                case Tool.Move:
                    var hit = Objects().Reverse().FirstOrDefault(o => WorldRect(grid, cs, o.pos, o.size).Contains(e.mousePosition));
                    if (hit.id != null) { Record("이동"); dragging = hit.id; }
                    break;
                case Tool.Wall: Record("벽"); painting = true; if (WallBuilder.InRoom(cell, GW, GH) && !L.walls.Contains(cell)) L.walls.Add(cell); break;
                case Tool.Erase: erasing = true; EraseAt(world, cell); break;
                case Tool.Planning: Record("재료대"); L.planning = snap; dragging = "planning"; break;
                case Tool.Programming: Record("재료대"); L.programming = snap; dragging = "programming"; break;
                case Tool.Graphic: Record("재료대"); L.graphic = snap; dragging = "graphic"; break;
                case Tool.QA: Record("재료대"); L.qa = snap; dragging = "qa"; break;
                case Tool.Crafter: Record("제작대"); L.craftingTable = snap; dragging = "crafter"; break;
                case Tool.Player: Record("시작점"); L.playerStart = snap - new Vector2(0f, 1f); dragging = "player"; break;
                case Tool.Terminal:
                    if (L.terminals.Length >= 4) { ShowNotification(new GUIContent("단말기는 최대 4대")); break; }
                    Record("단말기"); L.terminals = L.terminals.Concat(new[] { snap }).ToArray(); dragging = "terminal:" + (L.terminals.Length - 1); break;
                case Tool.Bee:
                    int count = selected.monsters?.Length ?? 0;
                    if (count >= 2) { ShowNotification(new GUIContent("몬스터는 스테이지당 최대 2마리")); break; }
                    Record("벌"); SetBee(count, snap); dragging = "bee:" + count; break;
            }
            e.Use();
        }
        else if (e.type == EventType.MouseDrag)
        {
            if (erasing) EraseAt(world, cell);
            else if (painting && WallBuilder.InRoom(cell, GW, GH) && !L.walls.Contains(cell)) L.walls.Add(cell);
            else if (dragging != null) MoveObject(dragging, snap);
            e.Use();
        }
        else if (e.type == EventType.MouseUp)
        {
            if (painting || erasing || dragging != null) EditorUtility.SetDirty(selected);
            painting = erasing = false; dragging = null;
            e.Use();
        }
    }

    void MoveObject(string id, Vector2 p)
    {
        var L = selected.layout;
        if (id == "planning") L.planning = p;
        else if (id == "programming") L.programming = p;
        else if (id == "graphic") L.graphic = p;
        else if (id == "qa") L.qa = p;
        else if (id == "crafter") L.craftingTable = p;
        else if (id == "player") L.playerStart = p - new Vector2(0f, 1f);
        else if (id.StartsWith("terminal:")) L.terminals[int.Parse(id.Substring(9))] = p;
        else if (id.StartsWith("bee:")) selected.monsters[int.Parse(id.Substring(4))].spawnPos = p;
    }

    void EraseAt(Vector2 world, Vector2Int cell)
    {
        var L = selected.layout;
        Record("지우기");
        // 단말기 / 벌 지우기
        for (int i = L.terminals.Length - 1; i >= 0; i--)
            if (new Rect(L.terminals[i] - StageLayoutDefaults.TerminalSize / 2f, StageLayoutDefaults.TerminalSize).Contains(world) && L.terminals.Length > 1)
            { L.terminals = L.terminals.Where((t, k) => k != i).ToArray(); return; }
        if (selected.monsters != null)
            for (int i = selected.monsters.Length - 1; i >= 0; i--)
                if (Vector2.Distance(selected.monsters[i].spawnPos, world) < 1f && selected.monsters.Length > 1)
                { selected.monsters = selected.monsters.Where((m, k) => k != i).ToArray(); return; }
        L.walls.Remove(cell);
    }

    void SetBee(int index, Vector2 pos)
    {
        var list = (selected.monsters ?? new MonsterSpawn[0]).ToList();
        while (list.Count <= index)
            list.Add(new MonsterSpawn { prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BeePrefab), patternCount = 1, patternMask = 1 });
        list[index].spawnPos = pos;
        selected.monsters = list.ToArray();
    }

    // ---------------- 경고 ----------------
    IEnumerable<(string, bool)> Checks()
    {
        var s = selected; var L = s.layout; GW = L.width; GH = L.height;
        var w = new HashSet<Vector2Int>(L.walls);
        var objs = Objects().ToList();
        bool any = false;
        foreach (var o in objs.Where(o => !o.id.StartsWith("bee")))
        {
            var size = o.id == "player" ? new Vector2(1f, 1f) : o.size;
            var center = o.id == "player" ? L.playerStart + new Vector2(0f, 0.5f) : o.pos;
            if (StageLayoutDefaults.HitsWall(w, center, size)) { any = true; yield return ($"{o.label}이(가) 벽과 겹쳐 있어요", true); }
            if (center.x - size.x / 2f < WallBuilder.MinX(GW) + 1 || center.x + size.x / 2f > WallBuilder.MaxX(GW) || center.y - size.y / 2f < WallBuilder.MinY(GH) + 1 || center.y + size.y / 2f > WallBuilder.MaxY(GH))
            { any = true; yield return ($"{o.label}이(가) 방 밖으로 나가 있어요", true); }
            if (new Rect(center - size / 2f, size).Overlaps(StageLayoutDefaults.HudZone(GW, GH)))
            { any = true; yield return ($"{o.label}이(가) 왼쪽 아래 보유 자료 칸에 가려져요 (빗금 구역)", true); }
        }
        for (int i = 0; i < objs.Count; i++)
            for (int j = i + 1; j < objs.Count; j++)
            {
                if (objs[i].id.StartsWith("bee") || objs[j].id.StartsWith("bee")) continue;
                var a = Box(objs[i]); var b = Box(objs[j]);
                if (a.Overlaps(b)) { any = true; yield return ($"{objs[i].label}와(과) {objs[j].label}이(가) 겹쳐 있어요", true); }
            }
        if (L.terminals.Length < 1 || L.terminals.Length > 4) { any = true; yield return ("단말기는 1~4대", true); }
        if (s.monsters == null || s.monsters.Length == 0) { any = true; yield return ("몬스터(벌)를 1마리 이상 놓으세요", true); }
        if (s.intro == null || s.intro.Length == 0) yield return ("시작 대사가 비어 있어요 (기획서 p.13: 스테이지 시작·끝 스토리)", true);
        if (s.outro == null || s.outro.Length == 0) yield return ("끝 대사가 비어 있어요", true);
        if (!any) yield return ("배치 문제 없음", false);
    }

    Rect Box(Obj o) => o.id == "player" ? new Rect(selected.layout.playerStart + new Vector2(-0.6f, -0.1f), new Vector2(1.2f, 1.2f)) : new Rect(o.pos - o.size / 2f, o.size);

    // ---------------- 오른쪽: 정보 입력 ----------------
    void DrawRight(Rect r)
    {
        GUILayout.BeginArea(new Rect(r.x + 6, r.y, r.width - 12, r.height - 6));
        if (selected == null) { GUILayout.EndArea(); return; }
        rightScroll = GUILayout.BeginScrollView(rightScroll);
        var s = selected;
        EditorGUI.BeginChangeCheck();

        Head("스테이지 이름");
        string name = EditorGUILayout.TextField(s.displayName, big, GUILayout.Height(28));
        Head("한 줄 요약 (맵 카드에 표시)");
        string summary = EditorGUILayout.TextArea(s.summary ?? "", big, GUILayout.Height(48));

        Head("방 크기 (칸) — 기본 30×16 = 화면에 꽉 참");
        GUILayout.BeginHorizontal();
        GUILayout.Label("가로", big, GUILayout.Width(36));
        int nw = EditorGUILayout.DelayedIntField(s.layout.width, big, GUILayout.Width(50), GUILayout.Height(26));
        GUILayout.Label("세로", big, GUILayout.Width(36));
        int nh = EditorGUILayout.DelayedIntField(s.layout.height, big, GUILayout.Width(50), GUILayout.Height(26));
        if (GUILayout.Button("기본 30×16", GUILayout.Height(26))) { nw = WallBuilder.DefaultW; nh = WallBuilder.DefaultH; }
        GUILayout.EndHorizontal();
        GUILayout.Label("짝수만 (가로 16~40, 세로 10~22). 바꾸면 바깥 벽이 새 크기로 다시 깔림", EditorStyles.miniLabel);
        if (nw != s.layout.width || nh != s.layout.height) ResizeRoom(nw, nh);

        Head("제한시간 (쉬움 기준: 이 값 × 1.3)");
        int total = Mathf.RoundToInt(s.timeLimit);
        GUILayout.BeginHorizontal();
        int min = EditorGUILayout.IntField(total / 60, big, GUILayout.Width(60), GUILayout.Height(26)); GUILayout.Label("분", big, GUILayout.Width(24));
        int sec = EditorGUILayout.IntField(total % 60, big, GUILayout.Width(60), GUILayout.Height(26)); GUILayout.Label("초", big, GUILayout.Width(24));
        GUILayout.EndHorizontal();

        Head("단말기 1대당 고칠 칸 수 (2~8)");
        var goals = (s.terminalGoal != null && s.terminalGoal.Length == 3) ? (int[])s.terminalGoal.Clone() : new[] { 2, 3, 4 };
        for (int i = 0; i < 3; i++) goals[i] = EditorGUILayout.IntSlider(new[] { "쉬움 (1학년)", "보통 (2학년)", "어려움 (3학년)" }[i], goals[i], 2, 8);

        Head("요구사항 하나당 남은 시간 (초)");
        var req = (s.requestTime != null && s.requestTime.Length == 3) ? (float[])s.requestTime.Clone() : new[] { 40f, 30f, 22f };
        for (int i = 0; i < 3; i++) req[i] = EditorGUILayout.IntSlider(new[] { "쉬움", "보통", "어려움" }[i], Mathf.RoundToInt(req[i]), 10, 90);

        if (EditorGUI.EndChangeCheck())
        {
            Record("정보 수정");
            s.displayName = name; s.summary = summary;
            s.timeLimit = Mathf.Max(10, min * 60 + sec);
            s.terminalGoal = goals; s.requestTime = req;
        }

        Head("벌 공격 패턴 (켠 것만 순서대로 반복)");
        if (s.monsters != null)
            for (int m = 0; m < s.monsters.Length; m++)
            {
                if (s.monsters.Length > 1) GUILayout.Label($"벌 {m + 1}", EditorStyles.miniBoldLabel);
                var sp = s.monsters[m];
                int mask = sp.patternMask != 0 ? sp.patternMask : (1 << Mathf.Max(1, sp.patternCount)) - 1;
                int nm = 0;
                for (int b = 0; b < Monster1A.PatternNames.Length; b++)
                {
                    bool on = (mask & (1 << b)) != 0;
                    var prev = GUI.backgroundColor; GUI.backgroundColor = on ? new Color(1f, 0.8f, 0.3f) : Color.white;
                    if (GUILayout.Toggle(on, Monster1A.PatternNames[b], "Button", GUILayout.Height(26))) nm |= 1 << b;
                    GUI.backgroundColor = prev;
                }
                if (nm == 0) nm = 1;
                if (nm != mask) { Record("패턴"); sp.patternMask = nm; sp.patternCount = Bits(nm); }
            }

        DrawLines("시작 대사", ref s.intro);
        DrawLines("끝 대사", ref s.outro);

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    void DrawLines(string head, ref DialogueLine[] lines)
    {
        Head(head);
        var list = (lines ?? new DialogueLine[0]).ToList();
        int remove = -1;
        for (int i = 0; i < list.Count; i++)
        {
            GUILayout.BeginHorizontal();
            var d = list[i];
            EditorGUI.BeginChangeCheck();
            string sp = EditorGUILayout.TextField(d.speaker ?? "", big, GUILayout.Width(80), GUILayout.Height(26));
            string tx = EditorGUILayout.TextArea(d.text ?? "", big, GUILayout.Height(40));
            if (EditorGUI.EndChangeCheck()) { Record("대사"); d.speaker = sp; d.text = tx; list[i] = d; lines = list.ToArray(); }
            if (GUILayout.Button("✕", GUILayout.Width(26), GUILayout.Height(26))) remove = i;
            GUILayout.EndHorizontal();
        }
        if (remove >= 0) { Record("대사 삭제"); list.RemoveAt(remove); lines = list.ToArray(); }
        if (GUILayout.Button("＋ 줄 추가", GUILayout.Height(26))) { Record("대사 추가"); list.Add(new DialogueLine { speaker = list.Count > 0 ? list[list.Count - 1].speaker : "나", text = "" }); lines = list.ToArray(); }
    }

    void Head(string t) { GUILayout.Space(8); GUILayout.Label(t, bigBold); }

    // =====================================================================================
    // 동작
    // =====================================================================================
    void Select(StageData s) { selected = s; selectedPath = s != null ? AssetDatabase.GetAssetPath(s) : null; GUI.FocusControl(null); }

    void Record(string what) { if (selected != null) { Undo.RecordObject(selected, what); EditorUtility.SetDirty(selected); } }

    static int Bits(int m) { int c = 0; while (m != 0) { c += m & 1; m >>= 1; } return c; }

    void EnsureLayout(StageData s)
    {
        if (s.layout == null || !s.layout.use || s.layout.walls == null || s.layout.walls.Count == 0)
        {
            s.layout = StageLayoutDefaults.Default();
            EditorUtility.SetDirty(s);
        }
        if (s.layout.terminals == null || s.layout.terminals.Length == 0) s.layout.terminals = new[] { new Vector2(10.5f, 2f) };
    }

    /// <summary>방 크기 변경: 옛 바깥 벽과 방 밖 벽을 지우고 새 바깥 벽을 깖. 물체는 그대로 (경고로 알려 줌)</summary>
    void ResizeRoom(int w, int h)
    {
        w = Mathf.Clamp(w + (w & 1), 16, 40); h = Mathf.Clamp(h + (h & 1), 10, 22);
        var L = selected.layout;
        if (w == L.width && h == L.height) return;
        Record("방 크기");
        var oldBorder = new HashSet<Vector2Int>(WallBuilder.Border(L.width, L.height));
        L.walls = L.walls.Where(c => !oldBorder.Contains(c) && WallBuilder.InRoom(c, w, h)).ToList();
        L.walls.AddRange(WallBuilder.Border(w, h).Where(c => !L.walls.Contains(c)));
        L.width = w; L.height = h;
    }

    void NewStage(bool duplicate)
    {
        int c = chapterTab + 1;
        var all = LoadAll();
        int n = Enumerable.Range(1, PerChapter).FirstOrDefault(k => !all.Any(x => x.chapter == c && x.stage == k));
        if (n == 0) { EditorUtility.DisplayDialog("Stage Maker", $"{c}학년 스테이지 10칸이 모두 찼어요.", "확인"); return; }
        CreateAt(c, n, duplicate ? selected : null);
    }

    void CreateAt(int chapter, int stage, StageData template)
    {
        var all = LoadAll();
        if (template == null) template = all.Where(s => s.chapter == chapter && s.stage < stage).OrderByDescending(s => s.stage).FirstOrDefault();
        StageData s = template != null ? Instantiate(template) : CreateInstance<StageData>();
        s.chapter = chapter; s.stage = stage;
        s.displayName = $"{chapter}-{stage}";
        if (template == null || template != selected) { s.intro = new DialogueLine[0]; s.outro = new DialogueLine[0]; s.summary = ""; }
        if (s.monsters == null || s.monsters.Length == 0)
            s.monsters = new[] { new MonsterSpawn { prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BeePrefab), patternCount = 1, patternMask = 1, spawnPos = new Vector2(3f, 3f) } };
        if (s.layout == null || !s.layout.use || s.layout.walls.Count == 0) s.layout = StageLayoutDefaults.Default();
        string path = AssetDatabase.GenerateUniqueAssetPath($"{DataFolder}/Stage_{chapter}_{stage}.asset");
        AssetDatabase.CreateAsset(s, path);
        AssetDatabase.SaveAssets();
        Register(s, true);
        Select(AssetDatabase.LoadAssetAtPath<StageData>(path));
    }

    void DeleteStage()
    {
        if (selected == null) return;
        if (!EditorUtility.DisplayDialog("스테이지 삭제", $"{selected.chapter}-{selected.stage} \"{selected.displayName}\"을(를) 지울까요?\n되돌릴 수 없어요.", "삭제", "취소")) return;
        var path = AssetDatabase.GetAssetPath(selected);
        Register(selected, false);
        AssetDatabase.DeleteAsset(path);
        Select(null);
    }

    void AutoGenerate()
    {
        Record("자동 생성");
        int terms = Mathf.Clamp(selected.layout?.terminals?.Length ?? 2, 1, 4);
        selected.layout = StageLayoutDefaults.Random(autoSeed++ * 7919 + selected.stage, terms, selected.layout.width, selected.layout.height, out var spawn);
        SetBee(0, spawn);
    }

    static void Save(StageData s)
    {
        if (s == null) return;
        EditorUtility.SetDirty(s);
        AssetDatabase.SaveAssetIfDirty(s);
        Register(s, true);
    }

    static void Play(StageData s)
    {
        Save(s);
        if (SceneManager.GetActiveScene().path != StageScenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(StageScenePath, OpenSceneMode.Single);
        }
        SessionState.SetString(StageManager.EditorPlayStageKey, AssetDatabase.GetAssetPath(s));
        EditorApplication.EnterPlaymode();
    }

    static StageData[] LoadAll()
        => AssetDatabase.FindAssets("t:StageData", new[] { DataFolder })
            .Select(g => AssetDatabase.LoadAssetAtPath<StageData>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(s => s != null).OrderBy(s => s.chapter).ThenBy(s => s.stage).ToArray();

    static StageData[] Registered()
    {
        var text = System.IO.File.Exists(TitleScenePath) ? System.IO.File.ReadAllText(TitleScenePath) : "";
        int i = text.IndexOf("allStages:");
        if (i < 0) return new StageData[0];
        var list = new List<StageData>();
        foreach (var line in text.Substring(i).Split('\n').Skip(1))
        {
            var t = line.Trim();
            if (!t.StartsWith("- {fileID")) break;
            int g = t.IndexOf("guid: "); if (g < 0) continue;
            var s = AssetDatabase.LoadAssetAtPath<StageData>(AssetDatabase.GUIDToAssetPath(t.Substring(g + 6, 32)));
            if (s != null) list.Add(s);
        }
        return list.ToArray();
    }

    /// <summary>Title 씬 GameManager.allStages에 넣기/빼기 → 맵 화면에 표시/숨김</summary>
    static void Register(StageData s, bool add)
    {
        if (add && Registered().Contains(s)) return;
        var scene = SceneManager.GetSceneByPath(TitleScenePath);
        bool opened = false;
        if (!scene.isLoaded) { scene = EditorSceneManager.OpenScene(TitleScenePath, OpenSceneMode.Additive); opened = true; }
        var gm = scene.GetRootGameObjects().Select(r => r.GetComponentInChildren<GameManager>(true)).FirstOrDefault(x => x != null);
        if (gm != null)
        {
            var gso = new SerializedObject(gm);
            var arr = gso.FindProperty("allStages");
            for (int i = arr.arraySize - 1; i >= 0; i--)
            {
                var v = arr.GetArrayElementAtIndex(i).objectReferenceValue;
                if (v == null || v == s) { arr.GetArrayElementAtIndex(i).objectReferenceValue = null; arr.DeleteArrayElementAtIndex(i); }
            }
            if (add) { arr.InsertArrayElementAtIndex(arr.arraySize); arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = s; }
            gso.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        if (opened) EditorSceneManager.CloseScene(scene, true);
    }

    // =====================================================================================
    // 그리기 도구
    // =====================================================================================
    static int GW = WallBuilder.DefaultW, GH = WallBuilder.DefaultH;   // 지금 그리는 방 크기
    static Rect CellRect(Rect grid, float cs, Vector2Int c) => new Rect(grid.x + (c.x - WallBuilder.MinX(GW)) * cs, grid.y + (WallBuilder.MaxY(GH) - c.y) * cs, cs, cs);
    static Rect WorldRect(Rect grid, float cs, Vector2 center, Vector2 size)
        => new Rect(grid.x + (center.x - size.x / 2f - WallBuilder.MinX(GW)) * cs, grid.y + (WallBuilder.MaxY(GH) + 1 - center.y - size.y / 2f) * cs, size.x * cs, size.y * cs);

    static void DrawSprite(Rect r, Sprite s, float angle)
    {
        var tex = s.texture;
        var uv = new Rect(s.textureRect.x / tex.width, s.textureRect.y / tex.height, s.textureRect.width / tex.width, s.textureRect.height / tex.height);
        if (Mathf.Approximately(angle, 0f)) { GUI.DrawTextureWithTexCoords(r, tex, uv); return; }
        var m = GUI.matrix;
        GUIUtility.RotateAroundPivot(-angle, r.center);
        GUI.DrawTextureWithTexCoords(r, tex, uv);
        GUI.matrix = m;
    }

    Texture2D ToolThumb(Tool t)
    {
        switch (t)
        {
            case Tool.Wall: return Thumb("wall");
            case Tool.Erase: return null;
            case Tool.Move: return null;
            case Tool.Planning: return Thumb("plan");
            case Tool.Programming: return Thumb("programming");
            case Tool.Graphic: return Thumb("graphic");
            case Tool.QA: return Thumb("qa");
            case Tool.Crafter: return Thumb("crafter");
            case Tool.Terminal: return Thumb("terminal");
            case Tool.Player: return Thumb("player");
            default: return Thumb("bee");
        }
    }

    Texture2D Thumb(string key)
    {
        if (thumbs.TryGetValue(key, out var t) && t != null) return t;
        string path = key switch
        {
            "plan" => "Assets/DebugRush/Art/Facilities/generator_plan.png",
            "programming" => "Assets/DebugRush/Art/Facilities/generator_programming.png",
            "graphic" => "Assets/DebugRush/Art/Facilities/generator_graphic.png",
            "qa" => "Assets/DebugRush/Art/Facilities/generator_qa.png",
            "crafter" => "Assets/DebugRush/Art/Placeholder/crafter_idle.png",
            "terminal" => "Assets/DebugRush/Art/Facilities/terminal_error.png",
            "player" => "Assets/DebugRush/Art/Drive/Player/run_R_01.png",
            "bee" => "Assets/DebugRush/Art/Drive/Monster1/move_01.png",
            _ => "Assets/DebugRush/Art/Placeholder/wall_single.png",
        };
        t = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        thumbs[key] = t;
        return t;
    }

    void Styles()
    {
        if (big != null) return;
        big = new GUIStyle(EditorStyles.textField) { fontSize = 14, wordWrap = true };
        bigBold = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 };
        title = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
        warn = new GUIStyle(EditorStyles.label) { fontSize = 13, normal = { textColor = new Color(1f, 0.4f, 0.4f) } };
        ok = new GUIStyle(EditorStyles.label) { fontSize = 13, normal = { textColor = new Color(0.4f, 0.9f, 0.5f) } };
        cardOn = new GUIStyle(GUI.skin.button) { fontSize = 14, alignment = TextAnchor.MiddleLeft, richText = true, normal = { textColor = new Color(0.6f, 1f, 0.7f) } };
        cardOff = new GUIStyle(GUI.skin.button) { fontSize = 14, alignment = TextAnchor.MiddleLeft, richText = true, normal = { textColor = new Color(0.6f, 0.6f, 0.65f) } };
        cardSel = new GUIStyle(GUI.skin.button) { fontSize = 14, alignment = TextAnchor.MiddleLeft, richText = true, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.4f, 0.9f, 1f) } };
    }

    // ---------------- 시연용: 툴 기능으로 1-6 만들기 + 창 캡처 ----------------
    /// <summary>툴 버튼과 같은 함수로 1-6 작성: 비어 있으면 새로 만들기 → 자동 생성 → 벽 몇 줄 칠하기 → 정보 입력 → 저장</summary>
    public static void MakeDemo16()
    {
        var w = CreateInstance<StageMakerWindow>();
        w.chapterTab = 0;
        var s = LoadAll().FirstOrDefault(x => x.chapter == 1 && x.stage == 6);
        if (s == null) w.CreateAt(1, 6, null); else w.Select(s);
        s = w.selected;
        w.EnsureLayout(s);
        w.autoSeed = 3;
        s.layout.terminals = new[] { Vector2.zero, Vector2.zero, Vector2.zero };   // 단말기 3대
        w.AutoGenerate();                                   // [자동 생성]
        var boxes = w.Objects().Select(w.Box).ToList();
        // [벽] 칠하기: 위쪽 가로 칸막이 한 줄 (물체와 겹치면 통째로 생략)
        var line = Enumerable.Range(-3, 6).Select(x => new Vector2Int(x, 3)).ToList();
        if (!line.Any(c => boxes.Any(b => b.Overlaps(new Rect(c.x - 0.5f, c.y - 0.5f, 2f, 2f))))) s.layout.walls.AddRange(line.Where(c => !s.layout.walls.Contains(c)));
        s.displayName = "마감 전날 밤";
        s.summary = "단말기 3대가 동시에 터진다. 벌은 전 패턴 사용.";
        s.timeLimit = 100;
        s.terminalGoal = new[] { 4, 5, 6 };
        s.monsters[0].patternMask = 31; s.monsters[0].patternCount = 5;
        s.intro = new[] { new DialogueLine { speaker = "나", text = "내일이 발표인데… 단말기가 셋 다 빨갛다." }, new DialogueLine { speaker = "나", text = "벌이 또 나타났다. 빨리 고치자!" } };
        s.outro = new[] { new DialogueLine { speaker = "나", text = "겨우 살렸다. 이제 진짜 자자." } };
        foreach (var (msg, bad) in w.Checks()) Debug.Log($"[StageMaker] check: {(bad ? "BAD " : "OK ")}{msg}");
        Save(s);
        DestroyImmediate(w);
        Debug.Log("[StageMaker] 1-6 made");
    }

    /// <summary>에디터(GUI)로 실행: 창을 띄워 1-6을 고른 뒤 화면을 PNG로 저장하고 종료. 인자 -smshot 경로</summary>
    public static void CaptureWindow()
    {
        var args = System.Environment.GetCommandLineArgs();
        int i = System.Array.IndexOf(args, "-smshot");
        string outPath = i >= 0 && i + 1 < args.Length ? args[i + 1] : "StageMaker.png";
        var w = GetWindow<StageMakerWindow>("Stage Maker");
        w.position = new Rect(40, 40, 1600, 900);
        var s = LoadAll().FirstOrDefault(x => x.chapter == 1 && x.stage == 6) ?? LoadAll().FirstOrDefault();
        w.Select(s);
        w.tool = Tool.Wall;
        double until = EditorApplication.timeSinceStartup + 6;
        EditorApplication.update += Tick;
        void Tick()
        {
            w.Repaint();
            if (EditorApplication.timeSinceStartup < until) return;
            EditorApplication.update -= Tick;
            var p = w.position;
            var px = UnityEditorInternal.InternalEditorUtility.ReadScreenPixel(new Vector2(p.x, p.y), (int)p.width, (int)p.height);
            var tex = new Texture2D((int)p.width, (int)p.height, TextureFormat.RGBA32, false);
            tex.SetPixels(px); tex.Apply();
            System.IO.File.WriteAllBytes(outPath, tex.EncodeToPNG());
            Debug.Log("[StageMaker] captured " + outPath);
            EditorApplication.Exit(0);
        }
    }

    // ---------------- 배치(batch) 검증·변환용 ----------------
    /// <summary>1-1~1-5를 맵 데이터(벽 포함) 방식으로 변환</summary>
    /// <summary>모든 스테이지 배치 경고 출력</summary>
    public static void CheckAll()
    {
        var w = CreateInstance<StageMakerWindow>();
        foreach (var s in LoadAll())
        {
            w.selected = s;
            foreach (var (msg, bad) in w.Checks()) Debug.Log($"[StageMaker] {s.chapter}-{s.stage} {s.layout.width}x{s.layout.height}: {(bad ? "BAD " : "OK ")}{msg}");
        }
        DestroyImmediate(w);
    }

    public static void ConvertExisting()
    {
        foreach (var s in LoadAll())
        {
            bool oldSize = s.layout != null && s.layout.walls != null && s.layout.walls.Contains(new Vector2Int(-14, -6)) && s.layout.walls.Contains(new Vector2Int(13, 5)) && !s.layout.walls.Contains(new Vector2Int(WallBuilder.MinX(s.layout.width), WallBuilder.MinY(s.layout.height)));   // 28×12 시절 데이터
            if (s.layout == null || !s.layout.use || s.layout.walls == null || s.layout.walls.Count == 0 || oldSize)
            {
                s.layout = StageLayoutDefaults.Default();
                EditorUtility.SetDirty(s);
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[StageMaker] converted");
    }
}
