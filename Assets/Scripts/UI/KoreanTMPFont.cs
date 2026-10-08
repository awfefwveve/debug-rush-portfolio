using TMPro;
using UnityEngine;

/// <summary>프로젝트에 포함된 나눔고딕 TMP 글꼴을 우선 사용합니다.</summary>
public static class KoreanTMPFont
{
    public static TMP_FontAsset Get()
    {
        foreach (var font in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
            if (font != null && font.name.Contains("Nanum")) return font;
        return TMP_Settings.defaultFontAsset;
    }
}
