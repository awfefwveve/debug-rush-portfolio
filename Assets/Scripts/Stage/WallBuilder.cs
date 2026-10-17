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

    public static GameObject Build(ICollection<Vector2Int> cells, int layer)
    {
        var root = new GameObject("Walls (StageLayout)");
        var set = new HashSet<Vector2Int>(cells);
        var fx = FxSet.Get();
        foreach (var c in set)
        {
            Piece(set, c, out var sprite, out float angle, fx);
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
        }
        return root;
    }

    /// <summary>
    /// 바닥 칸마다 바닥 그림을 깔고, 바닥도 벽도 아닌 빈칸(방 둘레 한 칸 포함)은 그림 없이 막음 → 빈칸은 배경색, 못 들어감.
    /// </summary>
    public static GameObject BuildFloor(HashSet<Vector2Int> floor, ICollection<Vector2Int> walls, int w, int h, Sprite sprite, int sortingOrder, int layer)
    {
        var root = new GameObject("Floor (StageLayout)");
        var wallSet = new HashSet<Vector2Int>(walls);
        foreach (var c in floor)
        {
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

    public static void Piece(HashSet<Vector2Int> cells, Vector2Int c, out Sprite sprite, out float angle, FxSet fx)
    {
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

    public static bool InRoom(Vector2Int c, int w, int h) => c.x >= MinX(w) && c.x <= MaxX(w) && c.y >= MinY(h) && c.y <= MaxY(h);

    /// <summary>바깥 테두리 칸</summary>
    public static IEnumerable<Vector2Int> Border(int w, int h)
    {
        for (int x = MinX(w); x <= MaxX(w); x++) { yield return new Vector2Int(x, MinY(h)); yield return new Vector2Int(x, MaxY(h)); }
        for (int y = MinY(h) + 1; y < MaxY(h); y++) { yield return new Vector2Int(MinX(w), y); yield return new Vector2Int(MaxX(w), y); }
    }
}
