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
            Piece(set, c, out var sprite, out float angle, fx, floor);
            var t = new GameObject("Wall");
            t.transform.SetParent(root.transform, false);
            t.transform.position = new Vector3(c.x + 0.5f, c.y + 0.5f, 0f);
            t.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            t.layer = layer;
            t.tag = "Wall";
            var sr = t.AddComponent<SpriteRenderer>();
            sr.sprite = sprite; sr.sortingOrder = 0;
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
        if (floor.Contains(c + Vector2Int.left)) Line(new Vector2(-h, 0f), new Vector2(EdgeWidth, 1f));
        if (floor.Contains(c + Vector2Int.right)) Line(new Vector2(h, 0f), new Vector2(EdgeWidth, 1f));
        if (floor.Contains(c + Vector2Int.up)) Line(new Vector2(0f, h), new Vector2(1f, EdgeWidth));
    }

    /// <summary>
    /// 바닥 칸마다 바닥 그림을 깔고, 바닥도 벽도 아닌 빈칸(방 둘레 한 칸 포함)은 그림 없이 막음 → 빈칸은 배경색, 못 들어감.
    /// </summary>
    public static GameObject BuildFloor(HashSet<Vector2Int> floor, ICollection<Vector2Int> walls, int w, int h, Sprite fallback, int sortingOrder, int layer, ICollection<Rect> workAreas = null)
    {
        var root = new GameObject("Floor (StageLayout)");
        var wallSet = new HashSet<Vector2Int>(walls);
        var fx = FxSet.Get();
        foreach (var c in floor)
        {
            var sprite = FloorSprite(c, fx, workAreas) ?? fallback;
            var t = new GameObject("FloorTile");
            t.transform.SetParent(root.transform, false);
            t.transform.position = new Vector3(c.x + 0.5f, c.y + 0.5f, 0f);
            var sr = t.AddComponent<SpriteRenderer>();
            sr.sprite = sprite; sr.sortingOrder = sortingOrder;
            if (sprite != null) t.transform.localScale = new Vector3(1f / sprite.bounds.size.x, 1f / sprite.bounds.size.y, 1f);
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

    public static void Piece(HashSet<Vector2Int> cells, Vector2Int c, out Sprite sprite, out float angle, FxSet fx, HashSet<Vector2Int> floor = null)
    {
        // 입체 타일이 있으면 윗면·앞면·모서리 방식 (회전 없음)
        if (fx != null && fx.Has3Q && floor != null) { angle = 0f; sprite = Piece3Q(cells, floor, c, fx); return; }
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
    /// 입체(3/4 시점) 벽: 방 귀퉁이처럼 ㄱ자 안쪽이 바닥이면 바깥 모서리, 벽 덩어리가 바닥 쪽으로 꺾이는 곳은 안쪽 모서리,
    /// 바로 아래 칸이 바닥이면 앞면(바닥에서 보이는 벽 옆면), 나머지는 윗면. 모서리 순서: 좌상·우상·좌하·우하
    /// </summary>
    static Sprite Piece3Q(HashSet<Vector2Int> w, HashSet<Vector2Int> floor, Vector2Int c, FxSet fx)
    {
        bool L = w.Contains(c + new Vector2Int(-1, 0)), R = w.Contains(c + new Vector2Int(1, 0)), U = w.Contains(c + new Vector2Int(0, 1)), D = w.Contains(c + new Vector2Int(0, -1));
        bool UL = w.Contains(c + new Vector2Int(-1, 1)), UR = w.Contains(c + new Vector2Int(1, 1)), DL = w.Contains(c + new Vector2Int(-1, -1)), DR = w.Contains(c + new Vector2Int(1, -1));
        Sprite Or(Sprite s) => s != null ? s : fx.wallTopTile;
        if (R && D && !L && !U && !DR) return Or(fx.cornerOut[0]);
        if (L && D && !R && !U && !DL) return Or(fx.cornerOut[1]);
        if (R && U && !L && !D && !UR) return Or(fx.cornerOut[2]);
        if (L && U && !R && !D && !UL) return Or(fx.cornerOut[3]);
        if (D && R && DR && !U && !L) return Or(fx.cornerIn[0]);
        if (D && L && DL && !U && !R) return Or(fx.cornerIn[1]);
        if (U && R && UR && !L && !D) return Or(fx.cornerIn[2]);
        if (U && L && UL && !R && !D) return Or(fx.cornerIn[3]);
        if (floor.Contains(c + new Vector2Int(0, -1)) && fx.wallFront != null && fx.wallFront.Length > 0)
            return Or(fx.wallFront[Hash(c) % fx.wallFront.Length]);
        return fx.wallTopTile;
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
