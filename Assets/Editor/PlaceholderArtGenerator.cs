using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 팀 아트 공통 규격 기준 임시 그림 생성 + 씬/프리팹 연결.
/// 규격: 기준 격자 32px, PNG는 4배 출력(1칸 = 128px), Unity 1칸 = 1유닛 (PPU 128).
/// 같은 파일 이름·아트보드 크기로 아트 팀 그림을 덮어쓰면 그대로 교체됨 (Assets/DebugRush/Art/Placeholder).
/// 메뉴: DebugRush > Generate Placeholder Art
/// </summary>
public static class PlaceholderArtGenerator
{
    public const string Dir = "Assets/DebugRush/Art/Placeholder/";
    const string P = "Assets/DebugRush/Prefabs/";
    const string Drive = "Assets/DebugRush/Art/Drive/";
    const int X4 = 4;               // PNG 배율
    const int PPU = 32 * X4;        // 128

    // 팔레트: 배경은 채도·명도 낮게, 상호작용 대상은 밝게
    static readonly Color Navy = Hex("0A0E1A"), FloorA = Hex("161C2C"), FloorB = Hex("1A2133"), FloorLine = Hex("212A40"),
        WallFace = Hex("39465F"), WallTop = Hex("5A6C8E"), WallDark = Hex("222B3F"), Outline = Hex("0B0F1C"),
        Cyan = Hex("39D0FF"), Pink = Hex("FF4FA3"), Yellow = Hex("FFD23F"), Green = Hex("4BE38A"),
        Orange = Hex("FF9F43"), Red = Hex("FF3B4E"), Purple = Hex("B86BFF"), Blue = Hex("4C7BFF"),
        Body = Hex("2B3A5C"), BodyDark = Hex("1D2740"), Screen = Hex("07101F"), White = Color.white;

    public static readonly Dictionary<ItemType, Color> MaterialColor = new Dictionary<ItemType, Color>
    {
        { ItemType.Planning, Yellow }, { ItemType.Programming, Cyan }, { ItemType.Graphic, Pink }, { ItemType.QA, Green },
    };
    static readonly Dictionary<ItemType, Color> ArtLabelColor = new Dictionary<ItemType, Color>
    {
        { ItemType.Planning, Hex("4C9BFF") }, { ItemType.Programming, Hex("E6E9F2") }, { ItemType.Graphic, Hex("FFD23F") }, { ItemType.QA, Hex("B86BFF") },
    };
    const string Fac = "Assets/DebugRush/Art/Facilities/";
    static Sprite F(string n) => AssetDatabase.LoadAssetAtPath<Sprite>(Fac + n + ".png");
    static Sprite Pick(string team, string placeholder) => F(team) != null ? F(team) : S(placeholder);
    const string Tiles = "Assets/DebugRush/Art/Tiles/";
    static Sprite T(string n) { var s = AssetDatabase.LoadAssetAtPath<Sprite>(Tiles + n + ".png"); return s != null ? s : S(n); }

    /// <summary>그래픽 팀 타일(Art/Tiles): 128px = 1칸, 칸 중심 기준, 픽셀아트라 Point 필터</summary>
    static void ConfigureTileArt()
    {
        if (!Directory.Exists(Tiles)) return;
        foreach (var path in Directory.GetFiles(Tiles, "*.png"))
        {
            var p = path.Replace('\\', '/');
            Import(p, PPU, new Vector2(0.5f, 0.5f), Vector4.zero, true);
            var imp = AssetImporter.GetAtPath(p) as TextureImporter;
            if (imp != null && imp.filterMode != FilterMode.Point) { imp.filterMode = FilterMode.Point; imp.SaveAndReimport(); }
        }
    }

    static readonly Dictionary<ItemType, string> MaterialName = new Dictionary<ItemType, string>
    {
        { ItemType.Planning, "기획" }, { ItemType.Programming, "프로그래밍" }, { ItemType.Graphic, "그래픽" }, { ItemType.QA, "QA" },
    };

    // ---------- 방 배치 (1칸 = 1유닛). 바깥 30×16칸, 안쪽 28×14칸 (스테이지 데이터가 있으면 그 배치 사용) ----------
    public const float RoomHalfW = 15f, RoomHalfH = 8f;
    // 기획서 p.18: 재료대 그래픽·QA 위 / 기획·프로그래밍 아래, 중앙 작업대 라인 위 제작대, 단말기 우상단·좌하단
    public static readonly Dictionary<ItemType, Vector2> StationPos = new Dictionary<ItemType, Vector2>
    {
        { ItemType.Graphic, new Vector2(-10f, 4.5f) }, { ItemType.QA, new Vector2(-5f, 4.5f) },
        { ItemType.Planning, new Vector2(5f, -4.5f) }, { ItemType.Programming, new Vector2(10f, -4.5f) },
    };
    public static readonly Vector2 TerminalTopRight = new Vector2(11.5f, 3f), TerminalBottomLeft = new Vector2(-11.5f, -3f);
    public static readonly Vector2 PlayerStart = new Vector2(0f, -4.5f);

    [MenuItem("DebugRush/Generate Placeholder Art")]
    public static void Run()
    {
        Directory.CreateDirectory(Dir);
        Generated.Clear();
        GenerateTextures();
        // 이번에 만들지 않은 예전 파일만 삭제 (같은 이름은 덮어써서 GUID 유지)
        foreach (var old in Directory.GetFiles(Dir, "*.png"))
            if (!Generated.Contains(Path.GetFileNameWithoutExtension(old))) { File.Delete(old); if (File.Exists(old + ".meta")) File.Delete(old + ".meta"); }
        AssetDatabase.Refresh();
        ConfigureImports();
        ConfigureDriveArt();
        ConfigureTileArt();
        AssetDatabase.Refresh();
        BuildFxSet();
        Apply();
        StageMakerWindow.ConvertExisting();
        AssetDatabase.SaveAssets();
        Debug.Log("[Placeholder] done");
    }

    // =====================================================================================
    // 텍스처 (기준 크기 × 4)
    // =====================================================================================
    static void GenerateTextures()
    {
        // 바닥 32×32 / 큰 바닥 패널 64×64
        var t = New(128, 128, FloorA);
        Rect(t, 0, 0, 128, 4, FloorLine); Rect(t, 0, 0, 4, 128, FloorLine);
        Rect(t, 62, 62, 4, 4, FloorLine);
        Save(t, "floor_32");
        t = New(256, 256, FloorB);
        Rect(t, 0, 0, 256, 4, FloorLine); Rect(t, 0, 0, 4, 256, FloorLine); Rect(t, 128, 0, 2, 256, FloorLine); Rect(t, 0, 128, 256, 2, FloorLine);
        Save(t, "floor_64");

        // 벽 32×32: 단독·끝·직선·코너·T자·십자 (연결부는 변 중앙, 회전 사용)
        WallTile("wall_single", false, false, false, false);
        WallTile("wall_end", false, false, true, false);        // 아래로 연결
        WallTile("wall_straight", true, true, false, false);     // 좌우 연결
        WallTile("wall_corner", false, true, true, false);       // 오른쪽·아래
        WallTile("wall_t", true, true, true, false);
        WallTile("wall_cross", true, true, true, true);
        WallTile("wall_fill", true, true, true, true, true);     // 반복 채움용
        // 입체(3/4 시점) 타일 임시 그림 — 그래픽 팀 파일 이름 그대로. 진짜 그림은 Art/Tiles 에 같은 이름으로 넣으면 그쪽을 씀
        for (int i = 1; i <= 3; i++) FloorTile3Q($"tile_floor_0{i}", i, false);
        FloorTile3Q("tile_floor_work", 0, true);
        TopTile("tile_wall_top", false, false, false, false);
        FrontTile("tile_wall_front_01", 0); FrontTile("tile_wall_front_02", 1);
        TopTile("tile_corner_out_tl", true, false, true, false); TopTile("tile_corner_out_tr", false, true, true, false);
        TopTile("tile_corner_out_bl", true, false, false, true); TopTile("tile_corner_out_br", false, true, false, true);
        InnerCorner("tile_corner_in_tl", 0); InnerCorner("tile_corner_in_tr", 1); InnerCorner("tile_corner_in_bl", 2); InnerCorner("tile_corner_in_br", 3);

        // 아이템 생성기 96×80 (기준점 48,40): 공통 몸체 + 재료 색상·표식
        foreach (var kv in MaterialColor) Generator(kv.Key, kv.Value);

        // 제작기 128×96 (기준점 64,48): 대기·제작 중·완료
        Crafter("crafter_idle", Hex("6B7A99"), false);
        Crafter("crafter_working", Orange, true);
        Crafter("crafter_done", Green, false);

        // 단말기 80×112 (기준점 40,56): 오류·전송·복구 (몸체 위치 동일)
        Terminal("terminal_error", Red, 0);
        Terminal("terminal_transmit", Cyan, 1);
        Terminal("terminal_recovered", Green, 2);

        // 아이템 32×32 → 128×128, 사방 2px(=8px) 여백, 글자 없음
        ItemIcon("item_02_graphic", Pink, 0);
        ItemIcon("item_03_qa", Green, 1);
        ItemIcon("item_08_graphic_fix", Purple, 2);
        ItemIcon("item_09_game_build", Blue, 3);
        ItemIcon("item_10_art_complete", Orange, 4);
        ItemIcon("item_11_error", Hex("6E6E80"), 5);

        // 몬스터 64×64 → 256 (몸체 중심 32,32)
        Robot("robot", Hex("6F7FA3"), Pink);
        Robot("robot_factory", Hex("A38A5E"), Orange);
        Robot("robot_scrap", Hex("7C7C7C"), Green);

        // 탄환: 일반 16×16 빨강 / 패링 가능 24×24 분홍 마름모 (외곽선 2px)
        t = New(64, 64, Clear); Circle(t, 32, 32, 28, Outline); Circle(t, 32, 32, 21, Red); Circle(t, 28, 36, 8, Hex("FFB3BB")); Save(t, "bullet_normal");
        t = New(96, 96, Clear); Diamond(t, 48, 48, 46, 46, Outline); Diamond(t, 48, 48, 37, 37, Pink); Diamond(t, 44, 52, 12, 12, Hex("FFD6EA")); Save(t, "bullet_parry");
        t = New(128, 64, Clear); Diamond(t, 64, 32, 60, 26, Outline); Diamond(t, 64, 32, 52, 18, Hex("DDEBFF")); Save(t, "blade");
        t = New(128, 128, Clear); Circle(t, 64, 64, 46, Outline); Circle(t, 64, 64, 38, Hex("2B2B35")); Circle(t, 64, 64, 14, Red); Save(t, "mine");
        // 전선(직선 공격 몸체): 1칸 텍스처를 스크립트가 길이 방향으로 늘림
        t = New(128, 128, Clear);
        Rect(t, 0, 24, 128, 80, new Color(Green.r, Green.g, Green.b, 0.35f)); Rect(t, 0, 48, 128, 32, Green); Rect(t, 0, 58, 128, 12, Hex("EFFFF4"));
        Save(t, "wire");

        // 이펙트: 발사 64 (4f), 피격 64 (4f), 소멸 128 (6f)
        for (int i = 0; i < 4; i++) { t = New(256, 256, Clear); Burst(t, 128, 128, 40 + i * 22, 12 - i * 2, new Color(1f, 0.85f, 0.95f, 1f - i * 0.22f), 8); Save(t, "fx_fire_" + i); }
        for (int i = 0; i < 4; i++) { t = New(256, 256, Clear); Burst(t, 128, 128, 30 + i * 26, 14 - i * 3, new Color(1f, 0.35f, 0.35f, 1f - i * 0.22f), 6); Save(t, "fx_hit_" + i); }
        for (int i = 0; i < 6; i++)
        {
            t = New(512, 512, Clear);
            CircleOutline(t, 256, 256, 60 + i * 34, 14 - i * 2, new Color(1f, 0.6f, 0.9f, 1f - i * 0.16f));
            for (int k = 0; k < 8; k++) { float a = k * Mathf.PI / 4f; int r = 80 + i * 36; Circle(t, 256 + (int)(Mathf.Cos(a) * r), 256 + (int)(Mathf.Sin(a) * r), 10 - i, new Color(1f, 0.8f, 0.95f, 1f - i * 0.16f)); }
            Save(t, "fx_vanish_" + i);
        }

        // 공격 예고: 원형 128 (채움·테두리 분리), 직선 32×128 (아래 중앙 기준, 길이 방향 늘림)
        t = New(512, 512, Clear); Circle(t, 256, 256, 252, White); Save(t, "warn_circle_fill");
        t = New(512, 512, Clear); CircleOutline(t, 256, 256, 252, 16, White); Save(t, "warn_circle_border");
        t = New(128, 512, Clear); RoundRect(t, 0, 0, 128, 512, 40, White); Save(t, "warn_line");

        // 그림자, 하트(UI 64), UI 둥근 틀
        t = New(256, 96, Clear); Ellipse(t, 128, 48, 124, 44, new Color(0, 0, 0, 0.5f)); Save(t, "shadow");
        t = New(64, 64, Clear); Heart(t, Red); Save(t, "heart");
        t = New(64, 64, Clear); RoundRect(t, 0, 0, 64, 64, 16, White); Save(t, "ui_round");

        // 업적 아이콘 (글자 없이 표식): 첫 클리어 별, 무피격 방패, 학년 배지 1~3
        t = New(128, 128, Clear); RoundRect(t, 4, 4, 120, 120, 24, Hex("3A2E10")); Star(t, 64, 64, 46, 20, Yellow); Save(t, "ach_first_clear");
        t = New(128, 128, Clear); RoundRect(t, 4, 4, 120, 120, 24, Hex("103A22")); Shield(t, Green); Save(t, "ach_no_damage");
        for (int c = 1; c <= 3; c++)
        {
            t = New(128, 128, Clear); RoundRect(t, 4, 4, 120, 120, 24, Hex("10283A"));
            Circle(t, 64, 64, 46, c == 1 ? Cyan : c == 2 ? Purple : Orange);
            for (int k = 0; k < c; k++) Circle(t, (int)(64 + (k - (c - 1) / 2f) * 26), 64, 9, White);
            Save(t, $"ach_chapter{c}_clear");
        }
    }

    // ---------- 개별 그림 ----------
    static void FloorTile3Q(string name, int variant, bool work)
    {
        var t = New(128, 128, work ? Hex("1E2638") : FloorA);
        Rect(t, 0, 0, 128, 4, FloorLine); Rect(t, 0, 0, 4, 128, FloorLine);
        if (variant == 2) Rect(t, 60, 60, 8, 8, FloorLine);
        if (variant == 3) { Rect(t, 20, 90, 24, 4, FloorLine); Rect(t, 84, 30, 24, 4, FloorLine); }
        if (work) { Rect(t, 0, 0, 128, 6, Hex("3A3220")); Rect(t, 0, 122, 128, 6, Hex("3A3220")); }
        Save(t, name);
    }

    // 윗면 (모서리면 바깥쪽 두 변에 외곽선)
    static void TopTile(string name, bool left, bool right, bool top, bool bottom)
    {
        var t = New(128, 128, WallTop);
        Rect(t, 16, 16, 96, 96, Hex("62759A"));
        if (left) Rect(t, 0, 0, 8, 128, Outline);
        if (right) Rect(t, 120, 0, 8, 128, Outline);
        if (top) Rect(t, 0, 120, 128, 8, Outline);
        if (bottom) Rect(t, 0, 0, 128, 8, Outline);
        Save(t, name);
    }

    // 앞면: 위쪽 띠는 윗면 색, 아래는 벽 옆면
    static void FrontTile(string name, int variant)
    {
        var t = New(128, 128, WallFace);
        Rect(t, 0, 96, 128, 32, WallTop);
        Rect(t, 0, 88, 128, 8, Outline);
        Rect(t, 0, 0, 128, 10, WallDark);
        for (int y = 30; y < 88; y += 28) Rect(t, variant == 0 ? 0 : 20, y, variant == 0 ? 128 : 88, 4, WallDark);
        Save(t, name);
    }

    // 안쪽 모서리: 윗면 + 꺾이는 쪽 귀퉁이에 앞면 조각 (0 좌상 · 1 우상 · 2 좌하 · 3 우하)
    static void InnerCorner(string name, int corner)
    {
        var t = New(128, 128, WallTop);
        Rect(t, 16, 16, 96, 96, Hex("62759A"));
        int x = corner % 2 == 0 ? 0 : 64, y = corner < 2 ? 64 : 0;
        Rect(t, x, y, 64, 64, WallFace);
        Rect(t, x, y + (corner < 2 ? 0 : 56), 64, 8, Outline);
        Save(t, name);
    }

    static void WallTile(string name, bool l, bool r, bool d, bool u, bool fill = false)
    {
        var t = New(128, 128, Clear);
        // 블록 한 칸을 꽉 채우는 벽 (구멍처럼 보이지 않게)
        RoundRect(t, 0, 0, 128, 128, fill ? 0 : 12, WallFace);
        Rect(t, 0, 96, 128, 32, WallTop);                // 윗면
        Rect(t, 0, 0, 128, 10, WallDark);                // 아래 그림자
        if (!fill)
        {
            // 연결 표시: 연결 안 된 변에 외곽선 2px(=8px)
            if (!l) Rect(t, 0, 0, 8, 128, Outline);
            if (!r) Rect(t, 120, 0, 8, 128, Outline);
            if (!d) Rect(t, 0, 0, 128, 8, Outline);
            if (!u) Rect(t, 0, 120, 128, 8, Outline);
        }
        Rect(t, 30, 40, 68, 6, WallDark); Rect(t, 30, 60, 68, 6, WallDark);   // 패널 줄
        Save(t, name);
    }

    static void Generator(ItemType mat, Color c)
    {
        var t = New(384, 320, Clear);
        RoundRect(t, 8, 8, 368, 240, 28, Outline);
        RoundRect(t, 16, 16, 352, 224, 24, Body);
        RoundRect(t, 0, 176, 384, 136, 32, Outline);
        RoundRect(t, 8, 184, 368, 120, 28, c);                       // 색 상판
        RoundRect(t, 24, 196, 336, 24, 10, Lighten(c, 0.35f));
        RoundRect(t, 120, 40, 144, 112, 18, Screen);                 // 표식 창
        RoundRectOutline(t, 120, 40, 144, 112, 18, 6, c);
        Mark(t, mat, 192, 96, c);
        Save(t, "generator_" + mat.ToString().ToLower());
    }

    static void Mark(Texture2D t, ItemType mat, int cx, int cy, Color c)
    {
        switch (mat)
        {
            case ItemType.Planning:      // 전구
                Circle(t, cx, cy + 8, 30, c); Rect(t, cx - 14, cy - 36, 28, 18, Lighten(c, 0.3f)); Rect(t, cx - 4, cy, 8, 22, Screen); break;
            case ItemType.Programming:   // < >
                Chevron(t, cx - 26, cy, 26, 8, c, true); Chevron(t, cx + 26, cy, 26, 8, c, false); break;
            case ItemType.Graphic:       // 붓 (마름모 + 손잡이)
                Diamond(t, cx + 14, cy + 14, 20, 20, c); Line(t, cx - 30, cy - 30, cx + 6, cy + 6, 10, Lighten(c, 0.3f)); break;
            default:                     // QA: 체크
                Line(t, cx - 34, cy, cx - 10, cy - 26, 14, c); Line(t, cx - 10, cy - 26, cx + 36, cy + 30, 14, c); break;
        }
    }

    static void Crafter(string name, Color light, bool working)
    {
        var t = New(512, 384, Clear);
        RoundRect(t, 8, 8, 496, 256, 32, Outline);
        RoundRect(t, 16, 16, 480, 240, 28, Body);
        RoundRect(t, 0, 200, 512, 176, 36, Outline);
        RoundRect(t, 8, 208, 496, 160, 32, Hex("3B4C72"));            // 작업판
        RoundRect(t, 40, 236, 432, 104, 20, BodyDark);
        RoundRectOutline(t, 40, 236, 432, 104, 20, 6, light);
        // 재료 투입구 2개 + 결과 칸
        RoundRect(t, 72, 256, 96, 64, 12, Screen); RoundRect(t, 344, 256, 96, 64, 12, Screen);
        RoundRect(t, 208, 248, 96, 80, 14, light * 0.5f + Screen * 0.5f);
        // 램프
        for (int i = 0; i < 3; i++) Circle(t, 176 + i * 80, 80, 20, i == 1 || !working ? light : Lighten(light, 0.4f));
        if (working) for (int i = 0; i < 5; i++) Rect(t, 96 + i * 72, 140, 40, 12, light);
        Save(t, name);
    }

    static void Terminal(string name, Color screen, int state)
    {
        var t = New(320, 448, Clear);
        RoundRect(t, 64, 0, 192, 56, 16, Outline); RoundRect(t, 72, 8, 176, 40, 12, BodyDark);      // 받침
        RoundRect(t, 0, 40, 320, 408, 36, Outline);
        RoundRect(t, 8, 48, 304, 392, 32, Body);
        RoundRect(t, 32, 120, 256, 288, 20, Screen);
        RoundRectOutline(t, 32, 120, 256, 288, 20, 8, screen);
        int cx = 160, cy = 264;
        if (state == 0) { Triangle(t, cx, cy + 70, 120, screen); Rect(t, cx - 8, cy - 20, 16, 56, Screen); Rect(t, cx - 8, cy - 40, 16, 12, Screen); }
        else if (state == 1) { for (int i = 0; i < 3; i++) Chevron(t, cx, cy - 50 + i * 50, 40, 12, screen, false, true); }
        else { Line(t, cx - 70, cy, cx - 20, cy - 50, 22, screen); Line(t, cx - 20, cy - 50, cx + 80, cy + 60, 22, screen); }
        for (int i = 0; i < 4; i++) Circle(t, 80 + i * 54, 84, 10, i == state ? screen : Hex("3A4560"));
        Save(t, name);
    }

    static void ItemIcon(string name, Color c, int shape)
    {
        var t = New(128, 128, Clear);
        RoundRect(t, 8, 8, 112, 112, 22, Outline);
        RoundRect(t, 14, 14, 100, 100, 18, Darken(c, 0.35f));
        int cx = 64, cy = 64;
        switch (shape)
        {
            case 0: Circle(t, cx, cy, 34, c); Circle(t, cx - 14, cy + 12, 7, Outline); Circle(t, cx + 12, cy + 16, 7, Outline); Circle(t, cx + 18, cy - 8, 7, Outline); Circle(t, cx - 6, cy - 18, 9, Darken(c, 0.35f)); break;   // 팔레트
            case 1: CircleOutline(t, cx - 8, cy + 8, 26, 9, c); Line(t, cx + 10, cy - 10, cx + 36, cy - 36, 12, c); break;   // 돋보기
            case 2: Circle(t, cx - 6, cy + 6, 28, c); Line(t, cx + 2, cy - 30, cx + 16, cy - 44, 10, Green); Line(t, cx + 16, cy - 44, cx + 44, cy - 14, 10, Green); break;  // 팔레트+체크
            case 3: Rect(t, cx - 30, cy - 30, 60, 60, c); Rect(t, cx - 30, cy + 18, 60, 12, Lighten(c, 0.35f)); Rect(t, cx - 4, cy - 30, 8, 60, Darken(c, 0.6f)); break;  // 빌드 상자
            case 4: RoundRectOutline(t, cx - 38, cy - 38, 76, 76, 8, 8, c); Star(t, cx, cy, 26, 11, Yellow); break;   // 액자+별
            default: Blob(t, cx, cy, 38, c); Line(t, cx - 18, cy - 18, cx + 18, cy + 18, 9, Red); Line(t, cx - 18, cy + 18, cx + 18, cy - 18, 9, Red); break;   // 오류 덩어리
        }
        Save(t, name);
    }

    static void Robot(string name, Color metal, Color eye)
    {
        var t = New(256, 256, Clear);
        // 64 기준 몸체 약 44칸 → 중앙 정렬
        RoundRect(t, 120, 214, 16, 30, 6, Outline); Circle(t, 128, 240, 12, Outline); Circle(t, 128, 240, 8, eye);
        RoundRect(t, 64, 150, 128, 76, 18, Outline); RoundRect(t, 72, 158, 112, 60, 14, metal);
        RoundRect(t, 84, 170, 88, 36, 10, Screen); Circle(t, 108, 188, 10, eye); Circle(t, 148, 188, 10, eye);
        RoundRect(t, 48, 56, 160, 100, 22, Outline); RoundRect(t, 56, 64, 144, 84, 18, Darken(metal, 0.85f));
        RoundRect(t, 92, 82, 72, 44, 10, Screen); Rect(t, 100, 100, 56, 8, eye);
        RoundRect(t, 18, 72, 36, 68, 12, Outline); RoundRect(t, 202, 72, 36, 68, 12, Outline);
        RoundRect(t, 24, 78, 24, 56, 8, metal); RoundRect(t, 208, 78, 24, 56, 8, metal);
        RoundRect(t, 72, 16, 40, 46, 10, Outline); RoundRect(t, 144, 16, 40, 46, 10, Outline);
        RoundRect(t, 78, 22, 28, 34, 6, Darken(metal, 0.7f)); RoundRect(t, 150, 22, 28, 34, 6, Darken(metal, 0.7f));
        Save(t, name);
    }

    // =====================================================================================
    // 임포트 설정 (전부 PPU 128 = 1칸, 피벗은 규격 기준점)
    // =====================================================================================
    static void ConfigureImports()
    {
        foreach (var path in Directory.GetFiles(Dir, "*.png"))
        {
            var p = path.Replace('\\', '/');
            string n = Path.GetFileNameWithoutExtension(p);
            Vector2 pivot = new Vector2(0.5f, 0.5f);
            Vector4 border = Vector4.zero;
            if (n == "warn_line") { pivot = new Vector2(0.5f, 0f); border = new Vector4(32, 40, 32, 40); }
            if (n == "ui_round") border = new Vector4(16, 16, 16, 16);
            bool tile = n.StartsWith("floor") || n.StartsWith("wall") || n.StartsWith("tile_");
            Import(p, PPU, pivot, border, tile);
        }
    }

    static void Import(string path, float ppu, Vector2 pivot, Vector4 border, bool repeat = false)
    {
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) return;
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = ppu;
        imp.filterMode = FilterMode.Bilinear;               // 벡터→PNG 그림 (픽셀아트 아님)
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;
        imp.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        imp.maxTextureSize = 2048;
        var s = new TextureImporterSettings(); imp.ReadTextureSettings(s);
        s.spriteMeshType = SpriteMeshType.FullRect;
        s.spriteAlignment = (int)SpriteAlignment.Custom;
        s.spritePivot = pivot;
        s.spriteBorder = border;
        imp.SetTextureSettings(s);
        imp.SaveAndReimport();
    }

    /// <summary>
    /// 드라이브 아트: 캔버스 800×800 = 규격 64×64 아트보드 → 2칸 → PPU 400.
    /// 피벗: 주인공 발바닥(측정값: 바닥선 y≈784/800, 좌우 프레임 몸체 중심 차이 보정), 몬스터 몸체 중심.
    /// </summary>
    static void ConfigureDriveArt()
    {
        foreach (var f in Directory.GetFiles(Drive + "Player", "*.png"))
        {
            string n = Path.GetFileNameWithoutExtension(f);
            float px = n.StartsWith("run_R") ? 0.44f : n.StartsWith("run_L") ? 0.56f : n.StartsWith("dash_R") ? 0.47f : 0.53f;
            Import(f.Replace('\\', '/'), 400f, new Vector2(px, 0.02f), Vector4.zero);
        }
        foreach (var f in Directory.GetFiles(Drive + "Monster1", "*.png"))
        {
            string n = Path.GetFileNameWithoutExtension(f);
            float ppu = n.StartsWith("magnetic") ? 123f : n.StartsWith("shot") ? 128f : 400f;   // 자기장 원(약 540px)=지름 4.4칸
            Import(f.Replace('\\', '/'), ppu, new Vector2(0.5f, 0.5f), Vector4.zero);
        }
        foreach (var f in Directory.GetFiles(Drive + "Items", "*.png"))
            Import(f.Replace('\\', '/'), PPU, new Vector2(0.5f, 0.5f), Vector4.zero);

        // 팀 시설 아트: 단말기 960×1344 = 80×112 아트보드 ×12 → PPU 384 (2.5×3.5칸),
        //              아이템 생성기 1200×1000 = 96×80 ×12.5 → PPU 400 (3×2.5칸). 피벗 = 규격 기준점(중앙)
        if (Directory.Exists(Fac))
            foreach (var f in Directory.GetFiles(Fac, "*.png"))
            {
                string n = Path.GetFileNameWithoutExtension(f);
                Import(f.Replace('\\', '/'), n.StartsWith("terminal") ? 384f : 400f, new Vector2(0.5f, 0.5f), Vector4.zero);
            }
        const string logo = "Assets/DebugRush/Art/UI/title_logo.png";      // 1024×512 UI
        if (File.Exists(logo)) Import(logo, 100f, new Vector2(0.5f, 0.5f), Vector4.zero);
    }

    static Sprite S(string n) => AssetDatabase.LoadAssetAtPath<Sprite>(Dir + n + ".png");
    static Sprite D(string n) => AssetDatabase.LoadAssetAtPath<Sprite>(Drive + n + ".png");
    static Sprite[] Seq(string prefix, int count) => Enumerable.Range(0, count).Select(i => S(prefix + i)).ToArray();
    static Sprite[] DriveSeq(string folder, string prefix) =>
        Directory.GetFiles(Drive + folder, prefix + "_*.png").OrderBy(x => x).Select(x => AssetDatabase.LoadAssetAtPath<Sprite>(x.Replace('\\', '/'))).ToArray();

    /// <summary>좌상·우상·좌하·우하. 팀 그림 우선 → 오른쪽 팀 그림이 없고 왼쪽이 있으면 왼쪽 반전 → 임시 그림</summary>
    static void Corners(string prefix, out Sprite[] sprites, out bool[] flip)
    {
        string[] keys = { "tl", "tr", "bl", "br" };
        sprites = new Sprite[4]; flip = new bool[4];
        for (int i = 0; i < 4; i++)
        {
            var team = AssetDatabase.LoadAssetAtPath<Sprite>(Tiles + prefix + keys[i] + ".png");
            if (team == null && (i == 1 || i == 3))
            {
                var left = AssetDatabase.LoadAssetAtPath<Sprite>(Tiles + prefix + keys[i - 1] + ".png");
                if (left != null) { sprites[i] = left; flip[i] = true; continue; }
            }
            sprites[i] = team != null ? team : S(prefix + keys[i]);
        }
    }

    static void BuildFxSet()
    {
        Directory.CreateDirectory("Assets/Resources");
        const string path = "Assets/Resources/FxSet.asset";
        var fx = AssetDatabase.LoadAssetAtPath<FxSet>(path);
        if (fx == null) { fx = ScriptableObject.CreateInstance<FxSet>(); AssetDatabase.CreateAsset(fx, path); }
        fx.bulletNormal = S("bullet_normal"); fx.bulletParry = S("bullet_parry");
        fx.fireFx = Seq("fx_fire_", 4); fx.hitFx = Seq("fx_hit_", 4); fx.vanishFx = Seq("fx_vanish_", 6);
        fx.warnCircleFill = S("warn_circle_fill"); fx.warnCircleBorder = S("warn_circle_border"); fx.warnLine = S("warn_line");
        fx.wallSingle = S("wall_single"); fx.wallEnd = S("wall_end"); fx.wallStraight = S("wall_straight");
        fx.wallCorner = S("wall_corner"); fx.wallT = S("wall_t"); fx.wallCross = S("wall_cross");
        // 입체 타일: 그래픽 팀 그림(Art/Tiles)이 있으면 그걸, 없으면 임시 그림
        fx.tileFloor = new[] { "tile_floor_01", "tile_floor_02", "tile_floor_03" }.Select(T).Where(s => s != null).ToArray();
        fx.tileWorkFloor = T("tile_floor_work");
        fx.wallTopTile = T("tile_wall_top");
        fx.wallFront = new[] { "tile_wall_front_01", "tile_wall_front_02" }.Select(T).Where(s => s != null).ToArray();
        // 모서리: 그래픽 팀은 왼쪽(tl·bl)만 그려도 됨 → 오른쪽 그림이 없으면 왼쪽 그림을 좌우 반전
        Corners("tile_corner_out_", out fx.cornerOut, out fx.cornerOutFlip);
        Corners("tile_corner_in_", out fx.cornerIn, out fx.cornerInFlip);
        EditorUtility.SetDirty(fx);
    }

    // =====================================================================================
    // 씬·프리팹 연결
    // =====================================================================================
    static void Apply()
    {
        // ---------- 아이템 아이콘 (규격 표 번호 순서 = ItemType 순서) ----------
        var icons = AssetDatabase.LoadAssetAtPath<ItemIconSet>("Assets/DebugRush/Data/ItemIcons.asset");
        icons.icons = new[]
        {
            D("Items/item_00_plan"), D("Items/item_01_programming"), S("item_02_graphic"), S("item_03_qa"),
            D("Items/item_04_content_doc"), D("Items/item_05_system_doc"), D("Items/item_06_character_code"), D("Items/item_07_code_fix"),
            S("item_08_graphic_fix"), S("item_09_game_build"), S("item_10_art_complete"), S("item_11_error"),
        };
        EditorUtility.SetDirty(icons);
        foreach (var id in new[] { "first_clear", "no_damage", "chapter1_clear", "chapter2_clear", "chapter3_clear" })
        {
            var a = AssetDatabase.LoadAssetAtPath<AchievementData>($"Assets/DebugRush/Data/{id}.asset");
            if (a != null) { a.icon = S("ach_" + id); EditorUtility.SetDirty(a); }
        }

        // ---------- 프리팹 ----------
        EditPrefab("Player", go =>
        {
            var pc = go.GetComponent<PlayerController>();
            pc.moveSpeed = 7f; pc.interactRadius = 1.4f; pc.interactHeight = 0.6f; pc.jumpHeight = 0.8f;
            var col = go.GetComponent<CircleCollider2D>(); col.radius = 0.35f; col.offset = new Vector2(0f, 0.3f);
            var vis = pc.visual.GetComponent<SpriteRenderer>(); vis.sortingOrder = 0; vis.sprite = D("Player/run_R_01");
            var psa = go.GetComponent<PlayerSpriteAnimator>(); psa.runFps = 11f;
            AddShadow(go, 0.75f, 0.05f);
        });
        EditPrefab("Monster1A", go =>
        {
            Monster(go, 2.5f, true);
            var m = go.GetComponent<Monster1A>();
            m.projectilePrefab = AssetDatabase.LoadAssetAtPath<Projectile>(P + "Monster1A_Shot.prefab");
            m.spreadAngle = 20f;
            m.linePrefab = AssetDatabase.LoadAssetAtPath<Hazard>(P + "ElectricLine.prefab");
            m.fieldPrefab = AssetDatabase.LoadAssetAtPath<MagneticField>(P + "MagneticField.prefab");
            var msa = go.GetComponent<MonsterSpriteAnimator>(); msa.fps = 10f;
            var shotAnim = DriveSeq("Monster1", "pattern1"); var fieldAnim = DriveSeq("Monster1", "pattern2");
            msa.move = DriveSeq("Monster1", "move");
            msa.pattern1 = shotAnim; msa.pattern2 = fieldAnim; msa.pattern3 = shotAnim; msa.pattern4 = fieldAnim; msa.pattern5 = shotAnim;   // A-1~A-5
            AddShadow(go, 0.9f, -1.0f);
        });
        EditPrefab("MonsterRobot", go => { Monster(go, 0f, false); SetSprite(go, S("robot"), 10); AddShadow(go, 1.0f, -0.85f);
            var r = go.GetComponent<MonsterRobot>(); r.lineLength = 30f; r.lineThickness = 1f;
            r.projectilePrefab = AssetDatabase.LoadAssetAtPath<Projectile>(P + "Shot.prefab"); r.linePrefab = AssetDatabase.LoadAssetAtPath<Hazard>(P + "ElectricLine.prefab"); });
        EditPrefab("Monster1B", go => { Monster(go, 0f, false); SetSprite(go, S("robot_factory"), 10); AddShadow(go, 1.0f, -0.85f); });
        EditPrefab("Monster1C", go => { Monster(go, 0f, false); SetSprite(go, S("robot_scrap"), 10); AddShadow(go, 1.0f, -0.85f);
            var c = go.GetComponent<Monster1C>(); c.lineLength = 30f; c.lineThickness = 1f; });

        EditPrefab("Shot", go => Bullet(go, 7f, 0.22f));
        EditPrefab("SplitShot", go => Bullet(go, 7f, 0.22f));
        EditPrefab("FallShot", go => Bullet(go, 14f, 0.3f));
        EditPrefab("Monster1A_Shot", go =>
        {
            foreach (var a in go.GetComponentsInChildren<SpriteFrameAnimator>()) Object.DestroyImmediate(a, true);   // 깜빡임 원인 제거
            Bullet(go, 6.5f, 0.22f);
        });
        EditPrefab("Blade", go =>
        {
            Bullet(go, 14f, 0.3f);
            var p = go.GetComponent<Projectile>(); p.useSharedBullet = false; p.rotateToDirection = true;
            p.normalSprite = S("blade"); p.parrySprite = S("blade");
            SetSprite(go, S("blade"), 30);
        });
        EditPrefab("Mine", go => SetSprite(go, S("mine"), -5));
        EditPrefab("WarnMarker", go => { SetSprite(go, S("warn_circle_border"), -40); go.transform.localScale = Vector3.one * 0.35f; go.GetComponentInChildren<SpriteRenderer>().color = new Color(1f, 0.3f, 0.35f, 0.9f); });
        EditPrefab("ElectricLine", go => SetSprite(go, S("wire"), -5));
        EditPrefab("MagneticField", go =>
        {
            var col = go.GetComponent<CircleCollider2D>(); if (col != null) col.radius = 2.2f;
            go.transform.localScale = Vector3.one;
            var sr = go.GetComponentInChildren<SpriteRenderer>();
            var mag = DriveSeq("Monster1", "magnetic");
            sr.sprite = mag[0]; sr.sortingOrder = -30; sr.color = new Color(1f, 1f, 1f, 0.55f);
            var a = sr.GetComponent<SpriteFrameAnimator>(); if (a == null) a = sr.gameObject.AddComponent<SpriteFrameAnimator>();
            a.target = sr; a.frames = mag; a.fps = 10f; a.loop = true;
        });

        // ---------- Stage 씬 ----------
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Stage.unity");
        var cam = Camera.main;
        cam.orthographicSize = 1080f / 2f / 64f;          // 1칸 = 화면 64px (캐릭터 2칸 = 128px)
        cam.transform.position = new Vector3(0f, 0f, -10f);
        cam.backgroundColor = Navy;

        var floor = Find("Floor"); if (floor == null) floor = new GameObject("Floor");
        var fsr = Get<SpriteRenderer>(floor);
        fsr.sprite = S("floor_32"); fsr.drawMode = SpriteDrawMode.Tiled; fsr.size = new Vector2(RoomHalfW * 2f - 2f, RoomHalfH * 2f - 2f);
        fsr.sortingOrder = -100; floor.transform.position = Vector3.zero;

        SetWall("Wall_Top", new Vector2(0f, RoomHalfH - 0.5f), new Vector2(RoomHalfW * 2f, 1f));
        SetWall("Wall_Bottom", new Vector2(0f, -RoomHalfH + 0.5f), new Vector2(RoomHalfW * 2f, 1f));
        SetWall("Wall_Left", new Vector2(-RoomHalfW + 0.5f, 0f), new Vector2(1f, RoomHalfH * 2f - 2f));
        SetWall("Wall_Right", new Vector2(RoomHalfW - 0.5f, 0f), new Vector2(1f, RoomHalfH * 2f - 2f));
        // 중앙 작업대 라인 (가운데 제작기 양옆, 바깥쪽은 통로)
        SetWall("Wall_InnerA", new Vector2(-4.5f, 0f), new Vector2(5f, 1f));
        SetWall("Wall_InnerB", new Vector2(4.5f, 0f), new Vector2(5f, 1f));
        // 기획서 p.18 벽 모양: 좌상단·우하단 튀어나온 벽 (2×2칸)
        CloneWall("Wall_CornerTL", "Wall_Top"); SetWall("Wall_CornerTL", new Vector2(-13f, 6f), new Vector2(2f, 2f));
        CloneWall("Wall_CornerBR", "Wall_Top"); SetWall("Wall_CornerBR", new Vector2(13f, -6f), new Vector2(2f, 2f));

        BuildWallTiles();

        var playerGo = Find("Player");
        if (playerGo != null) playerGo.transform.position = PlayerStart;

        foreach (var st in Object.FindObjectsByType<ResourceStation>(FindObjectsSortMode.None))
        {
            string key = st.material.ToString().ToLower();
            st.transform.position = StationPos[st.material];
            st.transform.localScale = Vector3.one;
            var sr = st.GetComponent<SpriteRenderer>();
            sr.sprite = Pick("generator_" + (st.material == ItemType.Planning ? "plan" : key), "generator_" + key); sr.color = Color.white; sr.sortingOrder = 0;
            var box = st.GetComponent<BoxCollider2D>(); box.size = new Vector2(2.7f, 1.9f); box.offset = new Vector2(0f, -0.1f);
            RemoveChild(st.transform, "Icon"); RemoveChild(st.transform, "Label");
            Label(st.transform, MaterialName[st.material], new Vector2(0f, -1.55f), ArtLabelColor[st.material]);
        }

        var table = Object.FindAnyObjectByType<CraftingTable>();
        if (table != null)
        {
            table.transform.position = Vector3.zero; table.transform.localScale = Vector3.one;
            var sr = table.GetComponent<SpriteRenderer>();
            sr.sprite = S("crafter_idle"); sr.color = Color.white; sr.sortingOrder = 0;
            var box = table.GetComponent<BoxCollider2D>(); box.size = new Vector2(3.8f, 2.4f); box.offset = new Vector2(0f, -0.1f);
            RemoveChild(table.transform, "Label"); RemoveChild(table.transform, "Icon");
            Label(table.transform, "제작대", new Vector2(0f, -1.85f), Orange);
            var view = table.GetComponent<CraftingTableView>();
            view.idleSprite = S("crafter_idle"); view.workingSprite = S("crafter_working"); view.doneSprite = S("crafter_done");
            if (view.outputIcon != null)
            {
                view.outputIcon.transform.localPosition = new Vector3(0f, 0.68f, 0f);
                view.outputIcon.transform.localScale = Vector3.one * 0.62f;
                view.outputIcon.sortingOrder = 5;
            }
            view.materialSpacing = 1.06f; view.materialScale = 0.62f;
        }

        var sm = Object.FindAnyObjectByType<StageManager>();
        if (sm != null && sm.terminals != null && sm.terminals.Length >= 2)
        {
            sm.terminals[0].transform.position = TerminalTopRight;
            sm.terminals[1].transform.position = TerminalBottomLeft;
        }
        foreach (var term in Object.FindObjectsByType<TerminalView>(FindObjectsSortMode.None))
        {
            term.transform.localScale = Vector3.one;
            var sr = term.GetComponent<SpriteRenderer>();
            sr.sprite = Pick("terminal_error", "terminal_error"); sr.color = Color.white; sr.sortingOrder = 0;
            term.errorSprite = Pick("terminal_error", "terminal_error"); term.transmitSprite = Pick("terminal_sending", "terminal_transmit"); term.recoveredSprite = Pick("terminal_complete", "terminal_recovered");
            var box = term.GetComponent<BoxCollider2D>(); box.size = new Vector2(2.2f, 3.0f); box.offset = new Vector2(0f, 0f);
            for (int i = 0; i < term.requestIcons.Length; i++)
            {
                var r = term.requestIcons[i];
                r.transform.localPosition = new Vector3(-0.48f + i * 0.96f, 2.35f, 0f);
                r.transform.localScale = Vector3.one * 0.82f;
            }
            for (int i = 0; i < term.gaugeCells.Length; i++)
            {
                var g = term.gaugeCells[i];
                g.sprite = S("ui_round"); g.drawMode = SpriteDrawMode.Simple; g.sortingOrder = 6;
                g.transform.localPosition = new Vector3(-0.945f + i * 0.27f, -1.95f, 0f);
                g.transform.localScale = Vector3.one * 0.42f;
            }
            term.filled = Yellow; term.empty = Hex("2A3450");
        }

        var hud = Object.FindAnyObjectByType<StageHUD>();
        if (hud != null) foreach (var h in hud.hearts) { h.sprite = S("heart"); h.color = Color.white; h.preserveAspect = true; }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        // ---------- 타이틀 일러스트 ----------
        var title = EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");
        var menu = Object.FindAnyObjectByType<MainMenu>();
        if (menu != null)
        {
            menu.heroSprite = D("Player/run_R_01");
            menu.monsterSprite = D("Monster1/move_01");
            menu.logoSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/DebugRush/Art/UI/title_logo.png");
            EditorUtility.SetDirty(menu);
        }
        EditorSceneManager.MarkSceneDirty(title);
        EditorSceneManager.SaveScene(title);
    }

    static void Monster(GameObject go, float speed, bool wander)
    {
        var m = go.GetComponent<MonsterBase>();
        m.moveSpeed = speed; m.wander = wander;
        m.patternInterval = 2.6f; m.windupTime = 0.6f; m.recoverTime = 0.5f;
        m.moveArea = new Rect(-11f, -4f, 22f, 8f); m.firstAttackDelay = 3f;
        m.intervalStepMultiplier = 0.92f; m.minPatternInterval = 1.4f; m.maxAggression = 6;
        m.speedStepBonus = 0.06f; m.maxMoveSpeed = 4.5f;
        foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>()) if (sr.gameObject.name != "Shadow") sr.sortingOrder = 10;
    }

    static void Bullet(GameObject go, float speed, float radius)
    {
        var p = go.GetComponent<Projectile>(); p.speed = speed; p.useSharedBullet = true; p.rotateToDirection = false;
        var circle = go.GetComponent<CircleCollider2D>(); if (circle != null) circle.radius = radius;
        var box = go.GetComponent<BoxCollider2D>(); if (box != null) box.size = Vector2.one * radius * 2f;
        var sr = go.GetComponentInChildren<SpriteRenderer>(); sr.sprite = S("bullet_normal"); sr.color = Color.white; sr.sortingOrder = 30;
        go.transform.localScale = Vector3.one;
    }

    static GameObject Find(string name) => GameObject.Find(name);
    static T Get<T>(GameObject go) where T : Component { var c = go.GetComponent<T>(); return c != null ? c : go.AddComponent<T>(); }

    static void SetWall(string name, Vector2 pos, Vector2 size)
    {
        var w = Find(name); if (w == null) return;
        w.transform.position = pos; w.transform.localScale = Vector3.one;
        w.GetComponent<BoxCollider2D>().size = size;
        var sr = w.GetComponent<SpriteRenderer>();
        sr.sprite = S("wall_fill"); sr.drawMode = SpriteDrawMode.Tiled; sr.color = Color.white; sr.sortingOrder = 0;
        sr.size = size;
    }

    /// <summary>
    /// 벽을 32 격자 한 칸씩 연결 조각으로 깔기 (팀 규격: 단독·끝·직선·코너·T자·십자, 회전 사용).
    /// 조각 기본 방향: 끝=아래 연결, 직선=좌우, 코너=오른쪽+아래, T자=좌·우·아래. 콜라이더는 원래 벽 오브젝트 그대로.
    /// </summary>
    static void BuildWallTiles()
    {
        var walls = Object.FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None).Where(b => b.gameObject.name.StartsWith("Wall_")).ToList();
        var cells = new HashSet<Vector2Int>();
        foreach (var w in walls) foreach (var c in Cells(w)) cells.Add(c);

        foreach (var w in walls)
        {
            var sr = w.GetComponent<SpriteRenderer>(); if (sr != null) sr.enabled = false;
            RemoveChild(w.transform, "Tiles");
            var root = new GameObject("Tiles"); root.transform.SetParent(w.transform, false);
            foreach (var c in Cells(w))
            {
                bool l = cells.Contains(c + Vector2Int.left), r = cells.Contains(c + Vector2Int.right),
                     d = cells.Contains(c + Vector2Int.down), u = cells.Contains(c + Vector2Int.up);
                int n = (l ? 1 : 0) + (r ? 1 : 0) + (d ? 1 : 0) + (u ? 1 : 0);
                string piece; float angle = 0f;
                if (n == 0) piece = "wall_single";
                else if (n == 4) piece = "wall_cross";
                else if (n == 1) { piece = "wall_end"; angle = d ? 0f : r ? 90f : u ? 180f : 270f; }
                else if (n == 3) { piece = "wall_t"; angle = !u ? 0f : !l ? 90f : !d ? 180f : 270f; }
                else if ((l && r) || (u && d)) { piece = "wall_straight"; angle = l && r ? 0f : 90f; }
                else { piece = "wall_corner"; angle = r && d ? 0f : u && r ? 90f : l && u ? 180f : 270f; }

                var t = new GameObject(piece);
                t.transform.SetParent(root.transform, false);
                t.transform.position = new Vector3(c.x + 0.5f, c.y + 0.5f, 0f);
                t.transform.rotation = Quaternion.Euler(0f, 0f, angle);
                var tsr = t.AddComponent<SpriteRenderer>(); tsr.sprite = S(piece); tsr.sortingOrder = 0;
            }
        }
    }

    static IEnumerable<Vector2Int> Cells(BoxCollider2D w)
    {
        Vector2 c = (Vector2)w.transform.position + w.offset, h = w.size * 0.5f;
        for (int x = Mathf.RoundToInt(c.x - h.x); x < Mathf.RoundToInt(c.x + h.x); x++)
            for (int y = Mathf.RoundToInt(c.y - h.y); y < Mathf.RoundToInt(c.y + h.y); y++)
                yield return new Vector2Int(x, y);
    }

    static void CloneWall(string name, string source)
    {
        if (Find(name) != null) return;
        var src = Find(source); if (src == null) return;
        var copy = Object.Instantiate(src, src.transform.parent);    // 태그·레이어·콜라이더 그대로
        copy.name = name;
    }

    static void RemoveChild(Transform parent, string name) { var c = parent.Find(name); if (c != null) Object.DestroyImmediate(c.gameObject); }

    static void Label(Transform parent, string text, Vector2 pos, Color color)
    {
        var go = new GameObject("Label"); go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        var t = go.AddComponent<TextMeshPro>();
        t.font = AssetDatabase.FindAssets("t:TMP_FontAsset NanumGothic").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>).FirstOrDefault(f => f != null);
        t.text = text; t.fontSize = 3.6f; t.fontStyle = FontStyles.Bold; t.alignment = TextAlignmentOptions.Center;
        t.color = color; t.outlineWidth = 0.28f; t.outlineColor = new Color32(8, 12, 24, 255);
        t.rectTransform.sizeDelta = new Vector2(4f, 0.8f);
        t.GetComponent<MeshRenderer>().sortingOrder = 40;
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

    static void SetSprite(GameObject go, Sprite sprite, int order)
    {
        var sr = go.GetComponentsInChildren<SpriteRenderer>().First(s => s.gameObject.name != "Shadow");
        sr.sprite = sprite; sr.color = Color.white; sr.drawMode = SpriteDrawMode.Simple; sr.sortingOrder = order;
    }

    static void AddShadow(GameObject go, float width, float y)
    {
        var old = go.transform.Find("Shadow"); if (old != null) Object.DestroyImmediate(old.gameObject);
        var s = new GameObject("Shadow"); s.transform.SetParent(go.transform, false);
        s.transform.localPosition = new Vector3(0f, y, 0f); s.transform.localScale = new Vector3(width, width, 1f);
        var sr = s.AddComponent<SpriteRenderer>(); sr.sprite = S("shadow"); sr.sortingOrder = -60;
    }

    // =====================================================================================
    // 픽셀 도구
    // =====================================================================================
    static readonly Color Clear = new Color(0, 0, 0, 0);
    static Color Hex(string h) { ColorUtility.TryParseHtmlString("#" + h, out var c); return c; }
    static Color Darken(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, c.a);
    static Color Lighten(Color c, float k) => Color.Lerp(c, White, k);

    static Texture2D New(int w, int h, Color fill)
    {
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        t.SetPixels(Enumerable.Repeat(fill, w * h).ToArray()); return t;
    }

    static readonly HashSet<string> Generated = new HashSet<string>();
    static void Save(Texture2D t, string name) { t.Apply(); File.WriteAllBytes(Dir + name + ".png", t.EncodeToPNG()); Generated.Add(name); }

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

    static void Line(Texture2D t, int x0, int y0, int x1, int y1, int width, Color c)
    {
        int steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0));
        for (int s = 0; s <= steps; s++)
        {
            float k = steps == 0 ? 0 : s / (float)steps;
            Circle(t, Mathf.RoundToInt(Mathf.Lerp(x0, x1, k)), Mathf.RoundToInt(Mathf.Lerp(y0, y1, k)), width / 2, c);
        }
    }

    static void Chevron(Texture2D t, int cx, int cy, int size, int width, Color c, bool left, bool up = false)
    {
        if (up) { Line(t, cx - size, cy - size / 2, cx, cy + size / 2, width, c); Line(t, cx, cy + size / 2, cx + size, cy - size / 2, width, c); return; }
        int d = left ? -1 : 1;
        Line(t, cx - d * size / 2, cy + size, cx + d * size / 2, cy, width, c);
        Line(t, cx + d * size / 2, cy, cx - d * size / 2, cy - size, width, c);
    }

    static void Burst(Texture2D t, int cx, int cy, int r, int th, Color c, int rays)
    {
        CircleOutline(t, cx, cy, r, Mathf.Max(2, th), c);
        for (int k = 0; k < rays; k++)
        {
            float a = k * Mathf.PI * 2f / rays;
            Line(t, cx + (int)(Mathf.Cos(a) * r * 0.6f), cy + (int)(Mathf.Sin(a) * r * 0.6f), cx + (int)(Mathf.Cos(a) * r * 1.15f), cy + (int)(Mathf.Sin(a) * r * 1.15f), Mathf.Max(2, th), c);
        }
        Circle(t, cx, cy, Mathf.Max(2, r / 3), new Color(c.r, c.g, c.b, c.a * 0.8f));
    }

    static void Star(Texture2D t, int cx, int cy, int r, int inner, Color c)
    {
        var pts = new Vector2[10];
        for (int k = 0; k < 10; k++) { float a = Mathf.PI / 2f + k * Mathf.PI / 5f; float rr = k % 2 == 0 ? r : inner; pts[k] = new Vector2(cx + Mathf.Cos(a) * rr, cy + Mathf.Sin(a) * rr); }
        for (int j = cy - r; j <= cy + r; j++) for (int i = cx - r; i <= cx + r; i++) if (InPoly(new Vector2(i, j), pts)) Put(t, i, j, c);
    }

    static bool InPoly(Vector2 p, Vector2[] poly)
    {
        bool inside = false;
        for (int a = 0, b = poly.Length - 1; a < poly.Length; b = a++)
            if ((poly[a].y > p.y) != (poly[b].y > p.y) && p.x < (poly[b].x - poly[a].x) * (p.y - poly[a].y) / (poly[b].y - poly[a].y) + poly[a].x) inside = !inside;
        return inside;
    }

    static void Shield(Texture2D t, Color c)
    {
        for (int j = 20; j < 108; j++)
        {
            int half = j > 70 ? 38 : Mathf.RoundToInt(38f * (j - 20) / 50f);
            for (int i = 64 - half; i <= 64 + half; i++) Put(t, i, j, c);
        }
    }

    static void Blob(Texture2D t, int cx, int cy, int r, Color c)
    {
        for (int k = 0; k < 7; k++)
        {
            float a = k * Mathf.PI * 2f / 7f;
            Circle(t, cx + (int)(Mathf.Cos(a) * r * 0.45f), cy + (int)(Mathf.Sin(a) * r * 0.45f), (int)(r * (k % 2 == 0 ? 0.55f : 0.45f)), c);
        }
    }

    static void Heart(Texture2D t, Color c)
    {
        Circle(t, 20, 40, 15, c); Circle(t, 44, 40, 15, c);
        for (int row = 0; row < 30; row++) { int half = 31 - row; for (int i = 32 - half; i <= 32 + half; i++) Put(t, i, 38 - row, c); }
        Circle(t, 17, 45, 5, new Color(1, 1, 1, 0.6f));
    }
}
