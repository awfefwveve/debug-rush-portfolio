using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>StageData.layout ↔ Stage 씬 오브젝트 위치. 런타임(StageManager)과 Stage Maker 툴이 함께 사용.</summary>
public static class StageLayoutApplier
{
    /// <summary>배치 적용. allowClone=true면 단말기가 모자랄 때 첫 단말기를 복제 (런타임 전용).</summary>
    public static void Apply(StageLayout l, StageManager sm, bool allowClone)
    {
        if (l == null || sm == null) return;

        // 벽: 스테이지 데이터의 칸 목록 하나로 그림+충돌을 같이 만듦 (보이는 벽 = 막히는 벽)
        if (allowClone && l.walls != null && l.walls.Count > 0)
        {
            int layer = 0;
            foreach (var col in Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.None))
                if (col.gameObject.name.StartsWith("Wall_") || (col.CompareTag("Wall") && !col.isTrigger))
                { layer = col.gameObject.layer; col.gameObject.SetActive(false); }
            WallBuilder.Build(l.walls, layer);

            // 바닥을 방 크기에 맞춤
            var floor = GameObject.Find("Floor");
            var fsr = floor != null ? floor.GetComponent<SpriteRenderer>() : null;
            if (fsr != null)
            {
                floor.transform.position = new Vector3(WallBuilder.MinX(l.width) + l.width * 0.5f, WallBuilder.MinY(l.height) + l.height * 0.5f, floor.transform.position.z);
                floor.transform.localScale = Vector3.one;
                fsr.size = new Vector2(l.width - 2f, l.height - 2f);
            }
        }

        if (sm.player != null) sm.player.transform.position = l.playerStart;

        l.Migrate();
        PlaceStations(l, allowClone);
        PlaceCrafters(l, allowClone);

        if (sm.terminals == null || sm.terminals.Length == 0 || l.terminals == null || l.terminals.Length == 0) return;
        var used = new List<BugTerminal>();
        for (int i = 0; i < Mathf.Max(l.terminals.Length, sm.terminals.Length); i++)
        {
            BugTerminal t = i < sm.terminals.Length ? sm.terminals[i] : null;
            if (i >= l.terminals.Length)
            {
                if (t != null && allowClone) t.gameObject.SetActive(false);
                continue;
            }
            if (t == null)
            {
                if (!allowClone) continue;
                var src = sm.terminals[0];
                t = Object.Instantiate(src, src.transform.parent);
                t.name = "BugTerminal_" + (i + 1);
            }
            t.gameObject.SetActive(true);
            t.transform.position = l.terminals[i];
            used.Add(t);
        }
        if (allowClone) sm.terminals = used.ToArray();
    }

    /// <summary>재료대: 종류별로 씬에 있는 것을 원본 삼아 필요한 만큼 복제, 안 쓰는 원본은 끔</summary>
    static void PlaceStations(StageLayout l, bool allowClone)
    {
        var originals = Object.FindObjectsByType<ResourceStation>(FindObjectsSortMode.None);
        foreach (var group in originals.GroupBy(s => s.material))
        {
            var spots = l.stations.Where(s => s.material == group.Key).ToList();
            var pool = group.ToList();
            var src = pool[0];
            for (int i = 0; i < Mathf.Max(spots.Count, pool.Count); i++)
            {
                if (i >= spots.Count) { if (allowClone) pool[i].gameObject.SetActive(false); continue; }
                var st = i < pool.Count ? pool[i] : (allowClone ? Object.Instantiate(src, src.transform.parent) : null);
                if (st == null) continue;
                st.gameObject.SetActive(true);
                st.transform.position = spots[i].pos;
            }
        }
    }

    /// <summary>제작대: 씬의 제작대를 원본 삼아 필요한 만큼 복제</summary>
    static void PlaceCrafters(StageLayout l, bool allowClone)
    {
        var pool = Object.FindObjectsByType<CraftingTable>(FindObjectsSortMode.None).ToList();
        if (pool.Count == 0) return;
        var src = pool[0];
        for (int i = 0; i < Mathf.Max(l.crafters.Count, pool.Count); i++)
        {
            if (i >= l.crafters.Count) { if (allowClone) pool[i].gameObject.SetActive(false); continue; }
            var t = i < pool.Count ? pool[i] : (allowClone ? Object.Instantiate(src, src.transform.parent) : null);
            if (t == null) continue;
            t.gameObject.SetActive(true);
            t.transform.position = l.crafters[i];
        }
    }
}
