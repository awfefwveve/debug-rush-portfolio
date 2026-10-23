using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 기획 Stage Maker에서 내보낸 .stage 파일을 창 없이 가져오기 (같은 번호는 덮어쓰기, 맵·수치·대사 그대로).
/// 커맨드라인: -executeMethod StageImportBatch.Run -stageFile &lt;경로&gt;
/// 보스 그림이 들어온 뒤라, 몬스터가 비어 있는 학년 마지막 스테이지(기획 이름 '보스')에는 보스를 하나 놓음.
/// </summary>
public static class StageImportBatch
{
    [System.Serializable] class StagePack { public System.Collections.Generic.List<string> stages = new System.Collections.Generic.List<string>(); }

    public static void Run()
    {
        var args = System.Environment.GetCommandLineArgs();
        int i = System.Array.IndexOf(args, "-stageFile");
        if (i < 0 || i + 1 >= args.Length) { Debug.LogError("[Import] -stageFile 없음"); return; }
        var pack = JsonUtility.FromJson<StagePack>(System.IO.File.ReadAllText(args[i + 1]));
        var all = AssetDatabase.FindAssets("t:StageData", new[] { "Assets/DebugRush/Data" })
            .Select(g => AssetDatabase.LoadAssetAtPath<StageData>(AssetDatabase.GUIDToAssetPath(g))).Where(s => s != null).ToList();
        foreach (var json in pack.stages)
        {
            var tmp = ScriptableObject.CreateInstance<StageData>();
            EditorJsonUtility.FromJsonOverwrite(json, tmp);
            var target = all.FirstOrDefault(x => x.chapter == tmp.chapter && x.stage == tmp.stage);
            if (target != null)
            {
                string keep = target.name;
                EditorJsonUtility.FromJsonOverwrite(json, target);
                target.name = keep;
            }
            else
            {
                target = tmp;
                AssetDatabase.CreateAsset(target, $"Assets/DebugRush/Data/Stage_{tmp.chapter}_{tmp.stage}.asset");
                all.Add(target);
            }
            if (target.stage == StageManager.StagesPerChapter && (target.monsters == null || target.monsters.Length == 0))
                target.monsters = new[] { BossSpawn(target) };
            EditorUtility.SetDirty(target);
            StageMakerWindow.Register(target, true);
            Debug.Log($"[Import] {target.displayName} ← {tmp.name}");
        }
        AssetDatabase.SaveAssets();
    }

    static MonsterSpawn BossSpawn(StageData s)
    {
        // 맵 가운데에서 가까운, 3×3칸이 전부 바닥이고 벽·앞면·기계와 안 겹치는 자리 (Stage Maker에서 옮길 수 있음)
        var l = s.layout;
        var b = l.PaintedBounds();
        var floor = l.FloorSet();
        var solid = WallBuilder.Solid(new System.Collections.Generic.HashSet<Vector2Int>(l.walls), floor, FxSet.Get());
        var objs = new System.Collections.Generic.List<Rect>();
        foreach (var st in l.stations) objs.Add(new Rect(st.pos - StageLayoutDefaults.StationSize / 2f, StageLayoutDefaults.StationSize));
        foreach (var c in l.crafters) objs.Add(new Rect(c - StageLayoutDefaults.CrafterSize / 2f, StageLayoutDefaults.CrafterSize));
        foreach (var t in l.terminals) objs.Add(new Rect(t - StageLayoutDefaults.TerminalSize / 2f, StageLayoutDefaults.TerminalSize));
        objs.Add(new Rect(l.playerStart - new Vector2(2f, 1f), new Vector2(4f, 4f)));   // 시작점 바로 옆은 피함
        Vector2 center = new Vector2(b.x + b.width * 0.5f, b.y + b.height * 0.5f), best = center;
        float bestD = float.MaxValue;
        for (int x = b.xMin + 1; x < b.xMax - 1; x++)
            for (int y = b.yMin + 1; y < b.yMax - 1; y++)
            {
                bool ok = true;
                for (int dx = -1; dx <= 1 && ok; dx++) for (int dy = -1; dy <= 1 && ok; dy++)
                { var c = new Vector2Int(x + dx, y + dy); if (!floor.Contains(c) || solid.Contains(c)) ok = false; }
                var area = new Rect(x - 1, y - 1, 3, 3);
                if (!ok || objs.Any(o => o.Overlaps(area))) continue;
                var p = new Vector2(x + 0.5f, y + 0.5f);
                float d = (p - center).sqrMagnitude;
                if (d < bestD) { bestD = d; best = p; }
            }
        return new MonsterSpawn
        {
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DebugRush/Prefabs/Monster1C.prefab"),
            patternCount = 3, patternMask = 7,
            spawnPos = best,
            tuning = new MonsterTuning { speedGap = 4f, cooldown = new[] { 4f, 10f, 8f }, parryChance = 0.5f },
        };
    }
}
