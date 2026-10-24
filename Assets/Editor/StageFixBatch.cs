using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 10/23 기획 스테이지 정리 (B 요청): 제한시간 체크리스트 값, 1-4 몬스터2 2-A+2-B,
/// 벽(윗면+앞면 줄)과 겹치는 기계는 겹치지 않을 때까지 아래로 반 칸씩 내림.
/// 커맨드라인: -executeMethod StageFixBatch.Run
/// </summary>
public static class StageFixBatch
{
    static readonly Dictionary<int, float> TimeLimit = new Dictionary<int, float> { { 1, 120f }, { 2, 100f }, { 3, 115f }, { 4, 100f }, { 5, 110f } };

    public static void Run()
    {
        foreach (var s in AssetDatabase.FindAssets("t:StageData", new[] { "Assets/DebugRush/Data" })
                     .Select(g => AssetDatabase.LoadAssetAtPath<StageData>(AssetDatabase.GUIDToAssetPath(g))))
        {
            if (s == null || s.chapter != 1 || !TimeLimit.ContainsKey(s.stage)) continue;
            s.timeLimit = TimeLimit[s.stage];
            if (s.stage == 1 && s.monsters != null)   // 1-1은 벌 자기장(1-B) 끔, 1-2부터 사용 (10/23 B)
                foreach (var m in s.monsters)
                    if (m?.prefab != null && m.prefab.GetComponent<Monster1A>() != null) { m.patternMask = 1; m.patternCount = 1; }
            if (s.stage == 5 && s.layout != null && s.layout.walls != null)   // 1-5 가운데 십자 벽 삭제 → 바닥 (10/23 B)
            {
                int before = s.layout.walls.Count;
                s.layout.walls.RemoveAll(c => c.x >= -9 && c.x <= 9 && c.y >= -7 && c.y <= 5);
                if (s.layout.customFloor)
                    for (int x = -9; x <= 9; x++) for (int y = -7; y <= 5; y++) { var c = new Vector2Int(x, y); if (!s.layout.floor.Contains(c)) s.layout.floor.Add(c); }
                Debug.Log($"[Fix] 1-5 십자 벽 {before - s.layout.walls.Count}칸 삭제");
            }
            // 스토리 정리 (10/24): 1-4에 1-3 대사가 그대로 복사돼 있던 것 삭제(기획서: 1-2·1-4 대사 없음), 카드 한 줄 설명
            if (s.stage == 4) { s.intro = new DialogueLine[0]; s.outro = new DialogueLine[0]; }
            if (s.stage == 2 && string.IsNullOrEmpty(s.summary)) s.summary = "더 빨라진 벌들의 공격";
            if (s.stage == 4 && s.summary == "과부하된 네트워크") s.summary = "폭탄까지 설치하는 로봇";
            if (s.stage == 5 && s.summary == "보스") s.summary = "버그의 근원, 보스 등장";
            if (s.stage == 4 && s.monsters != null)
                foreach (var m in s.monsters)
                    if (m?.prefab != null && m.prefab.GetComponent<Monster1B>() != null)
                    {
                        m.patternMask = 3; m.patternCount = 2;   // 2-B 폭탄 켜기 (예전 기본값: 쿨 6초 · 감지 1.8 · 1초 뒤 · 폭발 1.6 · 최대 3)
                        if (m.tuning == null) m.tuning = new MonsterTuning();
                        m.tuning.cooldown = new[] { m.tuning.cooldown != null && m.tuning.cooldown.Length > 0 ? m.tuning.cooldown[0] : 3f, 6f };
                        m.tuning.mineDetect = 1.8f; m.tuning.mineDelay = 1f; m.tuning.mineRadius = 1.6f; m.tuning.maxMines = 3;
                    }
            int moved = 0;
            var l = s.layout;
            if (l != null && l.use)
            {
                var floor = l.FloorSet();
                var solid = WallBuilder.Solid(new HashSet<Vector2Int>(l.walls), floor, FxSet.Get());
                // 생성기만: 그림이 벽(윗면·앞면 줄)과 겹치면 한 칸 내림. 내린 자리가 벽·다른 기계·시작점과 겹치면 그대로 두고 로그
                var others = new List<Rect>();
                foreach (var c in l.crafters) others.Add(new Rect(c - StageLayoutDefaults.CrafterSize / 2f, StageLayoutDefaults.CrafterSize));
                foreach (var t in l.terminals) others.Add(new Rect(t - StageLayoutDefaults.TerminalSize / 2f, StageLayoutDefaults.TerminalSize));
                others.Add(new Rect(l.playerStart - new Vector2(0.5f, 0f), new Vector2(1f, 1.5f)));
                foreach (var st in l.stations)
                {
                    var sz = StageLayoutDefaults.StationSize;
                    var old = new Rect(st.pos - sz / 2f, sz);
                    if (!HitsSolid(Top(old), solid)) continue;                       // 위쪽(벽 줄)과 겹칠 때만
                    var np = st.pos + Vector2.down;
                    var nr = new Rect(np - sz / 2f, sz);
                    var before = SolidCells(old, solid);
                    bool clash = HitsSolid(Top(nr), solid) || SolidCells(nr, solid).Any(c => !before.Contains(c)) || others.Any(o => o.Overlaps(nr)) || l.stations.Any(o => o != st && new Rect(o.pos - sz / 2f, sz).Overlaps(nr));
                    if (clash) { Debug.Log($"[Fix] {s.displayName}: {st.material} 생성기({st.pos}) 내릴 자리 없음 (그대로) top={HitsSolid(Top(nr), solid)} new={string.Join(",", SolidCells(nr, solid).Where(c => !before.Contains(c)))} obj={others.Any(o => o.Overlaps(nr))} st={l.stations.Any(o => o != st && new Rect(o.pos - sz / 2f, sz).Overlaps(nr))}"); continue; }
                    st.pos = np; moved++;
                }
            }
            EditorUtility.SetDirty(s);
            Debug.Log($"[Fix] {s.displayName}: 시간 {s.timeLimit}, 옮긴 기계 {moved}");
        }
        AssetDatabase.SaveAssets();
        Export();
    }

    [System.Serializable] class StagePack { public List<string> stages = new List<string>(); }

    /// <summary>기획자 전달용: 1학년 전체를 Stage Maker 내보내기와 같은 형식으로 notes 폴더에 (-stageOut 경로)</summary>
    static void Export()
    {
        var args = System.Environment.GetCommandLineArgs();
        int i = System.Array.IndexOf(args, "-stageOut");
        if (i < 0 || i + 1 >= args.Length) return;
        var pack = new StagePack();
        foreach (var s in AssetDatabase.FindAssets("t:StageData", new[] { "Assets/DebugRush/Data" })
                     .Select(g => AssetDatabase.LoadAssetAtPath<StageData>(AssetDatabase.GUIDToAssetPath(g)))
                     .Where(s => s != null && s.chapter == 1 && s.stage >= 1).OrderBy(s => s.stage))
        { s.layout?.Migrate(); pack.stages.Add(EditorJsonUtility.ToJson(s)); }
        System.IO.File.WriteAllText(args[i + 1], JsonUtility.ToJson(pack, true));
        Debug.Log("[Fix] 내보냄 " + args[i + 1]);
    }

    // 위쪽 40% (맨 위 0.25칸은 그림 여백이라 겹쳐도 봐줌)
    static Rect Top(Rect r) => new Rect(r.x, r.y + r.height * 0.6f, r.width, r.height * 0.4f - 0.25f);

    static HashSet<Vector2Int> SolidCells(Rect r, HashSet<Vector2Int> solid)
    {
        var set = new HashSet<Vector2Int>();
        for (int x = Mathf.FloorToInt(r.xMin); x < Mathf.CeilToInt(r.xMax); x++)
            for (int y = Mathf.FloorToInt(r.yMin); y < Mathf.CeilToInt(r.yMax); y++)
                if (solid.Contains(new Vector2Int(x, y)) && new Rect(x + 0.05f, y + 0.05f, 0.9f, 0.9f).Overlaps(r)) set.Add(new Vector2Int(x, y));
        return set;
    }

    static bool HitsSolid(Rect r, HashSet<Vector2Int> solid)
    {
        for (int x = Mathf.FloorToInt(r.xMin); x < Mathf.CeilToInt(r.xMax); x++)
            for (int y = Mathf.FloorToInt(r.yMin); y < Mathf.CeilToInt(r.yMax); y++)
                if (solid.Contains(new Vector2Int(x, y)) && new Rect(x + 0.05f, y + 0.05f, 0.9f, 0.9f).Overlaps(r)) return true;
        return false;
    }

    // 기계 몸통(콜라이더 크기 기준: 가로 90% · 세로 아래쪽 75%)이 벽·앞면 칸과 겹치면 아래로 0.5칸씩 (최대 3칸)
    static Vector2 Clear(Vector2 pos, Vector2 size, HashSet<Vector2Int> solid)
    {
        for (int k = 0; k <= 6; k++)
        {
            var p = pos + Vector2.down * 0.5f * k;
            var body = new Rect(p.x - size.x * 0.45f, p.y - size.y * 0.5f, size.x * 0.9f, size.y * 0.75f);
            bool hit = false;
            for (int x = Mathf.FloorToInt(body.xMin); x < Mathf.CeilToInt(body.xMax) && !hit; x++)
                for (int y = Mathf.FloorToInt(body.yMin); y < Mathf.CeilToInt(body.yMax) && !hit; y++)
                    if (solid.Contains(new Vector2Int(x, y)) && new Rect(x, y, 1, 1).Overlaps(body)) hit = true;
            if (!hit) return p;
        }
        return pos;
    }
}
