using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 몬스터 길찾기 (칸 단위). 지나갈 수 있는 칸 = 바닥이면서 벽·앞면 줄이 아닌 칸.
/// 플레이어 칸에서 퍼져 나가는 거리표(BFS, 대각선 포함, 벽 모서리 끼임 방지)를 몬스터들이 같이 씀 → 벽을 돌아서 쫓아감.
/// </summary>
public static class MonsterNav
{
    static StageLayout layout;
    static HashSet<Vector2Int> open;
    static readonly Dictionary<Vector2Int, int> dist = new Dictionary<Vector2Int, int>();
    static Vector2Int distFrom = new Vector2Int(int.MinValue, 0);
    static float distAt = -1f;

    static readonly Vector2Int[] Dirs =
    {
        Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down,
        new Vector2Int(-1, 1), new Vector2Int(1, 1), new Vector2Int(-1, -1), new Vector2Int(1, -1),
    };

    public static Vector2Int Cell(Vector2 p) => new Vector2Int(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y));
    static Vector2 Center(Vector2Int c) => new Vector2(c.x + 0.5f, c.y + 0.5f);

    /// <summary>지금 스테이지 맵으로 준비 (맵 데이터가 없으면 false → 예전 방식대로 직선 이동)</summary>
    static bool Ready()
    {
        var l = StageManager.I != null && StageManager.I.Data != null ? StageManager.I.Data.layout : null;
        if (l == null || !l.use || l.walls == null || l.walls.Count == 0) return false;
        if (layout != l || open == null)
        {
            layout = l;
            var floor = l.FloorSet();
            var solid = WallBuilder.Solid(new HashSet<Vector2Int>(l.walls), floor, FxSet.Get());
            open = new HashSet<Vector2Int>();
            foreach (var c in floor) if (!solid.Contains(c)) open.Add(c);
            dist.Clear(); distFrom = new Vector2Int(int.MinValue, 0);
        }
        return open.Count > 0;
    }

    public static bool Passable(Vector2Int c) => open != null && open.Contains(c);

    static bool CanStep(Vector2Int a, Vector2Int d)
    {
        var b = a + d;
        if (!open.Contains(b)) return false;
        if (d.x != 0 && d.y != 0 && (!open.Contains(a + new Vector2Int(d.x, 0)) || !open.Contains(a + new Vector2Int(0, d.y)))) return false;   // 모서리 끼임 방지
        return true;
    }

    /// <summary>가장 가까운 지나갈 수 있는 칸 중심 (시작 위치가 벽 칸일 때)</summary>
    public static Vector2 NearestOpen(Vector2 p)
    {
        if (!Ready()) return p;
        var c = Cell(p);
        if (open.Contains(c)) return p;
        float best = float.MaxValue; Vector2 res = p;
        foreach (var o in open) { float d = (Center(o) - p).sqrMagnitude; if (d < best) { best = d; res = Center(o); } }
        return res;
    }

    static void Refresh(Vector2 target)
    {
        var start = Cell(target);
        if (!open.Contains(start)) start = Cell(NearestOpen(target));
        if (start == distFrom && Time.time - distAt < 0.25f) return;
        distFrom = start; distAt = Time.time;
        dist.Clear();
        var q = new Queue<Vector2Int>();
        dist[start] = 0; q.Enqueue(start);
        while (q.Count > 0)
        {
            var c = q.Dequeue();
            int d0 = dist[c];
            foreach (var d in Dirs)
            {
                var n = c + d;
                if (dist.ContainsKey(n) || !CanStep(c, d)) continue;
                dist[n] = d0 + 1; q.Enqueue(n);
            }
        }
    }

    /// <summary>
    /// 이번 프레임 이동 후 위치. 같은 칸·이웃 칸이면 플레이어 쪽으로 곧장, 아니면 거리표를 따라 다음 칸 중심으로.
    /// 속도는 그대로 (대각선도 같은 속도). 맵 데이터가 없으면 null.
    /// </summary>
    public static Vector2? Step(Vector2 from, Vector2 target, float maxDelta)
    {
        if (!Ready()) return null;
        Refresh(target);
        var c = Cell(from);
        if (!open.Contains(c)) return Vector2.MoveTowards(from, NearestOpen(from), maxDelta);   // 벽 안이면 빠져나옴
        if (!dist.TryGetValue(c, out int here)) return from;                                     // 갈 수 없는 구역
        Vector2 goal;
        if (here <= 1) goal = target;
        else
        {
            var bestCell = c; int best = here;
            foreach (var d in Dirs)
                if (CanStep(c, d) && dist.TryGetValue(c + d, out int v) && v < best) { best = v; bestCell = c + d; }
            goal = Center(bestCell);
        }
        var next = Vector2.MoveTowards(from, goal, maxDelta);
        return open.Contains(Cell(next)) ? next : Vector2.MoveTowards(from, Center(c), maxDelta);
    }
}
