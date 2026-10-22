using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 아트 팀 UI 그림 (Resources/UI, 원본: assets_drive/1022_UI). 없으면 null → 각 화면은 예전 색 상자로 그림.
/// 버튼 틀: button_gray(기본) / button_red(마우스 올림·누름). 마름모 아이콘: achievements·reset·credit.
/// </summary>
public static class UiArt
{
    static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    public static Sprite Get(string name)
    {
        if (cache.TryGetValue(name, out var s) && s != null) return s;
        s = Resources.Load<Sprite>("UI/" + name);
        cache[name] = s;
        return s;
    }

    public static Texture2D Tex(string name) { var s = Get(name); return s != null ? s.texture : null; }

    public static readonly Color Pink = new Color(1f, 0.31f, 0.55f);
    public static readonly Color Ink = new Color(0.93f, 0.94f, 0.98f);

    /// <summary>uGUI 버튼에 회색/빨강 틀 (마우스 올리면 빨강)</summary>
    public static void StyleButton(Button b, float fontSize = 34f)
    {
        var gray = Get("button_gray"); var red = Get("button_red");
        if (b == null || gray == null) return;
        var img = b.image != null ? b.image : b.GetComponent<Image>();
        if (img == null) return;
        img.sprite = gray; img.type = Image.Type.Sliced; img.color = Color.white; img.preserveAspect = false; img.pixelsPerUnitMultiplier = 3f;
        b.transition = Selectable.Transition.SpriteSwap;
        b.spriteState = new SpriteState { highlightedSprite = red, pressedSprite = red, selectedSprite = gray, disabledSprite = gray };
        var label = b.GetComponentInChildren<TMP_Text>(true);
        if (label != null) { label.color = Ink; label.fontSize = fontSize; label.fontStyle = FontStyles.Bold; }
    }

    // ---------- IMGUI (설정·일시정지·크레딧 창) ----------
    static GUIStyle labelStyle;

    /// <summary>그림 버튼 (회색, 마우스 올리면 빨강). 그림이 없으면 기본 버튼</summary>
    public static bool Button(Rect r, string text, float fontSize)
    {
        var gray = Tex("button_gray"); var red = Tex("button_red");
        if (gray == null) return GUI.Button(r, text, new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(fontSize) });
        bool hover = r.Contains(Event.current.mousePosition);
        GUI.DrawTexture(r, hover && red != null ? red : gray, ScaleMode.StretchToFill);
        Label(r, text, fontSize, Ink, TextAnchor.MiddleCenter, true);
        return GUI.Button(r, GUIContent.none, GUIStyle.none);
    }

    public static void Label(Rect r, string text, float fontSize, Color color, TextAnchor align = TextAnchor.MiddleLeft, bool bold = false)
    {
        if (labelStyle == null) labelStyle = new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true };
        labelStyle.font = GUI.skin.label.font;
        labelStyle.fontSize = Mathf.RoundToInt(fontSize);
        labelStyle.alignment = align;
        labelStyle.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        labelStyle.normal.textColor = color;
        GUI.Label(r, text, labelStyle);
    }

    public static void Draw(Rect r, string name, Color? tint = null)
    {
        var t = Tex(name);
        if (t == null) return;
        var prev = GUI.color;
        if (tint.HasValue) GUI.color = tint.Value;
        GUI.DrawTexture(r, t, ScaleMode.StretchToFill);
        GUI.color = prev;
    }

    /// <summary>창 틀(setting.png 1408×832) + 왼쪽 위 탭 이름. 화면 가운데, 세로 기준 크기. 그린 영역을 돌려줌</summary>
    public static Rect Window(string title, float w, float h)
    {
        float s = Screen.height / 1080f;
        var r = new Rect((Screen.width - w * s) * 0.5f, (Screen.height - h * s) * 0.5f, w * s, h * s);
        if (Tex("setting") != null)
        {
            GUI.DrawTexture(r, Tex("setting"), ScaleMode.StretchToFill);
            // 탭 글자 자리: 그림 기준 x 200~360, y 8~76 (1408×832, 탭 안 흰 막대 오른쪽)
            Label(new Rect(r.x + r.width * 0.142f, r.y + r.height * 0.01f, r.width * 0.2f, r.height * 0.082f), title, 34f * s, Ink, TextAnchor.MiddleLeft, true);
        }
        else GUI.Box(r, title);
        return r;
    }

    /// <summary>음량 바: 빈 바(bar_back) + 채움(bar_fill, 값만큼 잘라서) + 테두리(bar_frame). 드래그로 값 변경</summary>
    public static float Slider(Rect r, float value)
    {
        var back = Tex("bar_back"); var fill = Tex("bar_fill"); var frame = Tex("bar_frame");
        if (back == null || fill == null || frame == null) return GUI.HorizontalSlider(r, value, 0f, 1f);
        GUI.DrawTexture(r, back, ScaleMode.StretchToFill);
        // 채움 그림의 실제 칸: x 6~704, y 5~57 (800×64)
        float fx0 = 6f / 800f, fx1 = 704f / 800f, fy0 = 5f / 64f, fy1 = 57f / 64f;
        float v = Mathf.Clamp01(value);
        var fr = new Rect(r.x + r.width * fx0, r.y + r.height * fy0, r.width * (fx1 - fx0) * v, r.height * (fy1 - fy0));
        GUI.DrawTextureWithTexCoords(fr, fill, new Rect(fx0, 1f - fy1, (fx1 - fx0) * v, fy1 - fy0));
        GUI.DrawTexture(r, frame, ScaleMode.StretchToFill);
        var e = Event.current;
        var hit = new Rect(r.x, r.y, r.width * fx1, r.height);
        int id = GUIUtility.GetControlID(FocusType.Passive);
        float At(float x) => Mathf.Clamp01((x - fr.x) / (r.width * (fx1 - fx0)));
        if (e.type == EventType.MouseDown && hit.Contains(e.mousePosition)) { GUIUtility.hotControl = id; value = At(e.mousePosition.x); e.Use(); }
        else if (GUIUtility.hotControl == id)
        {
            if (e.type == EventType.MouseDrag) { value = At(e.mousePosition.x); e.Use(); }
            else if (e.type == EventType.MouseUp) { GUIUtility.hotControl = 0; e.Use(); }
        }
        return value;
    }
}
