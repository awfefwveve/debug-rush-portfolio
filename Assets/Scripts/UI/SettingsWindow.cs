using UnityEngine;

/// <summary>설정창 (기획서 UI 플로우): 브금 슬라이더, 효과음 슬라이더, 언어 드롭다운, 돌아가기. 시작화면·일시정지 공용.</summary>
public static class SettingsWindow
{
    public const float Width = 400f, Height = 340f;
    static bool languageOpen;

    static float dimFrom, dimLast = -1f;

    /// <summary>뒤 화면 전체를 반투명 검정으로 (0.15초 페이드). 설정창·일시정지·확인창 공용</summary>
    public static void Dim()
    {
        float now = Time.unscaledTime;
        if (now - dimLast > 0.1f) dimFrom = now;          // 막 열렸으면 페이드 시작
        dimLast = now;
        var prev = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.5f * Mathf.Clamp01((now - dimFrom) / 0.15f));
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = prev;
    }

    /// <summary>화면 중앙에 그림 (뒤는 어둡게). 돌아가기를 누르면 true.</summary>
    public static bool Draw()
    {
        KoreanIMGUIFont.Apply();
        Dim();
        var r = new Rect((Screen.width - Width) * 0.5f, (Screen.height - Height) * 0.5f, Width, Height);
        GUI.Box(r, "설정");
        GUILayout.BeginArea(new Rect(r.x + 28, r.y + 38, Width - 56, Height - 50));

        float bgm = GameSettings.BgmVolume;
        GUILayout.Label("브금  " + Mathf.RoundToInt(bgm * 100) + "%");
        float nb = GUILayout.HorizontalSlider(bgm, 0f, 1f);
        if (!Mathf.Approximately(nb, bgm)) GameSettings.BgmVolume = nb;

        GUILayout.Space(10);
        float sfx = GameSettings.SfxVolume;
        GUILayout.Label("효과음  " + Mathf.RoundToInt(sfx * 100) + "%");
        float ns = GUILayout.HorizontalSlider(sfx, 0f, 1f);
        if (!Mathf.Approximately(ns, sfx)) GameSettings.SfxVolume = ns;

        GUILayout.Space(10);
        GUILayout.Label("언어");
        if (GUILayout.Button(GameSettings.Languages[GameSettings.Language] + (languageOpen ? "  ▲" : "  ▼"))) languageOpen = !languageOpen;
        if (languageOpen)
            for (int i = 0; i < GameSettings.Languages.Length; i++)
                if (GUILayout.Button("   " + GameSettings.Languages[i])) { GameSettings.Language = i; languageOpen = false; }

        GUILayout.FlexibleSpace();
        bool back = GUILayout.Button("돌아가기");
        GUILayout.EndArea();
        if (back) languageOpen = false;
        return back;
    }
}
