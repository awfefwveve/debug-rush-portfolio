using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 기획서 p.3 "게임을 고치면 스테이지 클리어와 동시에 망가진 게임이 고쳐지는 걸 보는 성취감":
/// 스테이지 동안 화면에 깨진 게임 노이즈(글리치)가 덮여 있고, 오류를 고칠수록 옅어지며,
/// 클리어 순간 노이즈가 걷히고 "게임 복구 완료!" 연출.
/// HUD보다 아래, 게임 화면보다 위에 그려짐.
/// </summary>
public class GameRestoreFX : MonoBehaviour
{
    public float maxIntensity = 0.5f;

    RawImage glitch;
    Image flash;
    TextMeshProUGUI banner;
    float intensity, target;
    float nextJitter;
    bool restoring;

    void Start()
    {
        var canvasGo = new GameObject("FX_Glitch", typeof(Canvas), typeof(CanvasScaler));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -10;                          // HUD(0) 아래
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080);

        glitch = new GameObject("Glitch", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        glitch.transform.SetParent(canvasGo.transform, false);
        Stretch(glitch.rectTransform);
        glitch.texture = MakeNoise();
        glitch.raycastTarget = false;

        flash = new GameObject("RestoreFlash", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        flash.transform.SetParent(canvasGo.transform, false);
        Stretch(flash.rectTransform);
        flash.color = new Color(1, 1, 1, 0); flash.raycastTarget = false;

        banner = new GameObject("RestoreBanner", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        banner.transform.SetParent(canvasGo.transform, false);
        banner.rectTransform.sizeDelta = new Vector2(1400, 200);
        banner.rectTransform.anchoredPosition = new Vector2(0, 230);
        banner.font = KoreanTMPFont.Get(); banner.fontSize = 84; banner.fontStyle = FontStyles.Bold;
        banner.alignment = TextAlignmentOptions.Center; banner.color = new Color(0.4f, 1f, 0.6f, 0f);
        banner.outlineWidth = 0.2f; banner.outlineColor = new Color32(8, 12, 24, 255);
        banner.text = "게임 복구 완료!";
        banner.raycastTarget = false;

        intensity = target = maxIntensity;
        var sm = StageManager.I;
        if (sm != null) sm.StageEnded += cleared => { if (cleared) StartCoroutine(Restore()); };
    }

    void Update()
    {
        if (!restoring)
        {
            var sm = StageManager.I;
            int done = 0, total = 0;
            if (sm != null && sm.terminals != null)
                foreach (var t in sm.terminals)
                    if (t != null && t.gameObject.activeInHierarchy) { done += Mathf.Min(t.Completed, t.goal); total += t.goal; }
            float progress = total > 0 ? done / (float)total : 0f;
            target = maxIntensity * (1f - progress * 0.8f);   // 고칠수록 옅어짐
            intensity = Mathf.MoveTowards(intensity, target, Time.deltaTime * 0.3f);
        }

        // 노이즈가 흔들리며 깨진 화면처럼 보이게
        if (Time.unscaledTime > nextJitter)
        {
            nextJitter = Time.unscaledTime + Random.Range(0.06f, 0.16f);
            glitch.uvRect = new Rect(Random.value, Random.value, 1f, 1f);
        }
        float flicker = Random.value < 0.04f ? 1.4f : 1f;
        glitch.color = new Color(1, 1, 1, Mathf.Clamp01(intensity * flicker));
    }

    IEnumerator Restore()
    {
        restoring = true;
        float d = 1.1f, e = 0f, start = intensity;
        while (e < d)
        {
            e += Time.unscaledDeltaTime;
            float k = e / d;
            intensity = Mathf.Lerp(start, 0f, k);
            flash.color = new Color(1, 1, 1, Mathf.Sin(k * Mathf.PI) * 0.55f);
            banner.color = new Color(0.4f, 1f, 0.6f, Mathf.Clamp01(k * 2f));
            banner.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.3f, 1f, k);
            yield return null;
        }
        intensity = 0f; flash.color = new Color(1, 1, 1, 0);
    }

    static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }

    static Texture2D MakeNoise()
    {
        const int w = 192, h = 108;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
        var clear = new Color(0, 0, 0, 0);
        var px = new Color[w * h];
        for (int i = 0; i < px.Length; i++) px[i] = clear;
        // 가로 주사선
        for (int y = 0; y < h; y += 3) for (int x = 0; x < w; x++) px[y * w + x] = new Color(0f, 0f, 0f, 0.12f);
        // 깨진 블록 (분홍·하늘·흰색)
        var cols = new[] { new Color(1f, 0.25f, 0.65f, 0.45f), new Color(0.2f, 0.85f, 1f, 0.45f), new Color(1f, 1f, 1f, 0.3f) };
        for (int b = 0; b < 22; b++)
        {
            int bw = Random.Range(3, 18), bh = 1, bx = Random.Range(0, w), by = Random.Range(0, h);
            if (bx > w * 0.18f && bx < w * 0.8f && Random.value < 0.75f) continue;   // 가운데(플레이 영역)는 거의 비움
            var c = cols[Random.Range(0, cols.Length)];
            for (int y = by; y < Mathf.Min(h, by + bh); y++) for (int x = bx; x < Mathf.Min(w, bx + bw); x++) px[y * w + x] = c;
        }
        tex.SetPixels(px); tex.Apply();
        return tex;
    }
}
