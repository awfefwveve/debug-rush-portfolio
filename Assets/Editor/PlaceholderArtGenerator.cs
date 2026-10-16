using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 아트 팀 그림이 없는 오브젝트용 통일 플레이스홀더 생성기 (기준 1칸 = 100px).
/// 메뉴: DebugRush > Generate Placeholder Art. 생성 후 씬/프리팹에 연결까지 수행.
/// 아트 팀 그림이 오면 같은 파일 이름으로 덮어쓰면 그대로 교체됨 (Assets/DebugRush/Art/Placeholder).
/// </summary>
public static class PlaceholderArtGenerator
{
    public const string Dir = "Assets/DebugRush/Art/Placeholder/";
    const string P = "Assets/DebugRush/Prefabs/";
    const int PPU = 100;

    // 팀 팔레트 (사이버펑크 1학년 챕터)
    static readonly Color Navy = Hex("0B1020"), Floor = Hex("141B2E"), FloorLine = Hex("1D2742"),
        Wall = Hex("2A3754"), WallTop = Hex("44597F"), WallDark = Hex("1A2238"),
        Cyan = Hex("39D0FF"), Pink = Hex("FF4FA3"), Yellow = Hex("FFD23F"), Green = Hex("4BE38A"),
        Orange = Hex("FF9F43"), Red = Hex("FF4757"), Body = Hex("223150"), White = Color.white;

    public static readonly Dictionary<ItemType, Color> MaterialColor = new Dictionary<ItemType, Color>
    {
        { ItemType.Planning, Yellow }, { ItemType.Programming, Cyan }, { ItemType.Graphic, Pink }, { ItemType.QA, Green },
    };

    [MenuItem("DebugRush/Generate Placeholder Art")]
    public static void Run()
    {
        Directory.CreateDirectory(Dir);
        GenerateTextures();
        AssetDatabase.Refresh();
        ConfigureImports();
        GenerateLabelIcons();
        AssetDatabase.Refresh();
        ConfigureImports();
        Apply();
        AssetDatabase.SaveAssets();
        Debug.Log("[Placeholder] done");
    }

    // =====================================================================================
    // 텍스처
    // =====================================================================================
    static void GenerateTextures()
    {
        // 바닥 타일
        var t = New(100, 100, Floor);
        Rect(t, 0, 0, 100, 2, FloorLine); Rect(t, 0, 0, 2, 100, FloorLine);
        Rect(t, 48, 48, 4, 4, FloorLine);
        Save(t, "floor");

        // 벽 타일
        t = New(100, 100, Wall);
        Rect(t, 0, 82, 100, 18, WallTop); Rect(t, 0, 0, 100, 10, WallDark);
        Rect(t, 0, 0, 2, 100, WallDark); Rect(t, 49, 10, 2, 72, WallDark);
        Save(t, "wall");

        // 재료대 (책상 + 색 상판)
        foreach (var kv in MaterialColor)
        {
            t = New(120, 100, Clear);
            RoundRect(t, 4, 4, 112, 72, 10, Body); RoundRectOutline(t, 4, 4, 112, 72, 10, 4, kv.Value * 0.8f);
            RoundRect(t, 0, 54, 120, 42, 12, kv.Value); RoundRectOutline(t, 0, 54, 120, 42, 12, 3, Darken(kv.Value, 0.6f));
            Rect(t, 18, 14, 84, 6, Darken(kv.Value, 0.5f));
            Save(t, "station_" + kv.Key.ToString().ToLower());
        }

        // 제작대
        t = New(200, 110, Clear);
        RoundRect(t, 6, 4, 188, 80, 12, Body); RoundRectOutline(t, 6, 4, 188, 80, 12, 4, Orange * 0.8f);
        RoundRect(t, 0, 60, 200, 46, 14, Orange); RoundRectOutline(t, 0, 60, 200, 46, 14, 3, Darken(Orange, 0.6f));
        Circle(t, 40, 30, 10, Darken(Orange, 0.5f)); Circle(t, 160, 30, 10, Darken(Orange, 0.5f));
        Save(t, "crafting_table");

        // 단말기: 모니터 + 오른쪽 남은 시간 칸(6칸). frame t = 켜진 칸 수
        for (int lit = 0; lit <= 6; lit++)
        {
            t = New(130, 150, Clear);
            RoundRect(t, 10, 0, 90, 18, 6, WallDark);                     // 받침
            RoundRect(t, 0, 14, 104, 136, 12, Hex("1B2742"));             // 프레임
            RoundRectOutline(t, 0, 14, 104, 136, 12, 4, Cyan);
            RoundRect(t, 12, 30, 80, 104, 6, Hex("07101F"));              // 화면
            // 화면 속 경고 삼각형
            Triangle(t, 52, 102, 30, lit <= 2 ? Red : Yellow);
            Rect(t, 50, 84, 4, 14, Hex("07101F")); Rect(t, 50, 78, 4, 4, Hex("07101F"));
            // 남은 시간 칸
            RoundRect(t, 108, 14, 22, 136, 6, Hex("1B2742")); RoundRectOutline(t, 108, 14, 22, 136, 6, 3, Cyan * 0.7f);
            for (int i = 0; i < 6; i++)
            {
                bool on = i < lit;
                Color c = !on ? Hex("2A3450") : lit <= 2 ? Red : Cyan;
                Rect(t, 113, 22 + i * 21, 12, 16, c);
            }
            Save(t, "terminal_t" + lit);
        }

        // 로봇 (버그에 지배당한 로봇) + 1B/1C 변형
        Robot("robot", Hex("5A6B8C"), Pink);
        Robot("robot_factory", Hex("8C7A5A"), Orange);
        Robot("robot_scrap", Hex("6B6B6B"), Green);

        // 투사체·위험요소
        Orb("shot_cyan", 56, Cyan); Orb("shot_orange", 64, Orange); Orb("shot_red", 56, Red);
        t = New(90, 40, Clear); Diamond(t, 45, 20, 44, 18, White); Diamond(t, 45, 20, 36, 12, Hex("CFE8FF")); Save(t, "blade");
        t = New(70, 70, Clear); Circle(t, 35, 35, 30, Hex("2B2B35")); CircleOutline(t, 35, 35, 30, 4, Red); Circle(t, 35, 35, 9, Red); Save(t, "mine");
        t = New(100, 100, Clear); CircleOutline(t, 50, 50, 44, 8, Red); Rect(t, 46, 34, 8, 36, Red); Rect(t, 46, 22, 8, 8, Red); Save(t, "warn");
        // 전선: 1×1칸 텍스처를 스크립트가 (24, 0.6)으로 늘림 → 가로 띠
        t = New(100, 100, Clear);
        Rect(t, 0, 20, 100, 60, new Color(Green.r, Green.g, Green.b, 0.35f)); Rect(t, 0, 38, 100, 24, Green); Rect(t, 0, 46, 100, 8, Hex("EFFFF4"));
        Save(t, "wire");
        // 그림자
        t = New(100, 40, Clear); Ellipse(t, 50, 20, 48, 18, new Color(0, 0, 0, 0.45f)); Save(t, "shadow");
        // 하트
        t = New(64, 64, Clear); Heart(t, Red); Save(t, "heart");
        // UI 패널(9-slice용)
        t = New(48, 48, Clear); RoundRect(t, 0, 0, 48, 48, 12, White); Save(t, "ui_round");
    }

    static void Robot(string name, Color metal, Color eye)
    {
        var t = New(150, 160, Clear);
        Rect(t, 72, 140, 6, 16, metal); Circle(t, 75, 154, 6, eye);              // 안테나
        RoundRect(t, 30, 92, 90, 52, 12, metal); RoundRectOutline(t, 30, 92, 90, 52, 12, 4, Darken(metal, 0.6f)); // 머리
        RoundRect(t, 44, 106, 62, 26, 8, Hex("101828"));
        Circle(t, 60, 119, 7, eye); Circle(t, 90, 119, 7, eye);                  // 눈
        RoundRect(t, 20, 26, 110, 70, 14, Darken(metal, 0.85f)); RoundRectOutline(t, 20, 26, 110, 70, 14, 4, Darken(metal, 0.55f)); // 몸
        RoundRect(t, 52, 40, 46, 30, 6, Hex("101828")); Rect(t, 58, 52, 34, 6, eye);
        RoundRect(t, 0, 40, 22, 46, 8, metal); RoundRect(t, 128, 40, 22, 46, 8, metal); // 팔
        RoundRect(t, 36, 0, 28, 30, 6, Darken(metal, 0.7f)); RoundRect(t, 86, 0, 28, 30, 6, Darken(metal, 0.7f)); // 다리
        Save(t, name);
    }

    static void Orb(string name, int size, Color c)
    {
        var t = New(size, size, Clear);
        float r = size / 2f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                if (d > 1f) continue;
                Color col = d < 0.45f ? Color.Lerp(White, c, d / 0.45f) : new Color(c.r, c.g, c.b, Mathf.Clamp01((1f - d) / 0.55f) * 0.95f + 0.05f);
                t.SetPixel(x, y, col);
            }
        Save(t, name);
    }

    // =====================================================================================
    // 글자 아이콘 (아이템 6종, 업적) — TMP를 렌더 텍스처로 찍어 PNG 저장
    // =====================================================================================
    static readonly (string file, string label, Color color)[] LabelIcons =
    {
        ("item_02_graphic", "그래픽", Pink), ("item_03_qa", "QA", Green),
        ("item_08_graphic_fix", "그래픽\n수정본", Hex("C86BFF")), ("item_09_game_build", "게임\n빌딩", Hex("4C7BFF")),
        ("item_10_art_final", "아트\n완성본", Hex("FF7A59")), ("item_11_error_lump", "오류\n뭉텅이", Hex("5B5B6E")),
        ("ach_first_clear", "첫\n클리어", Yellow), ("ach_no_damage", "무피격", Green),
        ("ach_chapter1_clear", "1학년", Cyan), ("ach_chapter2_clear", "2학년", Hex("C86BFF")), ("ach_chapter3_clear", "3학년", Orange),
        ("station_label_planning", "기획", Yellow), ("station_label_programming", "프로그래밍", Cyan),
        ("station_label_graphic", "그래픽", Pink), ("station_label_qa", "QA", Green),
        ("station_label_craft", "제작대", Orange),
    };

    // 기획서 p.18 배치: 재료대 그래픽·QA 위 / 기획·프로그래밍 아래, 중앙 작업대 라인 위 제작대, 단말기 우상단·좌하단
    public static readonly Dictionary<ItemType, Vector2> StationPos = new Dictionary<ItemType, Vector2>
    {
        { ItemType.Graphic, new Vector2(-6.6f, 3.3f) }, { ItemType.QA, new Vector2(-3.6f, 3.3f) },
        { ItemType.Planning, new Vector2(3.6f, -3.4f) }, { ItemType.Programming, new Vector2(6.6f, -3.4f) },
    };
    public static readonly Vector2 TerminalTopRight = new Vector2(6.6f, 2.5f), TerminalBottomLeft = new Vector2(-6.6f, -2.7f);
    public static readonly Vector2 PlayerStart = new Vector2(0f, -2.2f);

    static void GenerateLabelIcons()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        var font = AssetDatabase.FindAssets("t:TMP_FontAsset NanumGothic").Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>).FirstOrDefault(f => f != null);
        var camGo = new GameObject("Cam"); var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true; cam.orthographicSize = 0.64f; cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Clear; camGo.transform.position = new Vector3(0, 0, -10);

        var round = AssetDatabase.LoadAssetAtPath<Sprite>(Dir + "ui_round.png");
        foreach (var (file, label, color) in LabelIcons)
        {
            bool isStation = file.StartsWith("station_label");
            int w = isStation ? 256 : 128, h = isStation ? 64 : 128;
            cam.orthographicSize = h / 200f;
            var bg = new GameObject("bg").AddComponent<SpriteRenderer>();
            bg.sprite = round; bg.drawMode = SpriteDrawMode.Sliced; bg.size = new Vector2(w / 100f - 0.06f, h / 100f - 0.06f);
            bg.color = isStation ? new Color(0.04f, 0.06f, 0.12f, 0.85f) : color;
            var border = new GameObject("border").AddComponent<SpriteRenderer>();
            border.sprite = round; border.drawMode = SpriteDrawMode.Sliced; border.size = new Vector2(w / 100f, h / 100f);
            border.color = isStation ? color : Darken(color, 0.55f); border.sortingOrder = -1;
            var txt = new GameObject("t").AddComponent<TextMeshPro>();
            txt.font = font; txt.text = label; txt.alignment = TextAlignmentOptions.Center;
            txt.fontStyle = FontStyles.Bold; txt.color = isStation ? color : White;
            txt.enableAutoSizing = true; txt.fontSizeMin = 1; txt.fontSizeMax = isStation ? 3.4f : 3.6f;
            txt.rectTransform.sizeDelta = new Vector2(w / 100f - 0.16f, h / 100f - 0.16f);
            txt.GetComponent<MeshRenderer>().sortingOrder = 2;
            if (!isStation) { txt.outlineWidth = 0.18f; txt.outlineColor = Darken(color, 0.4f); }
            txt.ForceMeshUpdate();

            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt; cam.aspect = (float)w / h; cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
            File.WriteAllBytes(Dir + file + ".png", tex.EncodeToPNG());
            RenderTexture.active = null; cam.targetTexture = null; rt.Release();
            Object.DestroyImmediate(bg.gameObject); Object.DestroyImmediate(border.gameObject); Object.DestroyImmediate(txt.gameObject);
        }
        Object.DestroyImmediate(camGo);
    }

    // =====================================================================================
    // 임포트 설정
    // =====================================================================================
    static void ConfigureImports()
    {
        foreach (var path in Directory.GetFiles(Dir, "*.png"))
        {
            var p = path.Replace('\\', '/');
            var imp = AssetImporter.GetAtPath(p) as TextureImporter;
            if (imp == null) continue;
            string n = Path.GetFileNameWithoutExtension(p);
            bool tile = n == "floor" || n == "wall";
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = n.StartsWith("item_") || n.StartsWith("ach_") ? 128 : n.StartsWith("station_label") ? 256 : PPU;
            imp.filterMode = FilterMode.Bilinear;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.wrapMode = tile ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            var settings = new TextureImporterSettings(); imp.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;                      // Tiled/Sliced용
            if (n == "ui_round") { imp.spriteBorder = new Vector4(16, 16, 16, 16); }
            if (n.StartsWith("terminal_t") || n.StartsWith("robot") || n == "crafting_table" || n.StartsWith("station_"))
            {
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
            }
            imp.SetTextureSettings(settings);
            if (n.StartsWith("terminal_t")) imp.spritePivot = new Vector2(0.4f, 0.45f);
            imp.SaveAndReimport();
        }
    }

    static Sprite S(string n) => AssetDatabase.LoadAssetAtPath<Sprite>(Dir + n + ".png");

    // =====================================================================================
    // 씬·프리팹에 연결
    // =====================================================================================
    static void Apply()
    {
        // ---------- 아이콘 세트 (그림 없는 6종) ----------
        var icons = AssetDatabase.LoadAssetAtPath<ItemIconSet>("Assets/DebugRush/Data/ItemIcons.asset");
        icons.icons[2] = S("item_02_graphic"); icons.icons[3] = S("item_03_qa");
        icons.icons[8] = S("item_08_graphic_fix"); icons.icons[9] = S("item_09_game_build");
        icons.icons[10] = S("item_10_art_final"); icons.icons[11] = S("item_11_error_lump");
        EditorUtility.SetDirty(icons);

        foreach (var id in new[] { "first_clear", "no_damage", "chapter1_clear", "chapter2_clear", "chapter3_clear" })
        {
            var a = AssetDatabase.LoadAssetAtPath<AchievementData>($"Assets/DebugRush/Data/{id}.asset");
            if (a != null) { a.icon = S("ach_" + id); EditorUtility.SetDirty(a); }
        }

        // ---------- 프리팹 ----------
        EditPrefab("MonsterRobot", go => { SetSprite(go, "robot", 10); AddShadow(go, 1.2f); });
        EditPrefab("Monster1B", go => { SetSprite(go, "robot_factory", 10); AddShadow(go, 1.2f); });
        EditPrefab("Monster1C", go => { SetSprite(go, "robot_scrap", 10); AddShadow(go, 1.2f); });
        EditPrefab("Monster1A", go => { SetOrder(go, 10); AddShadow(go, 0.9f, -0.75f); });
        EditPrefab("Player", go => { SetOrder(go, 11); AddShadow(go, 0.8f, -0.62f); });
        EditPrefab("Shot", go => SetSprite(go, "shot_cyan", 20));
        EditPrefab("SplitShot", go => SetSprite(go, "shot_orange", 20));
        EditPrefab("FallShot", go => SetSprite(go, "shot_red", 20));
        EditPrefab("Blade", go => SetSprite(go, "blade", 20));
        EditPrefab("Mine", go => SetSprite(go, "mine", -5));
        EditPrefab("WarnMarker", go => SetSprite(go, "warn", -4));
        EditPrefab("ElectricLine", go => SetSprite(go, "wire", -5));
        EditPrefab("Monster1A_Shot", go => SetOrder(go, 20));
        EditPrefab("MagneticField", go => SetOrder(go, -4));

        // ---------- Stage 씬 ----------
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Stage.unity");
        var cam = Camera.main;
        // 방(20.5×10.5칸) 위아래로 HUD 띠 140px가 남도록 (1920×1080 기준)
        cam.orthographicSize = 7f;
        cam.transform.position = new Vector3(0f, -0.1f, -10f);
        cam.backgroundColor = Navy;

        var floor = GameObject.Find("Floor");
        if (floor == null) floor = new GameObject("Floor");
        var fsr = floor.GetComponent<SpriteRenderer>();
        if (fsr == null) fsr = floor.AddComponent<SpriteRenderer>();
        fsr.sprite = S("floor"); fsr.drawMode = SpriteDrawMode.Tiled; fsr.size = new Vector2(20f, 10f);
        fsr.sortingOrder = -100; floor.transform.position = Vector3.zero;

        foreach (var name in new[] { "Wall_Top", "Wall_Bottom", "Wall_Left", "Wall_Right", "Wall_InnerA", "Wall_InnerB" })
        {
            var w = GameObject.Find(name); if (w == null) continue;
            var sr = w.GetComponent<SpriteRenderer>();
            sr.sprite = S("wall"); sr.drawMode = SpriteDrawMode.Tiled; sr.color = Color.white; sr.sortingOrder = -10;
            sr.size = w.GetComponent<BoxCollider2D>().size;
        }

        // 중앙 작업대 라인
        SetWall("Wall_InnerA", new Vector2(-3.4f, 0f), new Vector2(3.4f, 0.4f));
        SetWall("Wall_InnerB", new Vector2(3.4f, 0f), new Vector2(3.4f, 0.4f));

        var playerGo = GameObject.Find("Player");
        if (playerGo != null) playerGo.transform.position = PlayerStart;

        foreach (var st in Object.FindObjectsByType<ResourceStation>(FindObjectsSortMode.None))
        {
            string key = st.material.ToString().ToLower();
            st.transform.position = StationPos[st.material];
            var sr = st.GetComponent<SpriteRenderer>();
            sr.sprite = S("station_" + key); sr.color = Color.white; sr.sortingOrder = 0;
            Child(st.transform, "Icon", icons.Get(st.material), new Vector2(0f, 0.34f), 0.7f, 2);
            Child(st.transform, "Label", S("station_label_" + key), new Vector2(0f, -0.78f), 1.4f, 3);
        }

        var table = Object.FindAnyObjectByType<CraftingTable>();
        if (table != null)
        {
            table.transform.position = Vector3.zero;
            var sr = table.GetComponent<SpriteRenderer>();
            sr.sprite = S("crafting_table"); sr.color = Color.white; sr.sortingOrder = 0;
            Child(table.transform, "Label", S("station_label_craft"), new Vector2(0f, -0.85f), 1.4f, 3);
            var view = table.GetComponent<CraftingTableView>();
            if (view.outputIcon != null) { view.outputIcon.transform.localPosition = new Vector3(0f, 1.0f, 0f); view.outputIcon.transform.localScale = Vector3.one * 0.7f; view.outputIcon.sortingOrder = 6; }
        }

        var frames = Enumerable.Range(0, 7).Select(i => S("terminal_t" + (6 - i))).ToArray();   // [0] = 가득
        var sm = Object.FindAnyObjectByType<StageManager>();
        if (sm != null && sm.terminals != null && sm.terminals.Length >= 2)
        {
            sm.terminals[0].transform.position = TerminalTopRight;
            sm.terminals[1].transform.position = TerminalBottomLeft;
        }
        foreach (var term in Object.FindObjectsByType<TerminalView>(FindObjectsSortMode.None))
        {
            var sr = term.GetComponent<SpriteRenderer>();
            sr.sprite = frames[0]; sr.color = Color.white; sr.sortingOrder = 0;
            term.timerFrames = frames; term.timerRenderer = sr;
            for (int i = 0; i < term.requestIcons.Length; i++)
            {
                var r = term.requestIcons[i];
                r.transform.localPosition = new Vector3(-0.38f + i * 0.76f, 1.3f, 0f);
                r.transform.localScale = Vector3.one * 0.62f; r.sortingOrder = 6;
            }
            for (int i = 0; i < term.gaugeCells.Length; i++)
            {
                var g = term.gaugeCells[i];
                g.sprite = S("ui_round"); g.drawMode = SpriteDrawMode.Simple; g.sortingOrder = 6;
                g.transform.localPosition = new Vector3(-0.595f + i * 0.17f, -0.98f, 0f);
                g.transform.localScale = Vector3.one * 0.3f;
            }
            term.filled = Yellow; term.empty = Hex("2A3450");
        }

        // HUD 하트
        var hud = Object.FindAnyObjectByType<StageHUD>();
        if (hud != null) foreach (var h in hud.hearts) { h.sprite = S("heart"); h.color = Color.white; h.preserveAspect = true; }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        // ---------- 타이틀 일러스트 ----------
        var title = EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");
        var menu = Object.FindAnyObjectByType<MainMenu>();
        if (menu != null)
        {
            menu.heroSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/DebugRush/Art/Drive/Player/run_R_01.png");
            menu.monsterSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/DebugRush/Art/Drive/Monster1/move_01.png");
            EditorUtility.SetDirty(menu);
        }
        EditorSceneManager.MarkSceneDirty(title);
        EditorSceneManager.SaveScene(title);
    }

    static void SetWall(string name, Vector2 pos, Vector2 size)
    {
        var w = GameObject.Find(name); if (w == null) return;
        w.transform.position = pos;
        w.GetComponent<BoxCollider2D>().size = size;
        w.GetComponent<SpriteRenderer>().size = size;
    }

    static void Child(Transform parent, string name, Sprite sprite, Vector2 pos, float scale, int order)
    {
        var old = parent.Find(name); if (old != null) Object.DestroyImmediate(old.gameObject);
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        go.transform.localPosition = pos; go.transform.localScale = Vector3.one * scale;
        var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sprite; sr.sortingOrder = order;
    }

    static void EditPrefab(string name, System.Action<GameObject> edit)
    {
        string path = P + name + ".prefab";
        if (!File.Exists(path)) { Debug.LogWarning("[Placeholder] no prefab " + name); return; }
        var go = PrefabUtility.LoadPrefabContents(path);
        edit(go);
        PrefabUtility.SaveAsPrefabAsset(go, path);
        PrefabUtility.UnloadPrefabContents(go);
    }

    static void SetSprite(GameObject go, string sprite, int order)
    {
        var sr = go.GetComponentInChildren<SpriteRenderer>();
        sr.sprite = S(sprite); sr.color = Color.white; sr.drawMode = SpriteDrawMode.Simple; sr.sortingOrder = order;
    }

    static void SetOrder(GameObject go, int order)
    {
        foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>()) if (sr.gameObject.name != "Shadow") sr.sortingOrder = order;
    }

    static void AddShadow(GameObject go, float width, float y = -0.7f)
    {
        var old = go.transform.Find("Shadow"); if (old != null) Object.DestroyImmediate(old.gameObject);
        var s = new GameObject("Shadow"); s.transform.SetParent(go.transform, false);
        s.transform.localPosition = new Vector3(0f, y, 0f); s.transform.localScale = new Vector3(width, width, 1f);
        var sr = s.AddComponent<SpriteRenderer>(); sr.sprite = S("shadow"); sr.sortingOrder = -6;
    }

    // =====================================================================================
    // 픽셀 도구
    // =====================================================================================
    static readonly Color Clear = new Color(0, 0, 0, 0);
    static Color Hex(string h) { ColorUtility.TryParseHtmlString("#" + h, out var c); return c; }
    static Color Darken(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, c.a);

    static Texture2D New(int w, int h, Color fill)
    {
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var px = Enumerable.Repeat(fill, w * h).ToArray(); t.SetPixels(px); return t;
    }

    static void Save(Texture2D t, string name) { t.Apply(); File.WriteAllBytes(Dir + name + ".png", t.EncodeToPNG()); }

    static void Put(Texture2D t, int x, int y, Color c)
    {
        if (x < 0 || y < 0 || x >= t.width || y >= t.height) return;
        if (c.a >= 1f) { t.SetPixel(x, y, c); return; }
        var b = t.GetPixel(x, y); float a = c.a + b.a * (1 - c.a);
        t.SetPixel(x, y, a <= 0 ? Clear : new Color((c.r * c.a + b.r * b.a * (1 - c.a)) / a, (c.g * c.a + b.g * b.a * (1 - c.a)) / a, (c.b * c.a + b.b * b.a * (1 - c.a)) / a, a));
    }

    static void Rect(Texture2D t, int x, int y, int w, int h, Color c) { for (int j = y; j < y + h; j++) for (int i = x; i < x + w; i++) Put(t, i, j, c); }

    static bool InRound(int i, int j, int x, int y, int w, int h, int r)
    {
        int cx = Mathf.Clamp(i, x + r, x + w - 1 - r), cy = Mathf.Clamp(j, y + r, y + h - 1 - r);
        return (i - cx) * (i - cx) + (j - cy) * (j - cy) <= r * r;
    }

    static void RoundRect(Texture2D t, int x, int y, int w, int h, int r, Color c)
    { for (int j = y; j < y + h; j++) for (int i = x; i < x + w; i++) if (InRound(i, j, x, y, w, h, r)) Put(t, i, j, c); }

    static void RoundRectOutline(Texture2D t, int x, int y, int w, int h, int r, int th, Color c)
    {
        for (int j = y; j < y + h; j++) for (int i = x; i < x + w; i++)
            if (InRound(i, j, x, y, w, h, r) && !InRound(i, j, x + th, y + th, w - 2 * th, h - 2 * th, Mathf.Max(0, r - th))) Put(t, i, j, c);
    }

    static void Circle(Texture2D t, int cx, int cy, int r, Color c)
    { for (int j = cy - r; j <= cy + r; j++) for (int i = cx - r; i <= cx + r; i++) if ((i - cx) * (i - cx) + (j - cy) * (j - cy) <= r * r) Put(t, i, j, c); }

    static void CircleOutline(Texture2D t, int cx, int cy, int r, int th, Color c)
    {
        for (int j = cy - r; j <= cy + r; j++) for (int i = cx - r; i <= cx + r; i++)
        { int d = (i - cx) * (i - cx) + (j - cy) * (j - cy); if (d <= r * r && d >= (r - th) * (r - th)) Put(t, i, j, c); }
    }

    static void Ellipse(Texture2D t, int cx, int cy, int rx, int ry, Color c)
    {
        for (int j = cy - ry; j <= cy + ry; j++) for (int i = cx - rx; i <= cx + rx; i++)
        {
            float d = Mathf.Pow((i - cx) / (float)rx, 2) + Mathf.Pow((j - cy) / (float)ry, 2);
            if (d <= 1f) Put(t, i, j, new Color(c.r, c.g, c.b, c.a * (1f - d * 0.6f)));
        }
    }

    static void Diamond(Texture2D t, int cx, int cy, int rx, int ry, Color c)
    { for (int j = cy - ry; j <= cy + ry; j++) for (int i = cx - rx; i <= cx + rx; i++) if (Mathf.Abs(i - cx) / (float)rx + Mathf.Abs(j - cy) / (float)ry <= 1f) Put(t, i, j, c); }

    static void Triangle(Texture2D t, int cx, int top, int size, Color c)
    {
        for (int row = 0; row < size; row++)
        {
            int half = Mathf.RoundToInt(row * 0.6f);
            for (int i = cx - half; i <= cx + half; i++) Put(t, i, top - row, c);
        }
    }

    static void Heart(Texture2D t, Color c)
    {
        Circle(t, 20, 40, 15, c); Circle(t, 44, 40, 15, c);
        for (int row = 0; row < 30; row++) { int half = 31 - row; for (int i = 32 - half; i <= 32 + half; i++) Put(t, i, 38 - row, c); }
        Circle(t, 17, 45, 5, new Color(1, 1, 1, 0.6f));
    }
}
