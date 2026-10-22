using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 스테이지 맵(바닥·벽·앞면·모서리)을 게임과 같은 규칙으로 PNG 한 장씩 저장 (게임 실행 없이 확인용).
/// 기계 자리는 색 테두리로 표시. 커맨드라인: -executeMethod MapPreviewRender.RenderAll -mapOut &lt;폴더&gt;
/// </summary>
public static class MapPreviewRender
{
    const int Cell = 32;
    static readonly Dictionary<Texture2D, Texture2D> readable = new Dictionary<Texture2D, Texture2D>();

    [MenuItem("DebugRush/Render Map Previews")]
    public static void RenderAll()
    {
        string outDir = GetArg("-mapOut") ?? Path.Combine(Directory.GetCurrentDirectory(), "MapPreviews");
        Directory.CreateDirectory(outDir);
        var fx = FxSet.Get();
        foreach (var s in AssetDatabase.FindAssets("t:StageData").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<StageData>))
        {
            if (s == null || s.layout == null || !s.layout.use || s.layout.walls == null || s.layout.walls.Count == 0) continue;
            var png = Render(s.layout, fx).EncodeToPNG();
            File.WriteAllBytes(Path.Combine(outDir, s.displayName + ".png"), png);
            Debug.Log("[MapPreview] " + s.displayName);
        }
        readable.Clear();
    }

    static Texture2D Render(StageLayout l, FxSet fx)
    {
        l.Migrate();
        int w = l.width, h = l.height, x0 = WallBuilder.MinX(w), y0 = WallBuilder.MinY(h);
        var img = new Texture2D(w * Cell, h * Cell, TextureFormat.RGBA32, false);
        img.SetPixels(Enumerable.Repeat(new Color(0.04f, 0.05f, 0.1f), img.width * img.height).ToArray());
        var floor = l.FloorSet();
        var walls = new HashSet<Vector2Int>(l.walls);
        var work = l.crafters.Select(p => new Rect(p - StageLayoutDefaults.CrafterSize / 2f - Vector2.one, StageLayoutDefaults.CrafterSize + Vector2.one * 2f)).ToList();
        foreach (var p in WallBuilder.FloorLayout(floor, fx, work))
            Blit(img, p.sprite, (p.center.x - p.size / 2f - x0) * Cell, (p.center.y - p.size / 2f - y0) * Cell, p.size * Cell, false);
        foreach (var t in WallBuilder.Tiles(walls, floor, fx))
            Blit(img, t.sprite, (t.cell.x - x0) * Cell, (t.cell.y - y0) * Cell, Cell, t.flipX, t.angle);
        void Box(Vector2 c, Vector2 size, Color col)
        {
            int ax = Mathf.RoundToInt((c.x - size.x / 2f - x0) * Cell), ay = Mathf.RoundToInt((c.y - size.y / 2f - y0) * Cell);
            int bx = Mathf.RoundToInt((c.x + size.x / 2f - x0) * Cell), by = Mathf.RoundToInt((c.y + size.y / 2f - y0) * Cell);
            for (int x = ax; x < bx; x++) for (int k = 0; k < 2; k++) { Set(img, x, ay + k, col); Set(img, x, by - 1 - k, col); }
            for (int y = ay; y < by; y++) for (int k = 0; k < 2; k++) { Set(img, ax + k, y, col); Set(img, bx - 1 - k, y, col); }
        }
        foreach (var st in l.stations) Box(st.pos, StageLayoutDefaults.StationSize, new Color(0.3f, 0.8f, 1f));
        foreach (var c in l.crafters) Box(c, StageLayoutDefaults.CrafterSize, new Color(1f, 0.6f, 0.2f));
        foreach (var t in l.terminals) Box(t, StageLayoutDefaults.TerminalSize, new Color(1f, 0.3f, 0.4f));
        Box(l.playerStart + Vector2.up * 0.5f, new Vector2(0.8f, 1f), Color.green);
        img.Apply();
        return img;
    }

    static void Set(Texture2D t, int x, int y, Color c) { if (x >= 0 && y >= 0 && x < t.width && y < t.height) t.SetPixel(x, y, c); }

    static void Blit(Texture2D dst, Sprite s, float px, float py, float size, bool flip, float angle = 0f)
    {
        if (s == null) return;
        var src = Readable(s.texture);
        var r = s.textureRect;
        int n = Mathf.RoundToInt(size), ox = Mathf.RoundToInt(px), oy = Mathf.RoundToInt(py);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                // 화면 칸 → 그림 좌표 (반시계 angle도 회전된 그림이므로 반대로 돌려서 읽음)
                float du = (x + 0.5f) / n - 0.5f, dv = (y + 0.5f) / n - 0.5f;
                float rad = -angle * Mathf.Deg2Rad, cs = Mathf.Cos(rad), sn = Mathf.Sin(rad);
                float u = du * cs - dv * sn + 0.5f, v = du * sn + dv * cs + 0.5f;
                if (flip) u = 1f - u;
                u = Mathf.Clamp(u, 0f, 0.999f); v = Mathf.Clamp(v, 0f, 0.999f);
                var c = src.GetPixel(Mathf.FloorToInt(r.x + u * r.width), Mathf.FloorToInt(r.y + v * r.height));
                if (c.a <= 0.01f) continue;
                int dx = ox + x, dy = oy + y;
                if (dx < 0 || dy < 0 || dx >= dst.width || dy >= dst.height) continue;
                dst.SetPixel(dx, dy, Color.Lerp(dst.GetPixel(dx, dy), new Color(c.r, c.g, c.b, 1f), c.a));
            }
    }

    // 임포트된 텍스처는 읽기 불가 → 렌더 텍스처로 복사해서 읽음
    static Texture2D Readable(Texture2D tex)
    {
        if (readable.TryGetValue(tex, out var r)) return r;
        var rt = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        Graphics.Blit(tex, rt);
        var prev = RenderTexture.active; RenderTexture.active = rt;
        r = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
        r.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0); r.Apply();
        RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rt);
        readable[tex] = r;
        return r;
    }

    static string GetArg(string name)
    {
        var args = System.Environment.GetCommandLineArgs();
        int i = System.Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
