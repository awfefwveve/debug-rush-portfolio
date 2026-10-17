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

    enum Tool { Move, Wall, Floor, Erase, Planning, Programming, Graphic, QA, Crafter, Terminal, Player, Bee }

    static readonly (Tool tool, string label, string tip)[] Palette =
    {
        (Tool.Move, "이동", "물체를 끌어서 옮기기"),
        (Tool.Wall, "벽", "클릭·드래그로 벽 칠하기"),
        (Tool.Floor, "바닥", "클릭·드래그로 바닥 칠하기 (바닥 없는 칸은 게임에서 빈 공간, 못 들어감)"),
        (Tool.Erase, "지우개", "물체 → 벽 → 바닥 순으로 지우기 (우클릭도 지우기)"),
        (Tool.Planning, "기획", "기획 재료대 추가 (클릭마다 하나)"),
        (Tool.Programming, "프로그래밍", "프로그래밍 재료대 추가 (클릭마다 하나)"),
        (Tool.Graphic, "그래픽", "그래픽 재료대 추가 (클릭마다 하나)"),
        (Tool.QA, "QA", "QA 재료대 추가 (클릭마다 하나)"),
        (Tool.Crafter, "제작대", "제작대 추가 (클릭마다 하나)"),
        (Tool.Terminal, "단말기", "단말기 추가 (클릭마다 하나)"),
        (Tool.Player, "시작점", "플레이어 시작 위치"),
        (Tool.Bee, "벌", "벌 추가. 놓인 벌을 클릭하면 그 벌의 공격 패턴을 오른쪽에서 편집"),
    };

    StageData selected;
    string selectedPath;
    int chapterTab;
    Tool tool = Tool.Wall;
    Vector2 rightScroll, leftScroll;
    int autoSeed = 1;
    int selBee = -1;           // 오른쪽에서 패턴 편집 중인 벌

    // 드래그 상태
    string dragging;           // "station:0", "crafter:0", "terminal:1", "player", "bee:0" …
    bool painting, erasing, flooring;
    int eraseMode;             // 한 번 드래그하는 동안 지우는 대상 고정: 0 미정, 1 벽, 2 바닥

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
        GUILayout.Space(16);
        if (Btn("내보내기", 80)) ExportStages();
        if (Btn("가져오기", 80)) ImportStages();
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
        // 빈칸(바닥 없음) = 어두운 배경, 바닥 칸 = 바닥 그림
        EditorGUI.DrawRect(grid, new Color(0.03f, 0.04f, 0.07f));
        var L = selected.layout;
        var floorTex = Thumb("floor");
        foreach (var c in L.FloorSet())
        {
            var fr = CellRect(grid, cs, c);
            if (floorTex != null) GUI.DrawTexture(fr, floorTex); else EditorGUI.DrawRect(fr, new Color(0.09f, 0.11f, 0.17f));
        }
        for (int x = 0; x <= GW; x++) EditorGUI.DrawRect(new Rect(grid.x + x * cs, grid.y, 1, grid.height), new Color(1, 1, 1, 0.05f));
        for (int y = 0; y <= GH; y++) EditorGUI.DrawRect(new Rect(grid.x, grid.y + y * cs, grid.width, 1), new Color(1, 1, 1, 0.05f));

        var wallSet = new HashSet<Vector2Int>(L.walls);
        var fx = FxSet.Get();
        foreach (var c in wallSet)
        {
            WallBuilder.Piece(wallSet, c, out var spr, out float ang, fx);
            var cell = CellRect(grid, cs, c);
            if (spr != null) DrawSprite(cell, spr, ang); else EditorGUI.DrawRect(cell, new Color(0.35f, 0.42f, 0.58f));
        }

        // 보유 자료 칸에 가려지는 구역 (물체 금지)
        var hz = StageLayoutDefaults.HudZone(selected.layout.PaintedBounds());
        var hzr = WorldRect(grid, cs, hz.center, hz.size);
        EditorGUI.DrawRect(hzr, new Color(1f, 0.35f, 0.35f, 0.22f));
        GUI.Label(hzr, "보유 자료\n(놓지 말 것)", EditorStyles.centeredGreyMiniLabel);

        // 물체
        foreach (var o in Objects()) DrawObject(grid, cs, o);

        HandleMouse(area, grid, cs);

        // 경고
        var y0 = grid.yMax + 8;
        if (OutOfRoom().Any())
        {
            if (GUI.Button(new Rect(r.x + 12, y0, 200, 24), "방 밖 물체를 방 안으로 모으기")) GatherIntoRoom();
            y0 += 28;
        }
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
        for (int i = 0; i < L.stations.Count; i++)
            yield return O("station:" + i, MatName(L.stations[i].material), L.stations[i].pos, StageLayoutDefaults.StationSize, MatThumb(L.stations[i].material));
        for (int i = 0; i < L.crafters.Count; i++) yield return O("crafter:" + i, "제작대", L.crafters[i], StageLayoutDefaults.CrafterSize, "crafter");
        for (int i = 0; i < L.terminals.Length; i++) yield return O("terminal:" + i, $"단말기 {i + 1}", L.terminals[i], StageLayoutDefaults.TerminalSize, "terminal");
        var p = O("player", "시작점", L.playerStart + new Vector2(0f, 1f), new Vector2(2f, 2f), "player"); p.feet = true; yield return p;
        if (selected.monsters != null)
            for (int i = 0; i < selected.monsters.Length; i++) yield return O("bee:" + i, $"벌 {i + 1}", selected.monsters[i].spawnPos, new Vector2(2f, 2f), "bee");
    }

    static readonly ItemType[] Materials = { ItemType.Planning, ItemType.Programming, ItemType.Graphic, ItemType.QA };
    static string MatName(ItemType t) => t == ItemType.Planning ? "기획" : t == ItemType.Programming ? "프로그래밍" : t == ItemType.Graphic ? "그래픽" : "QA";
    static string MatThumb(ItemType t) => t == ItemType.Planning ? "plan" : t == ItemType.Programming ? "programming" : t == ItemType.Graphic ? "graphic" : "qa";
    static ItemType? ToolMaterial(Tool t) => t == Tool.Planning ? ItemType.Planning : t == Tool.Programming ? ItemType.Programming : t == Tool.Graphic ? ItemType.Graphic : t == Tool.QA ? ItemType.QA : (ItemType?)null;

    Obj O(string id, string label, Vector2 pos, Vector2 size, string thumb) => new Obj { id = id, label = label, pos = pos, size = size, tex = Thumb(thumb) };

    void DrawObject(Rect grid, float cs, Obj o)
    {
        var rect = WorldRect(grid, cs, o.pos, o.size);
        if (dragging == o.id || o.id == "bee:" + selBee) EditorGUI.DrawRect(rect, new Color(0.3f, 0.9f, 1f, 0.25f));
        if (o.tex != null) GUI.DrawTexture(rect, o.tex, ScaleMode.ScaleToFit);
        else EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.6f, 0.6f));
        var lr = new Rect(rect.x - 20, rect.yMax - 2, rect.width + 40, 18);
        EditorGUI.DrawRect(new Rect(lr.center.x - 40, lr.y, 80, 18), new Color(0, 0, 0, 0.55f));
        GUI.Label(lr, o.label, title);
    }

    // ---------------- 마우스 ----------------
    // 격자 밖(방 밖)에 놓인 물체도 집고·옮기고·지울 수 있게 가운데 영역 전체에서 받음
    void HandleMouse(Rect area, Rect grid, float cs)
    {
        var e = Event.current;
        if (!area.Contains(e.mousePosition) && e.type != EventType.MouseUp) return;
        Vector2 world = new Vector2(WallBuilder.MinX(GW) + (e.mousePosition.x - grid.x) / cs, WallBuilder.MaxY(GH) + 1 - (e.mousePosition.y - grid.y) / cs);
        var cell = new Vector2Int(Mathf.FloorToInt(world.x), Mathf.FloorToInt(world.y));
        Vector2 snap = new Vector2(StageLayoutDefaults.Snap(world.x), StageLayoutDefaults.Snap(world.y));
        var L = selected.layout;

        if (e.type == EventType.MouseDown)
        {
            if (e.button == 1) { erasing = true; eraseMode = 0; EraseAt(world, cell); e.Use(); return; }
            if (e.button != 0) return;
            var hit = HitObject(world);
            var mat = ToolMaterial(tool);
            // 같은 종류 물체를 누르면 새로 놓지 않고 그걸 집음
            bool sameKind = hit != null && (tool == Tool.Move
                || (mat != null && hit.StartsWith("station:") && L.stations[Index(hit)].material == mat)
                || (tool == Tool.Crafter && hit.StartsWith("crafter:"))
                || (tool == Tool.Terminal && hit.StartsWith("terminal:"))
                || (tool == Tool.Bee && hit.StartsWith("bee:"))
                || (tool == Tool.Player && hit == "player"));
            if (sameKind)
            {
                Record("이동"); dragging = hit;
                if (hit.StartsWith("bee:")) { selBee = Index(hit); GUI.FocusControl(null); }
                e.Use(); return;
            }
            // 물체는 바닥 칸 위에만
            bool placing = mat != null || tool == Tool.Crafter || tool == Tool.Terminal || tool == Tool.Bee || tool == Tool.Player;
            if (placing && !OnFloor(tool == Tool.Player ? snap - new Vector2(0f, 0.5f) : snap)) { ShowNotification(new GUIContent("바닥 위에만 놓을 수 있어요")); e.Use(); return; }
            switch (tool)
            {
                case Tool.Move: if (hit == null) selBee = -1; break;
                case Tool.Wall: Record("벽"); painting = true; if (WallBuilder.InRoom(cell, GW, GH) && !L.walls.Contains(cell)) L.walls.Add(cell); break;
                case Tool.Floor: Record("바닥"); flooring = true; L.MakeFloorExplicit(); if (WallBuilder.InRoom(cell, GW, GH) && !L.floor.Contains(cell)) L.floor.Add(cell); break;
                case Tool.Erase: erasing = true; eraseMode = 0; EraseAt(world, cell); break;
                case Tool.Planning: case Tool.Programming: case Tool.Graphic: case Tool.QA:
                    Record("재료대"); L.stations.Add(new StationSpot { material = mat.Value, pos = snap }); dragging = "station:" + (L.stations.Count - 1); break;
                case Tool.Crafter: Record("제작대"); L.crafters.Add(snap); dragging = "crafter:" + (L.crafters.Count - 1); break;
                case Tool.Player: Record("시작점"); L.playerStart = snap - new Vector2(0f, 1f); dragging = "player"; break;
                case Tool.Terminal: Record("단말기"); L.terminals = L.terminals.Concat(new[] { snap }).ToArray(); dragging = "terminal:" + (L.terminals.Length - 1); break;
                case Tool.Bee:
                    int count = selected.monsters?.Length ?? 0;
                    Record("벌"); SetBee(count, snap); dragging = "bee:" + count; selBee = count; GUI.FocusControl(null); break;
            }
            e.Use();
        }
        else if (e.type == EventType.MouseDrag)
        {
            if (erasing) EraseAt(world, cell);
            else if (painting && WallBuilder.InRoom(cell, GW, GH) && !L.walls.Contains(cell)) L.walls.Add(cell);
            else if (flooring && WallBuilder.InRoom(cell, GW, GH) && !L.floor.Contains(cell)) L.floor.Add(cell);
            else if (dragging != null && OnFloor(dragging == "player" ? snap - new Vector2(0f, 0.5f) : snap)) MoveObject(dragging, snap);
            e.Use();
        }
        else if (e.type == EventType.MouseUp)
        {
            if (painting || erasing || flooring || dragging != null) EditorUtility.SetDirty(selected);
            painting = erasing = flooring = false; dragging = null;
            e.Use();
        }
    }

    static int Index(string id) => int.Parse(id.Substring(id.IndexOf(':') + 1));

    /// <summary>마우스 위치의 물체 (위에 그려진 것 우선)</summary>
    string HitObject(Vector2 world)
    {
        foreach (var o in Objects().Reverse())
        {
            var box = o.id.StartsWith("bee") || o.id == "player" ? new Rect(o.pos - o.size / 2f, o.size) : Box(o);
            if (box.Contains(world)) return o.id;
        }
        return null;
    }

    /// <summary>그 점이 바닥 칸(벽 아님) 위인지</summary>
    bool OnFloor(Vector2 p)
    {
        var c = new Vector2Int(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y));
        var L = selected.layout;
        return L.FloorSet().Contains(c) && !L.walls.Contains(c);
    }

    /// <summary>물체가 차지하는 칸들</summary>
    static IEnumerable<Vector2Int> Cells(Rect r)
    {
        for (int x = Mathf.FloorToInt(r.xMin + 0.05f); x <= Mathf.FloorToInt(r.xMax - 0.05f); x++)
            for (int y = Mathf.FloorToInt(r.yMin + 0.05f); y <= Mathf.FloorToInt(r.yMax - 0.05f); y++)
                yield return new Vector2Int(x, y);
    }

    /// <summary>시작점에서 걸어서 갈 수 있는 칸 (바닥 − 벽 − 물체)</summary>
    HashSet<Vector2Int> Reachable(HashSet<Vector2Int> floor, HashSet<Vector2Int> walls, List<Obj> objs)
    {
        var blocked = new HashSet<Vector2Int>(walls);
        foreach (var o in objs.Where(o => o.id != "player" && !o.id.StartsWith("bee"))) foreach (var c in Cells(Box(o))) blocked.Add(c);
        var start = new Vector2Int(Mathf.FloorToInt(selected.layout.playerStart.x), Mathf.FloorToInt(selected.layout.playerStart.y + 0.5f));
        var seen = new HashSet<Vector2Int>();
        if (!floor.Contains(start) || blocked.Contains(start)) return seen;
        var q = new Queue<Vector2Int>(); q.Enqueue(start); seen.Add(start);
        var dirs = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        while (q.Count > 0)
        {
            var c = q.Dequeue();
            foreach (var d in dirs)
            {
                var n = c + d;
                if (seen.Contains(n) || !floor.Contains(n) || blocked.Contains(n)) continue;
                seen.Add(n); q.Enqueue(n);
            }
        }
        return seen;
    }

    void MoveObject(string id, Vector2 p)
    {
        var L = selected.layout;
        if (id == "player") L.playerStart = p - new Vector2(0f, 1f);
        else if (id.StartsWith("station:")) L.stations[Index(id)].pos = p;
        else if (id.StartsWith("crafter:")) L.crafters[Index(id)] = p;
        else if (id.StartsWith("terminal:")) L.terminals[Index(id)] = p;
        else if (id.StartsWith("bee:")) selected.monsters[Index(id)].spawnPos = p;
    }

    /// <summary>마우스 아래 물체 하나를 지움 (시작점 제외). 물체가 없으면 벽 → 바닥 순으로 지움</summary>
    void EraseAt(Vector2 world, Vector2Int cell)
    {
        var L = selected.layout;
        Record("지우기");
        var hit = HitObject(world);
        if (hit != null && hit != "player")
        {
            int i = Index(hit);
            if (hit.StartsWith("station:")) L.stations.RemoveAt(i);
            else if (hit.StartsWith("crafter:")) L.crafters.RemoveAt(i);
            else if (hit.StartsWith("terminal:")) L.terminals = L.terminals.Where((t, k) => k != i).ToArray();
            else if (hit.StartsWith("bee:")) { selected.monsters = selected.monsters.Where((m, k) => k != i).ToArray(); selBee = -1; }
            erasing = false;              // 드래그로 여러 개가 한꺼번에 지워지지 않게
            return;
        }
        if (eraseMode == 0) eraseMode = L.walls.Contains(cell) ? 1 : 2;
        if (eraseMode == 1) L.walls.Remove(cell);
        else { L.MakeFloorExplicit(); L.floor.Remove(cell); }
    }

    void SetBee(int index, Vector2 pos)
    {
        var list = (selected.monsters ?? new MonsterSpawn[0]).ToList();
        int mask = selected.newMonsterPatternMask != 0 ? selected.newMonsterPatternMask : 1;
        while (list.Count <= index)
            list.Add(new MonsterSpawn { prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BeePrefab), patternCount = Bits(mask), patternMask = mask });
        list[index].spawnPos = pos;
        selected.monsters = list.ToArray();
    }

    /// <summary>방 밖으로 나간 물체 이름들</summary>
    IEnumerable<string> OutOfRoom()
    {
        float x0 = WallBuilder.MinX(GW) + 1, x1 = WallBuilder.MaxX(GW), y0 = WallBuilder.MinY(GH) + 1, y1 = WallBuilder.MaxY(GH);
        foreach (var o in Objects())
        {
            var b = o.id == "player" ? Box(o) : new Rect(o.pos - o.size / 2f, o.size);
            if (b.xMin < x0 - 0.01f || b.xMax > x1 + 0.01f || b.yMin < y0 - 0.01f || b.yMax > y1 + 0.01f) yield return o.id;
        }
    }

    /// <summary>방 밖 물체를 방 안쪽 가장자리로 끌어옴</summary>
    void GatherIntoRoom()
    {
        Record("방 안으로 모으기");
        float x0 = WallBuilder.MinX(GW) + 1, x1 = WallBuilder.MaxX(GW), y0 = WallBuilder.MinY(GH) + 1, y1 = WallBuilder.MaxY(GH);
        foreach (var id in OutOfRoom().ToList())
        {
            var o = Objects().First(x => x.id == id);
            var half = id == "player" ? new Vector2(0.6f, 0.6f) : o.size / 2f;
            var c = id == "player" ? selected.layout.playerStart + new Vector2(0f, 0.5f) : o.pos;
            c.x = Mathf.Clamp(c.x, x0 + half.x, x1 - half.x);
            c.y = Mathf.Clamp(c.y, y0 + half.y, y1 - half.y);
            c = new Vector2(StageLayoutDefaults.Snap(c.x), StageLayoutDefaults.Snap(c.y));
            MoveObject(id, id == "player" ? c + new Vector2(0f, 0.5f) : c);
        }
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
            if (new Rect(center - size / 2f, size).Overlaps(StageLayoutDefaults.HudZone(selected.layout.PaintedBounds())))
            { any = true; yield return ($"{o.label}이(가) 왼쪽 아래 보유 자료 칸에 가려져요 (빗금 구역)", true); }
        }
        for (int i = 0; i < objs.Count; i++)
            for (int j = i + 1; j < objs.Count; j++)
            {
                if (objs[i].id.StartsWith("bee") || objs[j].id.StartsWith("bee")) continue;
                var a = Box(objs[i]); var b = Box(objs[j]);
                if (a.Overlaps(b)) { any = true; yield return ($"{objs[i].label}와(과) {objs[j].label}이(가) 겹쳐 있어요", true); }
            }
        // 바닥 밖 / 시작점에서 못 가는 물체
        var floorSet = L.FloorSet();
        var reach = Reachable(floorSet, w, objs);
        foreach (var o in objs)
        {
            var box = o.id == "player" ? Box(o) : o.id.StartsWith("bee") ? new Rect(o.pos - Vector2.one * 0.5f, Vector2.one) : Box(o);
            if (Cells(box).Any(c => !floorSet.Contains(c))) { any = true; yield return ($"{o.label}이(가) 바닥 밖(빈칸)에 있어요", true); continue; }
            if (o.id == "player" || o.id.StartsWith("bee")) continue;
            var around = new Rect(box.x - 1f, box.y - 1f, box.width + 2f, box.height + 2f);
            if (!Cells(around).Any(reach.Contains)) { any = true; yield return ($"{o.label}에 시작점에서 걸어서 갈 수 없어요", true); }
        }

        // 플레이가 안 되는 경우만 경고 (개수 제한 없음)
        if (L.terminals.Length == 0) { any = true; yield return ("단말기가 없어요 (깰 목표가 없음)", true); }
        if (L.crafters.Count == 0) { any = true; yield return ("제작대가 없어요 (조합 불가)", true); }
        foreach (var m in Materials)
            if (!L.stations.Any(x => x.material == m)) { any = true; yield return ($"{MatName(m)} 재료대가 없어요 (조합표에 필요)", true); }
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

        Head("방 크기 (칸) — 기본 30×16 = 화면에 딱 맞음");
        GUILayout.BeginHorizontal();
        GUILayout.Label("가로", big, GUILayout.Width(36));
        int nw = EditorGUILayout.DelayedIntField(s.layout.width, big, GUILayout.Width(50), GUILayout.Height(26));
        GUILayout.Label("세로", big, GUILayout.Width(36));
        int nh = EditorGUILayout.DelayedIntField(s.layout.height, big, GUILayout.Width(50), GUILayout.Height(26));
        if (GUILayout.Button("기본 30×16", GUILayout.Height(26))) { nw = WallBuilder.DefaultW; nh = WallBuilder.DefaultH; }
        GUILayout.EndHorizontal();
        GUILayout.Label("크기 자유 (8×6 ~ 120×80). 게임에서는 카메라가 그린 맵 전체에 맞춰 자동으로 확대·축소", EditorStyles.miniLabel);
        if (nw != s.layout.width || nh != s.layout.height) ResizeRoom(nw, nh);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("바닥 전부 채우기", GUILayout.Height(24))) { Record("바닥 채우기"); s.layout.customFloor = false; s.layout.floor.Clear(); }
        if (GUILayout.Button("바닥 둘레에 벽 두르기", GUILayout.Height(24))) WallAroundFloor();
        GUILayout.EndHorizontal();

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

        // 몬스터별 공격 패턴: 맵이나 아래 버튼에서 벌을 고르면 그 벌만 편집
        int beeCount = s.monsters?.Length ?? 0;
        if (selBee >= beeCount) selBee = -1;
        Head("몬스터 공격 패턴 (켠 것만 순서대로 반복)");
        if (beeCount > 0)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(selBee < 0, "새 몬스터 기본값", "Button", GUILayout.Height(24))) selBee = -1;
            for (int m = 0; m < beeCount; m++)
                if (GUILayout.Toggle(selBee == m, $"벌 {m + 1}", "Button", GUILayout.Height(24))) selBee = m;
            GUILayout.EndHorizontal();
        }
        if (selBee >= 0)
        {
            var sp = s.monsters[selBee];
            GUILayout.Label($"벌 {selBee + 1} 전용 패턴", EditorStyles.miniBoldLabel);
            int mask = sp.patternMask != 0 ? sp.patternMask : (1 << Mathf.Max(1, sp.patternCount)) - 1;
            int nm = PatternToggles(mask);
            if (nm != mask) { Record("패턴"); sp.patternMask = nm; sp.patternCount = Bits(nm); }
        }
        else
        {
            GUILayout.Label("새로 놓는 벌의 기본 패턴 (맵에서 벌을 클릭하면 그 벌만 따로 편집)", EditorStyles.miniLabel);
            int mask = s.newMonsterPatternMask != 0 ? s.newMonsterPatternMask : 1;
            int nm = PatternToggles(mask);
            if (nm != mask) { Record("기본 패턴"); s.newMonsterPatternMask = nm; }
        }

        DrawLines("시작 대사", ref s.intro);
        DrawLines("끝 대사", ref s.outro);

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    static int PatternToggles(int mask)
    {
        int nm = 0;
        for (int b = 0; b < Monster1A.PatternNames.Length; b++)
        {
            bool on = (mask & (1 << b)) != 0;
            var prev = GUI.backgroundColor; GUI.backgroundColor = on ? new Color(1f, 0.8f, 0.3f) : Color.white;
            if (GUILayout.Toggle(on, Monster1A.PatternNames[b], "Button", GUILayout.Height(26))) nm |= 1 << b;
            GUI.backgroundColor = prev;
        }
        return nm == 0 ? 1 : nm;
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
    void Select(StageData s) { selected = s; selBee = -1; selectedPath = s != null ? AssetDatabase.GetAssetPath(s) : null; GUI.FocusControl(null); }

    void Record(string what) { if (selected != null) { Undo.RecordObject(selected, what); EditorUtility.SetDirty(selected); } }

    static int Bits(int m) { int c = 0; while (m != 0) { c += m & 1; m >>= 1; } return c; }

    void EnsureLayout(StageData s)
    {
        if (s.layout == null || !s.layout.use || s.layout.walls == null || s.layout.walls.Count == 0)
        {
            s.layout = StageLayoutDefaults.Default();
            EditorUtility.SetDirty(s);
        }
        s.layout.Migrate();
    }

    /// <summary>방 크기 변경: 옛 바깥 벽과 방 밖 벽을 지우고 새 바깥 벽을 깖. 물체는 그대로 (경고로 알려 줌)</summary>
    void ResizeRoom(int w, int h)
    {
        w = Mathf.Clamp(w, 8, 120); h = Mathf.Clamp(h, 6, 80);   // 칸 수 제한 없음 (툴 안전선만). 게임 카메라가 맵 전체에 맞춰 줌
        var L = selected.layout;
        if (w == L.width && h == L.height) return;
        Record("방 크기");
        if (L.customFloor)
        {
            // 직접 칠한 바닥: 최대 크기 밖 칸만 정리, 새로 생긴 자리는 빈칸
            L.walls = L.walls.Where(c => WallBuilder.InRoom(c, w, h)).ToList();
            L.floor = L.floor.Where(c => WallBuilder.InRoom(c, w, h)).ToList();
        }
        else
        {
            var oldBorder = new HashSet<Vector2Int>(WallBuilder.Border(L.width, L.height));
            L.walls = L.walls.Where(c => !oldBorder.Contains(c) && WallBuilder.InRoom(c, w, h)).ToList();
            L.walls.AddRange(WallBuilder.Border(w, h).Where(c => !L.walls.Contains(c)));
        }
        L.width = w; L.height = h;
    }

    /// <summary>바닥 칸에 붙어 있는 빈칸(대각선 포함)에 벽을 깖</summary>
    void WallAroundFloor()
    {
        Record("바닥 둘레 벽");
        var L = selected.layout;
        var floor = L.FloorSet();
        var add = new HashSet<Vector2Int>();
        foreach (var c in floor)
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    var n = new Vector2Int(c.x + dx, c.y + dy);
                    if (!floor.Contains(n) && WallBuilder.InRoom(n, L.width, L.height)) add.Add(n);
                }
        foreach (var n in add) if (!L.walls.Contains(n)) L.walls.Add(n);
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
        int terms = Mathf.Max(1, selected.layout?.terminals?.Length ?? 2);
        selected.layout = StageLayoutDefaults.Random(autoSeed++ * 7919 + selected.stage, terms, selected.layout.width, selected.layout.height, out var spawn);
        SetBee(0, spawn);
    }

    // ---------------- 내보내기 / 가져오기 (기획자 ↔ 개발자 스테이지 주고받기) ----------------
    [System.Serializable] class StagePack { public List<string> stages = new List<string>(); }

    void ExportStages()
    {
        int c = chapterTab + 1;
        int pick = EditorUtility.DisplayDialogComplex("스테이지 내보내기", "어떤 스테이지를 파일로 저장할까요?",
            selected != null ? $"선택한 {selected.chapter}-{selected.stage}만" : "선택한 스테이지 (없음)", "취소", $"{c}학년 전체");
        if (pick == 1 || (pick == 0 && selected == null)) return;
        var list = pick == 0 ? new[] { selected } : LoadAll().Where(s => s.chapter == c).ToArray();
        if (list.Length == 0) return;
        string name = pick == 0 ? $"Stage_{selected.chapter}_{selected.stage}" : $"Stages_{c}학년";
        string path = EditorUtility.SaveFilePanel("스테이지 내보내기", "", name, "stage");
        if (string.IsNullOrEmpty(path)) return;
        var pack = new StagePack();
        foreach (var s in list) { s.layout?.Migrate(); pack.stages.Add(EditorJsonUtility.ToJson(s)); }
        System.IO.File.WriteAllText(path, JsonUtility.ToJson(pack, true));
        EditorUtility.RevealInFinder(path);
        ShowNotification(new GUIContent($"{list.Length}개 내보냄"));
    }

    void ImportStages()
    {
        string path = EditorUtility.OpenFilePanel("스테이지 가져오기", "", "stage");
        if (string.IsNullOrEmpty(path)) return;
        var pack = JsonUtility.FromJson<StagePack>(System.IO.File.ReadAllText(path));
        if (pack == null || pack.stages.Count == 0) { EditorUtility.DisplayDialog("가져오기", "스테이지가 들어 있지 않은 파일이에요.", "확인"); return; }
        int added = 0, replaced = 0;
        StageData last = null;
        foreach (var json in pack.stages)
        {
            var tmp = CreateInstance<StageData>();
            EditorJsonUtility.FromJsonOverwrite(json, tmp);
            var existing = LoadAll().FirstOrDefault(x => x.chapter == tmp.chapter && x.stage == tmp.stage);
            if (existing != null)
            {
                int a = EditorUtility.DisplayDialogComplex("같은 번호가 있어요",
                    $"{tmp.chapter}-{tmp.stage} \"{existing.displayName}\"이(가) 이미 있어요.\n가져오는 것: \"{tmp.displayName}\"", "덮어쓰기", "건너뛰기", "빈 번호로 추가");
                if (a == 1) { DestroyImmediate(tmp); continue; }
                if (a == 0)
                {
                    Undo.RecordObject(existing, "가져오기");
                    string keepName = existing.name;
                    EditorJsonUtility.FromJsonOverwrite(json, existing);
                    existing.name = keepName;
                    Save(existing);
                    DestroyImmediate(tmp); replaced++; last = existing; continue;
                }
                int free = Enumerable.Range(1, PerChapter).FirstOrDefault(k => !LoadAll().Any(x => x.chapter == tmp.chapter && x.stage == k));
                if (free == 0) { EditorUtility.DisplayDialog("가져오기", $"{tmp.chapter}학년 10칸이 모두 찼어요. 건너뜀.", "확인"); DestroyImmediate(tmp); continue; }
                tmp.stage = free;
            }
            string p = AssetDatabase.GenerateUniqueAssetPath($"{DataFolder}/Stage_{tmp.chapter}_{tmp.stage}.asset");
            tmp.name = System.IO.Path.GetFileNameWithoutExtension(p);
            AssetDatabase.CreateAsset(tmp, p);
            Register(tmp, true);
            added++; last = tmp;
        }
        AssetDatabase.SaveAssets();
        if (last != null) { chapterTab = Mathf.Clamp(last.chapter - 1, 0, Chapters - 1); Select(last); }
        EditorUtility.DisplayDialog("가져오기", $"추가 {added}개, 덮어쓰기 {replaced}개", "확인");
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
            case Tool.Floor: return Thumb("floor");
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
            "floor" => "Assets/DebugRush/Art/Placeholder/floor_32.png",
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
            w.selected = s; w.EnsureLayout(s);
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
            if (s.layout != null) { s.layout.Migrate(); EditorUtility.SetDirty(s); }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[StageMaker] converted");
    }
}
