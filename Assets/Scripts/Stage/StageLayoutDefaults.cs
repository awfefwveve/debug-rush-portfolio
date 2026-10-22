using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>기본 맵 (기획서 p.18) + 자동 생성. Stage Maker와 런타임이 함께 사용.</summary>
public static class StageLayoutDefaults
{
    // 물체가 차지하는 칸 크기 (가로, 세로)
    /// <summary>아이템 생성기·제작대·단말기 그림 크기 배율 (10/22 1.25배)</summary>
    public const float FacilityScale = 1.25f;
    public static readonly Vector2 StationSize = new Vector2(3f, 2.5f) * FacilityScale, CrafterSize = new Vector2(4f, 3f) * FacilityScale, TerminalSize = new Vector2(2.5f, 3.5f) * FacilityScale;

    /// <summary>좌하단 보유 자료 박스(화면 190×120)가 겹치는 자리 (물체 금지). 카메라 줌에 따라 칸 수가 달라짐 (30×16이면 3×2칸)</summary>
    public static Rect HudZone(RectInt b)
    {
        float ppu = Mathf.Min(1920f / Mathf.Max(1, b.width), 1024f / Mathf.Max(1, b.height));
        return new Rect(b.xMin, b.yMin, Mathf.Ceil(190f / ppu - 0.05f), Mathf.Ceil(120f / ppu - 0.05f));
    }
    public static Rect HudZone(int w, int h) => HudZone(new RectInt(WallBuilder.MinX(w), WallBuilder.MinY(h), w, h));

    /// <summary>기획서 p.18 기본 배치 (30×16): 그래픽·QA 위 / 기획·프로그래밍 아래, 가운데 작업대 라인 위 제작대, 단말기 우상단·좌하단, 좌상단·우하단 튀어나온 벽</summary>
    public static StageLayout Default()
    {
        int W = WallBuilder.DefaultW, H = WallBuilder.DefaultH;
        var l = new StageLayout { use = true, width = W, height = H };
        var w = new HashSet<Vector2Int>(WallBuilder.Border(W, H));
        for (int x = -8; x <= -3; x++) w.Add(new Vector2Int(x, 0));          // 작업대 라인 왼쪽
        for (int x = 2; x <= 7; x++) w.Add(new Vector2Int(x, 0));            // 작업대 라인 오른쪽
        foreach (var c in Block(-14, 5)) w.Add(c);                           // 좌상단 튀어나온 벽
        foreach (var c in Block(12, -7)) w.Add(c);                           // 우하단
        l.walls = w.ToList();
        l.playerStart = new Vector2(0f, -4.5f);
        l.stations = new List<StationSpot>
        {
            new StationSpot { material = ItemType.Graphic, pos = new Vector2(-10f, 4.5f) }, new StationSpot { material = ItemType.QA, pos = new Vector2(-5f, 4.5f) },
            new StationSpot { material = ItemType.Planning, pos = new Vector2(5f, -4.5f) }, new StationSpot { material = ItemType.Programming, pos = new Vector2(10f, -4.5f) },
        };
        l.crafters = new List<Vector2> { Vector2.zero };
        l.MarkListsReady();
        l.terminals = new[] { new Vector2(11.5f, 3f), new Vector2(-11.5f, -3f) };
        return l;
    }

    /// <summary>자동 생성: 외곽 벽 + 가운데 라인(있을 수도 없을 수도) + 물체 랜덤 배치 (겹치지 않게)</summary>
    public static StageLayout Random(int seed, int terminalCount, int width, int height, out Vector2 monsterSpawn)
    {
        // 하나라도 못 놓으면 다른 배치로 다시 (항상 겹침 없는 결과)
        for (int k = 0; k < 100; k++)
        {
            var l = TryRandom(seed * 31 + k, terminalCount, width, height, out monsterSpawn, out bool ok);
            if (ok) return l;
        }
        monsterSpawn = new Vector2(3f, 3f);
        return Default();
    }

    static StageLayout TryRandom(int seed, int terminalCount, int W, int H, out Vector2 monsterSpawn, out bool ok)
    {
        bool good = true;
        var rnd = new System.Random(seed);
        var l = new StageLayout { use = true, width = W, height = H };
        var w = new HashSet<Vector2Int>(WallBuilder.Border(W, H));
        int x0 = WallBuilder.MinX(W), x1 = WallBuilder.MaxX(W), y0 = WallBuilder.MinY(H), y1 = WallBuilder.MaxY(H);
        float halfW = W / 2f - 1f, halfH = H / 2f - 1f;               // 안쪽 반폭
        int line = Mathf.Max(2, W / 6);
        int variant = rnd.Next(3);
        if (variant == 0) { for (int x = -line - 3; x <= -3; x++) w.Add(new Vector2Int(x, 0)); for (int x = 2; x <= line + 2; x++) w.Add(new Vector2Int(x, 0)); }
        else if (variant == 1) { for (int y = -2; y <= 1; y++) { w.Add(new Vector2Int(-W / 5, y)); w.Add(new Vector2Int(W / 5 - 1, y)); } }
        // 모서리 블록 랜덤
        if (rnd.Next(2) == 0) foreach (var c in Block(x0 + 1, y1 - 2)) w.Add(c);
        if (rnd.Next(2) == 0) foreach (var c in Block(x1 - 2, y0 + 1)) w.Add(c);
        if (rnd.Next(2) == 0) foreach (var c in Block(x1 - 2, y1 - 2)) w.Add(c);
        l.walls = w.ToList();

        var zone = HudZone(W, H);
        var placed = new List<Rect> { new Rect(zone.x - 0.6f, zone.y - 0.6f, zone.width + 0.6f, zone.height + 0.6f) };
        Vector2 Place(Vector2 size, float yMin, float yMax)
        {
            for (int tries = 0; tries < 200; tries++)
            {
                float x = Snap(Lerp(rnd, -halfW + 1f + size.x / 2f, halfW - 1f - size.x / 2f));
                float y = Snap(Lerp(rnd, yMin, yMax));
                var r = new Rect(x - size.x / 2f - 0.6f, y - size.y / 2f - 0.6f, size.x + 1.2f, size.y + 1.2f);
                if (placed.Any(p => p.Overlaps(r))) continue;
                if (HitsWall(w, new Vector2(x, y), size)) continue;
                placed.Add(r);
                return new Vector2(x, y);
            }
            good = false;
            return Vector2.zero;
        }
        float top = halfH - 1.4f, low = -halfH + 1.4f;              // 재료대 중심이 들어갈 수 있는 위·아래 한계
        l.crafters = new List<Vector2> { Place(CrafterSize, -1f, 1f) };
        l.stations = new List<StationSpot>
        {
            new StationSpot { material = ItemType.Graphic, pos = Place(StationSize, 2.5f, top) }, new StationSpot { material = ItemType.QA, pos = Place(StationSize, 2.5f, top) },
            new StationSpot { material = ItemType.Planning, pos = Place(StationSize, low, -2.5f) }, new StationSpot { material = ItemType.Programming, pos = Place(StationSize, low, -2.5f) },
        };
        l.MarkListsReady();
        var terms = new List<Vector2>();
        for (int i = 0; i < Mathf.Max(1, terminalCount); i++) terms.Add(Place(TerminalSize, -halfH + 1.9f, halfH - 1.9f));
        l.terminals = terms.ToArray();
        l.playerStart = Place(new Vector2(1.2f, 1.2f), -halfH + 1.6f, -halfH + 2.4f) - new Vector2(0f, 0.5f);   // 발 위치 = 칸 중심 - 0.5
        monsterSpawn = Place(new Vector2(2f, 2f), 1f, halfH - 1.5f);
        ok = good;
        return l;
    }

    static IEnumerable<Vector2Int> Block(int x, int y) { yield return new Vector2Int(x, y); yield return new Vector2Int(x + 1, y); yield return new Vector2Int(x, y + 1); yield return new Vector2Int(x + 1, y + 1); }
    static float Lerp(System.Random r, float a, float b) => a + (float)r.NextDouble() * (b - a);
    public static float Snap(float v) => Mathf.Round(v * 2f) / 2f;

    public static bool HitsWall(ICollection<Vector2Int> walls, Vector2 center, Vector2 size)
    {
        for (int x = Mathf.FloorToInt(center.x - size.x / 2f + 0.05f); x <= Mathf.FloorToInt(center.x + size.x / 2f - 0.05f); x++)
            for (int y = Mathf.FloorToInt(center.y - size.y / 2f + 0.05f); y <= Mathf.FloorToInt(center.y + size.y / 2f - 0.05f); y++)
                if (walls.Contains(new Vector2Int(x, y))) return true;
        return false;
    }
}
