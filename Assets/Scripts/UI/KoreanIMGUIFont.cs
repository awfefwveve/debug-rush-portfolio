using UnityEngine;

/// <summary>OnGUI 기반 임시 메뉴에서 한글 글리프가 빠지지 않도록 Nanum Gothic을 적용합니다.</summary>
public static class KoreanIMGUIFont
{
    static Font cached;
    public static void Apply()
    {
        if (cached == null)
            foreach (var font in Resources.FindObjectsOfTypeAll<Font>())
                if (font != null && font.name.Contains("Nanum")) { cached = font; break; }
        if (cached == null) return;
        GUI.skin.box.font = cached; GUI.skin.label.font = cached; GUI.skin.button.font = cached;
        GUI.skin.window.font = cached; GUI.skin.toggle.font = cached;
    }
}
