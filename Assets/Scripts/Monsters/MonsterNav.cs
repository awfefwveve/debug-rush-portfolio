using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 몬스터 길찾기 (칸 단위). 지나갈 수 있는 칸 = 바닥이면서 벽·앞면 줄이 아닌 칸.
/// 걷는 몬스터(몬스터2·보스)는 기계(생성기·제작대·단말기) 자리도 못 지나감, 나는 벌은 기계 위로는 지나감.
/// 플레이어 칸에서 퍼져 나가는 거리표(BFS, 대각선 포함, 벽 모서리 끼임 방지)를 몬스터들이 같이 씀 → 벽·기계를 돌아서 쫓아감.
/// </summary>
public static class MonsterNav
{
    class Grid
    {
        public HashSet<Vector2Int> open;
        public readonly Dictionary<Vector2Int, int> dist = new Dictionary<Vector2Int, int>();
        public Vector2Int from = new Vector2Int(int.MinValue, 0);
        public float at = -1f;
    }

    static StageLayout layout;
    static Grid fly, walk;
    static readonly List<Rect> machineRects = new List<Rect>();

    static readonly Vector2Int[] Dirs =
    {
        Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down,
        new Vector2Int(-1, 1), new Vector2Int(1, 1), new Vector2Int(-1, -1), new Vector2Int(1, -1),
    };

    public static Vector2Int Cell(Vector2 p) => new Vector2Int(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y));
    static Vector2 Center(Vector2Int c) => new Vector2(c.x + 0.5f, c.y + 0.5f);

    // 기계 몸통 (게임 콜라이더와 같은 크기: 생성기 2.7×1.9 · 제작대 3.8×2.4 · 단말기 2.2×3.0, × 시설 배율, 중심 0.1 아래)
    static readonly Vector2 StationBody = new Vector2(2.7f, 1.9f), CrafterBody = new Vector2(3.8f, 2.4f), TerminalBody = new Vector2(2.2f, 3.0f);

    /// <summary>지금 스테이지 맵으로 준비 (맵 데이터가 없으면 false → 예전 방식대로 직선 이동)</summary>
    static bool Ready()
    {
        var l = StageManager.I != null && StageManager.I.Data != null ? StageManager.I.Data.layout : null;
        if (l == null || !l.use || l.walls == null || l.walls.Count == 0) return false;
        if (layout != l || fly == null)
        {
            layout = l;
            var floor = l.FloorSet();
            var solid = WallBuilder.Solid(new HashSet<Vector2Int>(l.walls), floor, FxSet.Get());
            fly = new Grid { open = new HashSet<Vector2Int>() };
            foreach (var c in floor) if (!solid.Contains(c)) fly.open.Add(c);

            var machines = machineRects; machines.Clear();
            float k = StageLayoutDefaults.FacilityScale;
            void Add(Vector2 pos, Vector2 body) { var s = body * k; machines.Add(new Rect(pos.x - s.x / 2f, pos.y - 0.1f * k - s.y / 2f, s.x, s.y)); }
            foreach (var st in l.stations) Add(st.pos, StationBody);
            foreach (var c in l.crafters) Add(c, CrafterBody);
            if (l.terminals != null) foreach (var t in l.terminals) Add(t, TerminalBody);
            walk = new Grid { open = new HashSet<Vector2Int>() };
            foreach (var c in fly.open)
            {
                var cell = new Rect(c.x + 0.15f, c.y + 0.15f, 0.7f, 0.7f);   // 칸이 기계 몸통과 조금이라도 겹치면 막힘
                bool blocked = false;
                foreach (var m in machines) if (m.Overlaps(cell)) { blocked = true; break; }
                if (!blocked) walk.open.Add(c);
            }
        }
        return fly.open.Count > 0;
    }

    static bool CanStep(Grid g, Vector2Int a, Vector2Int d)
    {
        var b = a + d;
        if (!g.open.Contains(b)) return false;
        if (d.x != 0 && d.y != 0 && (!g.open.Contains(a + new Vector2Int(d.x, 0)) || !g.open.Contains(a + new Vector2Int(0, d.y)))) return false;   // 모서리 끼임 방지
        return true;
    }

    static Vector2 NearestOpen(Grid g, Vector2 p)
    {
        var c = Cell(p);
        if (g.open.Contains(c)) return p;
        float best = float.MaxValue; Vector2 res = p;
        foreach (var o in g.open) { float d = (Center(o) - p).sqrMagnitude; if (d < best) { best = d; res = Center(o); } }
        return res;
    }

    /// <summary>걷는 몬스터 기준 가장 가까운 빈 바닥 (벽·기계 칸이면 옮김). 폭탄 설치 자리</summary>
    public static Vector2 NearestWalkable(Vector2 p) => Ready() ? NearestOpen(walk, p) : p;

    /// <summary>발밑이 어떤 기계의 아래쪽 경계보다 위(뒤)이고 가로로 겹치는가 → 몬스터를 기계 뒤로 그림</summary>
    public static bool BehindMachine(Vector2 foot, float halfWidth)
    {
        if (!Ready()) return false;
        foreach (var m in machineRects)
            if (foot.y > m.yMin && foot.y < m.yMax + 3f && foot.x + halfWidth > m.xMin && foot.x - halfWidth < m.xMax) return true;
        return false;
    }

    /// <summary>가장 가까운 지나갈 수 있는 칸 중심 (시작 위치가 벽 칸일 때)</summary>
    public static Vector2 NearestOpen(Vector2 p) => Ready() ? NearestOpen(fly, p) : p;

    static void Refresh(Grid g, Vector2 target)
    {
        var start = Cell(target);
        if (!g.open.Contains(start)) start = Cell(NearestOpen(g, target));   // 플레이어가 기계에 붙어 있으면 가장 가까운 빈칸 기준
        if (start == g.from && Time.time - g.at < 0.25f) return;
        g.from = start; g.at = Time.time;
        g.dist.Clear();
        var q = new Queue<Vector2Int>();
        g.dist[start] = 0; q.Enqueue(start);
        while (q.Count > 0)
        {
            var c = q.Dequeue();
            int d0 = g.dist[c];
            foreach (var d in Dirs)
            {
                var n = c + d;
                if (g.dist.ContainsKey(n) || !CanStep(g, c, d)) continue;
                g.dist[n] = d0 + 1; q.Enqueue(n);
            }
        }
    }

    /// <summary>
    /// 이번 프레임 이동 후 위치. 같은 칸·이웃 칸이면 플레이어 쪽으로 곧장, 아니면 거리표를 따라 다음 칸 중심으로.
    /// 속도는 그대로 (대각선도 같은 속도). walks = 걷는 몬스터(기계도 못 지나감). 맵 데이터가 없으면 null.
    /// </summary>
    public static Vector2? Step(Vector2 from, Vector2 target, float maxDelta, bool walks = false)
    {
        if (!Ready()) return null;
        var g = walks ? walk : fly;
        Refresh(g, target);
        var c = Cell(from);
        if (!g.open.Contains(c)) return Vector2.MoveTowards(from, NearestOpen(g, from), maxDelta);   // 벽·기계 안이면 빠져나옴
        if (!g.dist.TryGetValue(c, out int here)) return from;                                          // 갈 수 없는 구역
        Vector2 goal;
        if (here <= 1) goal = g.open.Contains(Cell(target)) ? target : Center(g.from);
        else
        {
            var bestCell = c; int best = here;
            foreach (var d in Dirs)
                if (CanStep(g, c, d) && g.dist.TryGetValue(c + d, out int v) && v < best) { best = v; bestCell = c + d; }
            goal = Center(bestCell);
        }
        var next = Vector2.MoveTowards(from, goal, maxDelta);
        return g.open.Contains(Cell(next)) ? next : Vector2.MoveTowards(from, Center(c), maxDelta);
    }
}
