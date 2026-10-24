using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게임 시작 인트로 (실행할 때 한 번): 팀 로고 '팥없찐'이 팡 튀어나옴 → 학교(청강문화산업대학교) → 타이틀.
/// 아무 키·클릭이면 다음으로 넘김. 학교 로고 그림은 Resources/UI/intro_school_logo.png 가 있으면 그걸, 없으면 글자.
/// </summary>
public class IntroSplash : MonoBehaviour
{
    public static bool Shown { get; private set; }

    CanvasGroup root, teamGroup, schoolGroup;
    RectTransform teamLogo;
    bool skip;

    public static void Play()
    {
        if (Shown) return;
        Shown = true;
        new GameObject("IntroSplash").AddComponent<IntroSplash>();
    }

    void Awake()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 32000;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();
        root = gameObject.AddComponent<CanvasGroup>();

        var bg = Full(transform, "Bg", new Color(0.11f, 0.11f, 0.13f, 1f));   // 레퍼런스처럼 어두운 회색
        bg.raycastTarget = true;                                                 // 뒤 메뉴 클릭 막음

        teamGroup = Group("Team");
        var logo = UiArt.Get("intro_team_logo");
        var img = new GameObject("TeamLogo", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        img.transform.SetParent(teamGroup.transform, false);
        img.sprite = logo; img.preserveAspect = true; img.raycastTarget = false;
        teamLogo = img.rectTransform; teamLogo.sizeDelta = new Vector2(1180f, 640f);

        schoolGroup = Group("School");
        // 학교 화면: 검은 갈색 바탕 가운데 주황 빛 번짐 (청강 로고 영상처럼)
        Full(schoolGroup.transform, "SchoolBg", new Color(0.06f, 0.035f, 0.02f, 1f));
        var glow = new GameObject("Glow", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        glow.transform.SetParent(schoolGroup.transform, false);
        glow.sprite = Radial(); glow.color = new Color(0.85f, 0.45f, 0.12f, 0.45f); glow.raycastTarget = false;
        glow.rectTransform.sizeDelta = new Vector2(1300f, 1000f);
        var schoolArt = UiArt.Get("intro_school_logo");
        if (schoolArt != null)
        {
            var s = new GameObject("SchoolLogo", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            s.transform.SetParent(schoolGroup.transform, false);
            s.sprite = schoolArt; s.preserveAspect = true; s.raycastTarget = false; s.rectTransform.sizeDelta = new Vector2(460f, 320f); s.rectTransform.anchoredPosition = new Vector2(0f, 30f);
            StageHUD.Label(schoolGroup.transform, "청강문화산업대학교", new Vector2(0.5f, 0.5f), new Vector2(0f, -190f), new Vector2(1000f, 60f), 30f, TextAlignmentOptions.Center, Color.white, true).characterSpacing = 6f;
        }
        else
        {
            var t = StageHUD.Label(schoolGroup.transform, "청강문화산업대학교", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200f, 120f), 72f, TextAlignmentOptions.Center, Color.white, true);
            t.characterSpacing = 8f;
        }
        teamGroup.alpha = 0f; schoolGroup.alpha = 0f;
    }

    static Sprite radial;
    static Sprite Radial()
    {
        if (radial != null) return radial;
        const int N = 128; var tex = new Texture2D(N, N, TextureFormat.RGBA32, false); var px = new Color32[N * N];
        for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
        { float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(N / 2f, N / 2f)) / (N / 2f); px[y * N + x] = new Color32(255, 255, 255, (byte)(255 * Mathf.Clamp01(1f - d) * Mathf.Clamp01(1f - d))); }
        tex.SetPixels32(px); tex.Apply();
        return radial = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f);
    }

    CanvasGroup Group(string name)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
        go.transform.SetParent(transform, false);
        var r = (RectTransform)go.transform; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
        return go.GetComponent<CanvasGroup>();
    }

    static Image Full(Transform parent, string name, Color c)
    {
        var img = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        img.transform.SetParent(parent, false);
        var r = img.rectTransform; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
        img.color = c;
        return img;
    }

    void Update()
    {
        if (Input.anyKeyDown || Input.GetMouseButtonDown(0)) skip = true;
    }

    IEnumerator Start()
    {
        // 타이틀 노래는 인트로가 끝나고 타이틀부터
        var bgm = SoundEffectManager.Instance != null ? SoundEffectManager.Instance.bgmSource : null;
        if (bgm != null) bgm.Pause();
        yield return Wait(0.3f);

        // 팀 로고: 작게 → 크게 튀어나왔다 → 제자리, 살짝 흔들림이 잦아듦
        teamGroup.alpha = 1f;
        float t = 0f;
        while (t < 2.6f && !skip)
        {
            t += Time.unscaledDeltaTime;
            float pop = t < 0.18f ? Mathf.Lerp(0.2f, 1.18f, t / 0.18f) : 1f + 0.18f * Mathf.Exp(-(t - 0.18f) * 7f) * Mathf.Cos((t - 0.18f) * 22f);
            teamLogo.localScale = Vector3.one * pop;
            teamLogo.localRotation = Quaternion.Euler(0f, 0f, 4f * Mathf.Exp(-t * 3f) * Mathf.Sin(t * 18f));
            yield return null;
        }
        teamLogo.localScale = Vector3.one; teamLogo.localRotation = Quaternion.identity;
        skip = false;
        yield return Fade(teamGroup, 1f, 0f, 0.35f);

        // 학교
        yield return Fade(schoolGroup, 0f, 1f, 0.35f);
        yield return Wait(1.3f);
        skip = false;
        yield return Fade(schoolGroup, 1f, 0f, 0.35f);

        // 타이틀로
        if (bgm != null) bgm.UnPause();
        yield return Fade(root, 1f, 0f, 0.4f);
        Destroy(gameObject);
    }

    IEnumerator Wait(float s)
    {
        float t = 0f;
        while (t < s && !skip) { t += Time.unscaledDeltaTime; yield return null; }
    }

    IEnumerator Fade(CanvasGroup g, float from, float to, float s)
    {
        float t = 0f;
        while (t < s) { t += Time.unscaledDeltaTime; g.alpha = Mathf.Lerp(from, to, t / s); yield return null; }
        g.alpha = to;
    }
}
