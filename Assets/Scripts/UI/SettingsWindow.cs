using UnityEngine;

/// <summary>설정창 (기획서 UI 플로우): 브금 슬라이더, 효과음 슬라이더, 언어 드롭다운, 돌아가기. 시작화면·일시정지 공용.</summary>
public static class SettingsWindow
{
    public const float Width = 400f, Height = 340f;
    static bool languageOpen;

    /// <summary>화면 중앙에 그림. 돌아가기를 누르면 true.</summary>
    public static bool Draw()
    {
        KoreanIMGUIFont.Apply();
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

        GUILayout.Space(10);
        bool noise = GUILayout.Toggle(GameSettings.ScreenNoise, "  화면 노이즈 효과");
        if (noise != GameSettings.ScreenNoise) GameSettings.ScreenNoise = noise;

        GUILayout.FlexibleSpace();
        bool back = GUILayout.Button("돌아가기");
        GUILayout.EndArea();
        if (back) languageOpen = false;
        return back;
    }
}
