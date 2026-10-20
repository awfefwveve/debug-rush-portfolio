using UnityEngine;

/// <summary>OnGUI 기반 임시 메뉴에서 한글 글리프가 빠지지 않도록 Nanum Gothic을 적용합니다.</summary>
public static class KoreanIMGUIFont
{
    static Font cached;
    static float nextSearch;
    public static void Apply()
    {
        // 메모리 전체를 뒤지는 검색은 2초에 한 번만 (OnGUI는 매 프레임 여러 번 불려서, 못 찾으면 매번 검색하던 게 렉 원인)
        if (cached == null && Time.realtimeSinceStartup >= nextSearch)
        {
            nextSearch = Time.realtimeSinceStartup + 2f;
            foreach (var font in Resources.FindObjectsOfTypeAll<Font>())
                if (font != null && font.name.Contains("Nanum")) { cached = font; break; }
        }
        if (cached == null) return;
        GUI.skin.box.font = cached; GUI.skin.label.font = cached; GUI.skin.button.font = cached;
        GUI.skin.window.font = cached; GUI.skin.toggle.font = cached;
    }
}
