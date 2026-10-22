using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 벽 칸 목록 하나로 벽 그림과 충돌을 같이 만듦 → "보이는 벽 = 막히는 벽".
/// 칸마다 이웃을 보고 연결 조각(단독·끝·직선·코너·T자·십자)과 회전을 고름.
/// 조각 기본 방향: 끝=아래 연결, 직선=좌우, 코너=오른쪽+아래, T자=좌·우·아래.
/// </summary>
public static class WallBuilder
{
    /// <summary>기본 방 크기 (칸). 1칸 = 화면 64px 기준 30×16 = 1920×1024 (상단 바 56px 아래 꽉 참)</summary>
    public const int DefaultW = 30, DefaultH = 16;

    /// <summary>방 격자: 가운데 기준 x -W/2 ~ W/2-1, y -H/2 ~ H/2-1. 칸 (x,y)의 중심 = (x+0.5, y+0.5)</summary>
    public static int MinX(int w) => -w / 2;
    public static int MaxX(int w) => -w / 2 + w - 1;
    public static int MinY(int h) => -h / 2;
    public static int MaxY(int h) => -h / 2 + h - 1;

    public static GameObject Build(ICollection<Vector2Int> cells, int layer, HashSet<Vector2Int> floor = null)
    {
        var root = new GameObject("Walls (StageLayout)");
        var set = new HashSet<Vector2Int>(cells);
        var fx = FxSet.Get();
        foreach (var c in set)
        {
            Piece(set, c, out var sprite, out float angle, out bool flip, fx, floor);
            var t = new GameObject("Wall");
            t.transform.SetParent(root.transform, false);
            t.transform.position = new Vector3(c.x + 0.5f, c.y + 0.5f, 0f);
            t.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            t.layer = layer;
            t.tag = "Wall";
            var sr = t.AddComponent<SpriteRenderer>();
            sr.sprite = sprite; sr.sortingOrder = 0; sr.flipX = flip;
            var box = t.AddComponent<BoxCollider2D>();
            box.size = Vector2.one;
            if (fx != null && fx.Has3Q && floor != null) AddEdgeLines(t.transform, set, floor, c);
        }
        return root;
    }

    /// <summary>입체 타일: 벽이 바닥과 맞닿은 왼쪽·오른쪽·위 변에 진한 경계선 (2px). 아래 변은 앞면 그림이 맡음</summary>
    public const float EdgeWidth = 1f / 16f;
    public static readonly Color EdgeColor = new Color(0.04f, 0.06f, 0.11f, 0.9f);
    static Sprite edgeSprite;

    /// <summary>벽 옆 칸이 실제 바닥인지 (벽 칸은 제외)</summary>
    public static bool Open(HashSet<Vector2Int> walls, HashSet<Vector2Int> floor, Vector2Int n) => !walls.Contains(n) && floor.Contains(n);

    static void AddEdgeLines(Transform cell, HashSet<Vector2Int> walls, HashSet<Vector2Int> floor, Vector2Int c)
    {
        if (edgeSprite == null) edgeSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        void Line(Vector2 offset, Vector2 size)
        {
            var g = new GameObject("Edge");
            g.transform.SetParent(cell, false);
            g.transform.localPosition = offset; g.transform.localScale = new Vector3(size.x, size.y, 1f);
            var sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = edgeSprite; sr.color = EdgeColor; sr.sortingOrder = 1;
        }
        float h = 0.5f - EdgeWidth / 2f;
        if (Open(walls, floor, c + Vector2Int.left)) Line(new Vector2(-h, 0f), new Vector2(EdgeWidth, 1f));
        if (Open(walls, floor, c + Vector2Int.right)) Line(new Vector2(h, 0f), new Vector2(EdgeWidth, 1f));
        if (Open(walls, floor, c + Vector2Int.up)) Line(new Vector2(0f, h), new Vector2(1f, EdgeWidth));
    }

    /// <summary>
    /// 바닥 칸마다 바닥 그림을 깔고, 바닥도 벽도 아닌 빈칸(방 둘레 한 칸 포함)은 그림 없이 막음 → 빈칸은 배경색, 못 들어감.
    /// </summary>
    public static GameObject BuildFloor(HashSet<Vector2Int> floor, ICollection<Vector2Int> walls, int w, int h, Sprite fallback, int sortingOrder, int layer, ICollection<Rect> workAreas = null)
    {
        var root = new GameObject("Floor (StageLayout)");
        var wallSet = new HashSet<Vector2Int>(walls);
        var fx = FxSet.Get();
        foreach (var p in FloorLayout(floor, fx, workAreas))
        {
            var sprite = p.sprite != null ? p.sprite : fallback;
            var t = new GameObject("FloorTile");
            t.transform.SetParent(root.transform, false);
            t.transform.position = new Vector3(p.center.x, p.center.y, 0f);
            var sr = t.AddComponent<SpriteRenderer>();
            sr.sprite = sprite; sr.sortingOrder = sortingOrder;
            if (sprite != null) t.transform.localScale = new Vector3(p.size / sprite.bounds.size.x, p.size / sprite.bounds.size.y, 1f);
        }
        for (int x = MinX(w) - 1; x <= MaxX(w) + 1; x++)
            for (int y = MinY(h) - 1; y <= MaxY(h) + 1; y++)
            {
                var c = new Vector2Int(x, y);
                if (floor.Contains(c) || wallSet.Contains(c)) continue;
                var v = new GameObject("Void");
                v.transform.SetParent(root.transform, false);
                v.transform.position = new Vector3(x + 0.5f, y + 0.5f, 0f);
                v.layer = layer; v.tag = "Wall";
                v.AddComponent<BoxCollider2D>().size = Vector2.one;
            }
        return root;
    }

    public static void Piece(HashSet<Vector2Int> cells, Vector2Int c, out Sprite sprite, out float angle, out bool flipX, FxSet fx, HashSet<Vector2Int> floor = null)
    {
        // 입체 타일이 있으면 윗면·앞면·모서리 방식 (회전 없음)
        flipX = false;
        if (fx != null && fx.Has3Q && floor != null) { angle = 0f; sprite = Piece3Q(cells, floor, c, fx, out flipX); return; }
        bool l = cells.Contains(c + Vector2Int.left), r = cells.Contains(c + Vector2Int.right),
             d = cells.Contains(c + Vector2Int.down), u = cells.Contains(c + Vector2Int.up);
        int n = (l ? 1 : 0) + (r ? 1 : 0) + (d ? 1 : 0) + (u ? 1 : 0);
        angle = 0f;
        if (n == 0) sprite = fx?.wallSingle;
        else if (n == 4) sprite = fx?.wallCross;
        else if (n == 1) { sprite = fx?.wallEnd; angle = d ? 0f : r ? 90f : u ? 180f : 270f; }
        else if (n == 3) { sprite = fx?.wallT; angle = !u ? 0f : !l ? 90f : !d ? 180f : 270f; }
        else if ((l && r) || (u && d)) { sprite = fx?.wallStraight; angle = l && r ? 0f : 90f; }
        else { sprite = fx?.wallCorner; angle = r && d ? 0f : u && r ? 90f : l && u ? 180f : 270f; }
    }

    /// <summary>
    /// 입체(3/4 시점) 벽 (우선순위 순): 방 테두리 꼭짓점 = 바깥 모서리, 바로 아래가 바닥 = 앞면, 방이 파인 꼭짓점 = 안쪽 모서리, 나머지 = 윗면.
    /// 이웃 4방향 벽·바닥 여부로 판정 (회전 없음, 오른쪽 그림이 없으면 왼쪽 그림 좌우 반전). 모서리 순서: 좌상·우상·좌하·우하
    /// </summary>
    static Sprite Piece3Q(HashSet<Vector2Int> w, HashSet<Vector2Int> floor, Vector2Int c, FxSet fx, out bool flip)
    {
        flip = false;
        bool L = w.Contains(c + Vector2Int.left), R = w.Contains(c + Vector2Int.right), U = w.Contains(c + Vector2Int.up), D = w.Contains(c + Vector2Int.down);
        // 바닥 = 바닥 칸이면서 벽이 아닌 칸 (바닥을 따로 칠하지 않은 맵은 방 안쪽 전체가 바닥 목록이라 벽 칸도 들어 있음)
        bool fL = !L && floor.Contains(c + Vector2Int.left), fR = !R && floor.Contains(c + Vector2Int.right), fU = !U && floor.Contains(c + Vector2Int.up), fD = !D && floor.Contains(c + Vector2Int.down);
        Sprite Or(Sprite s) => s != null ? s : fx.wallTopTile;

        // ㄱ자 꺾임: 수직인 두 이웃만 벽, 나머지 두 변은 벽이 아님. k = 벽 쪽 (0 좌상 · 1 우상 · 2 좌하 · 3 우하)
        int k = -1;
        if (R && D && !L && !U) k = 3; else if (L && D && !R && !U) k = 2; else if (R && U && !L && !D) k = 1; else if (L && U && !R && !D) k = 0;
        // 바깥 모서리: 열린 두 변이 바닥이 아님(방 밖) = 방 테두리의 꼭짓점. 그림 이름은 방 밖 쪽 (out_tl: 위·왼쪽이 밖, 오른쪽 아래가 방)
        if (k >= 0)
        {
            bool openIsFloor = k == 3 ? fU || fL : k == 2 ? fU || fR : k == 1 ? fD || fL : fD || fR;
            if (!openIsFloor) { int o = 3 - k; flip = fx.cornerOutFlip != null && fx.cornerOutFlip[o]; return Or(fx.cornerOut[o]); }
        }
        // 앞면: 바로 아래가 바닥이면 바닥에서 보이는 벽 옆면 (01·02 칸마다 고정 무작위)
        if (fD && fx.wallFront != null && fx.wallFront.Length > 0)
            return Or(fx.wallFront[Hash(c) % fx.wallFront.Length]);
        // 안쪽 모서리: 방이 파인 곳(벽이 방 안으로 꺾여 들어온 꼭짓점), 열린 두 변이 바닥. 그림 이름은 벽 덩어리 쪽 (in_br: 오른쪽·아래가 벽, 위·왼쪽이 바닥)
        // (위쪽이 파인 곳 in_tl·in_tr은 아래가 바닥이라 위의 앞면 규칙이 먼저 적용됨 — 3/4 시점에서 그 자리는 벽 옆면이 보임)
        if (k >= 0) { flip = fx.cornerInFlip != null && fx.cornerInFlip[k]; return Or(fx.cornerIn[k]); }
        return fx.wallTopTile;
    }

    public struct FloorPiece { public Vector2 center; public float size; public Sprite sprite; }

    /// <summary>바닥 깔기 계획: 2×2가 전부 바닥인 자리 중 일부에 큰 바닥(64×64), 나머지는 칸마다 바닥 타일. 게임과 Stage Maker가 같이 씀</summary>
    public static List<FloorPiece> FloorLayout(HashSet<Vector2Int> floor, FxSet fx, ICollection<Rect> workAreas)
    {
        var list = new List<FloorPiece>();
        var used = new HashSet<Vector2Int>();
        bool Work(Vector2Int c) { if (workAreas == null) return false; foreach (var a in workAreas) if (a.Contains(new Vector2(c.x + 0.5f, c.y + 0.5f))) return true; return false; }
        var ordered = new List<Vector2Int>(floor);
        ordered.Sort((a, b) => a.y != b.y ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));   // 게임·툴에서 같은 결과
        if (fx != null && fx.tileFloorLarge != null && fx.tileFloorLarge.Length > 0)
            foreach (var c in ordered)
            {
                if ((c.x & 1) != 0 || (c.y & 1) != 0 || Hash(c) % 5 != 0) continue;
                var four = new[] { c, c + Vector2Int.right, c + Vector2Int.up, c + Vector2Int.one };
                bool ok = true;
                foreach (var q in four) if (!floor.Contains(q) || used.Contains(q) || Work(q)) { ok = false; break; }
                if (!ok) continue;
                foreach (var q in four) used.Add(q);
                list.Add(new FloorPiece { center = new Vector2(c.x + 1f, c.y + 1f), size = 2f, sprite = fx.tileFloorLarge[Hash(c) % fx.tileFloorLarge.Length] });
            }
        foreach (var c in ordered)
            if (!used.Contains(c)) list.Add(new FloorPiece { center = new Vector2(c.x + 0.5f, c.y + 0.5f), size = 1f, sprite = FloorSprite(c, fx, workAreas) });
        return list;
    }

    /// <summary>바닥 칸 그림: 제작대 주변은 작업대 바닥, 나머지는 변형 중 칸마다 고정 무작위</summary>
    public static Sprite FloorSprite(Vector2Int c, FxSet fx, ICollection<Rect> workAreas)
    {
        if (fx == null) return null;
        if (fx.tileWorkFloor != null && workAreas != null)
            foreach (var a in workAreas) if (a.Contains(new Vector2(c.x + 0.5f, c.y + 0.5f))) return fx.tileWorkFloor;
        if (fx.tileFloor != null && fx.tileFloor.Length > 0) return fx.tileFloor[Hash(c) % fx.tileFloor.Length];
        return null;
    }

    static int Hash(Vector2Int c) => (int)((uint)(c.x * 73856093 ^ c.y * 19349663) % 1000003u);

    public static bool InRoom(Vector2Int c, int w, int h) => c.x >= MinX(w) && c.x <= MaxX(w) && c.y >= MinY(h) && c.y <= MaxY(h);

    /// <summary>바깥 테두리 칸</summary>
    public static IEnumerable<Vector2Int> Border(int w, int h)
    {
        for (int x = MinX(w); x <= MaxX(w); x++) { yield return new Vector2Int(x, MinY(h)); yield return new Vector2Int(x, MaxY(h)); }
        for (int y = MinY(h) + 1; y < MaxY(h); y++) { yield return new Vector2Int(MinX(w), y); yield return new Vector2Int(MaxX(w), y); }
    }
}
