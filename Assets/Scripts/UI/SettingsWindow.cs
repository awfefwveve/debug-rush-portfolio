using UnityEngine;

/// <summary>설정창 (기획서 UI 플로우 + 아트 팀 설정창 러프): 브금 / 효과음 바, 언어 선택, 닫기. 시작화면·일시정지 공용.</summary>
public static class SettingsWindow
{
    public const float Width = 1100f, Height = 650f;   // setting.png 1408×832 비율
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

    /// <summary>화면 중앙에 그림 (뒤는 어둡게). 닫기를 누르면 true.</summary>
    public static bool Draw()
    {
        KoreanIMGUIFont.Apply();
        Dim();
        float s = Screen.height / 1080f;
        var r = UiArt.Window("설정", Width, Height);
        float x0 = r.x + r.width * 0.09f, nameW = r.width * 0.14f, nameH = nameW * 80f / 224f;

        float bgm = GameSettings.BgmVolume;
        float nb = VolumeRow(r, x0, r.y + r.height * 0.28f, nameW, nameH, "BGM", bgm, s);
        if (!Mathf.Approximately(nb, bgm)) GameSettings.BgmVolume = nb;

        float sfx = GameSettings.SfxVolume;
        float ns = VolumeRow(r, x0, r.y + r.height * 0.43f, nameW, nameH, "효과음", sfx, s);
        if (!Mathf.Approximately(ns, sfx)) GameSettings.SfxVolume = ns;

        // 언어: 이름표 + 분홍 선택 바 (▽), 펼치면 아래로 목록
        float ly = r.y + r.height * 0.66f;
        NameTag(new Rect(x0, ly, nameW, nameH), "언어", s);
        var bar = new Rect(x0 + nameW + 10f * s, ly, r.width * 0.62f, nameH);
        UiArt.Draw(bar, "language");
        UiArt.Label(bar, GameSettings.Languages[GameSettings.Language], 28f * s, UiArt.Ink, TextAnchor.MiddleCenter, true);
        UiArt.Label(new Rect(bar.xMax - 60f * s, bar.y, 50f * s, bar.height), languageOpen ? "△" : "▽", 26f * s, UiArt.Ink, TextAnchor.MiddleCenter, true);
        if (GUI.Button(bar, GUIContent.none, GUIStyle.none)) languageOpen = !languageOpen;
        if (languageOpen)
            for (int i = 0; i < GameSettings.Languages.Length; i++)
            {
                var item = new Rect(bar.x, bar.yMax + i * bar.height, bar.width, bar.height);
                UiArt.Draw(item, item.Contains(Event.current.mousePosition) ? "language" : "language_2");
                UiArt.Label(item, GameSettings.Languages[i], 26f * s, UiArt.Ink, TextAnchor.MiddleCenter);
                if (GUI.Button(item, GUIContent.none, GUIStyle.none)) { GameSettings.Language = i; languageOpen = false; }
            }

        // 닫기 (오른쪽 위)
        var close = new Rect(r.x + r.width * 0.7f, r.y + r.height * 0.8f, r.width * 0.17f, r.width * 0.17f * 0.3f);
        bool back = UiArt.Button(close, "닫기", 24f * s);
        // 개발자 모드 (스테이지 테스트용): 왼쪽 아래 버튼으로 켜고 끔
        var dev = new Rect(r.x + r.width * 0.09f, close.y, r.width * 0.3f, close.height);
        if (UiArt.Button(dev, GameSettings.DevMode ? "개발자 모드: 켜짐" : "개발자 모드: 꺼짐", 22f * s)) GameSettings.DevMode = !GameSettings.DevMode;
        if (GameSettings.DevMode) UiArt.Label(new Rect(dev.xMax + 12f * s, dev.y, r.width * 0.28f, dev.height), "모든 스테이지 열림", 20f * s, new Color(0.75f, 0.1f, 0.3f), TextAnchor.MiddleLeft, true);
        if (back) languageOpen = false;
        return back;
    }

    static float VolumeRow(Rect win, float x, float y, float nameW, float nameH, string label, float value, float s)
    {
        NameTag(new Rect(x, y, nameW, nameH), label, s);
        float iconH = nameH * 0.8f;
        var icon = new Rect(x + nameW + 12f * s, y + (nameH - iconH) * 0.5f, iconH, iconH);
        UiArt.Draw(icon, "speaker");
        var bar = new Rect(icon.xMax + 8f * s, y + nameH * 0.12f, win.width * 0.52f, nameH * 0.76f);
        float v = UiArt.Slider(bar, value);
        UiArt.Label(new Rect(bar.xMax + 4f * s, y, 90f * s, nameH), Mathf.RoundToInt(v * 100) + "%", 22f * s, new Color(0.12f, 0.13f, 0.18f), TextAnchor.MiddleLeft, true);
        return v;
    }

    static void NameTag(Rect r, string text, float s)
    {
        if (UiArt.Tex("name") != null) UiArt.Draw(r, "name"); else GUI.Box(r, "");
        // name.png: 왼쪽 칸 x 2~195 / 224
        UiArt.Label(new Rect(r.x, r.y, r.width * 0.87f, r.height), text, 24f * s, UiArt.Ink, TextAnchor.MiddleCenter, true);
    }
}
