using System.Collections.Generic;
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
        }

        if (sm.player != null) sm.player.transform.position = l.playerStart;

        foreach (var s in Object.FindObjectsByType<ResourceStation>(FindObjectsSortMode.None))
            s.transform.position = l.MaterialPos(s.material);

        var table = Object.FindAnyObjectByType<CraftingTable>();
        if (table != null) table.transform.position = l.craftingTable;

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

    /// <summary>현재 씬 배치를 layout에 기록</summary>
    public static void Capture(StageLayout l, StageManager sm)
    {
        if (l == null || sm == null) return;
        if (sm.player != null) l.playerStart = sm.player.transform.position;

        foreach (var s in Object.FindObjectsByType<ResourceStation>(FindObjectsSortMode.None))
            l.SetMaterialPos(s.material, s.transform.position);

        var table = Object.FindAnyObjectByType<CraftingTable>();
        if (table != null) l.craftingTable = table.transform.position;

        var pos = new List<Vector2>();
        if (sm.terminals != null)
            foreach (var t in sm.terminals)
                if (t != null && t.gameObject.activeSelf) pos.Add(t.transform.position);
        if (pos.Count > 0) l.terminals = pos.ToArray();
        l.use = true;
    }
}
