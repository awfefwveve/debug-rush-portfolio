using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 벽 칸 목록 하나로 벽 그림과 충돌을 같이 만듦 → "보이는 벽 = 막히는 벽".
/// 칸마다 이웃을 보고 연결 조각(단독·끝·직선·코너·T자·십자)과 회전을 고름.
/// 조각 기본 방향: 끝=아래 연결, 직선=좌우, 코너=오른쪽+아래, T자=좌·우·아래.
/// </summary>
public static class WallBuilder
{
    /// <summary>방 격자: x -14~13, y -6~5 (28×12칸). 칸 (x,y)의 중심 = (x+0.5, y+0.5)</summary>
    public const int MinX = -14, MaxX = 13, MinY = -6, MaxY = 5;

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

    public static bool InRoom(Vector2Int c) => c.x >= MinX && c.x <= MaxX && c.y >= MinY && c.y <= MaxY;

    /// <summary>바깥 테두리 칸</summary>
    public static IEnumerable<Vector2Int> Border()
    {
        for (int x = MinX; x <= MaxX; x++) { yield return new Vector2Int(x, MinY); yield return new Vector2Int(x, MaxY); }
        for (int y = MinY + 1; y < MaxY; y++) { yield return new Vector2Int(MinX, y); yield return new Vector2Int(MaxX, y); }
    }
}
