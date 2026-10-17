using System;
using UnityEngine;

/// <summary>설정값 저장 (기획서 UI 플로우: 브금 / 효과음 / 언어). PlayerPrefs에 저장.</summary>
public static class GameSettings
{
    const string BgmKey = "settings_bgm", SfxKey = "settings_sfx", LangKey = "settings_language";

    public static readonly string[] Languages = { "한국어" };

    public static event Action Changed;

    public static float BgmVolume
    {
        get => PlayerPrefs.GetFloat(BgmKey, 0.8f);
        set { PlayerPrefs.SetFloat(BgmKey, Mathf.Clamp01(value)); PlayerPrefs.Save(); Changed?.Invoke(); }
    }

    public static float SfxVolume
    {
        get => PlayerPrefs.GetFloat(SfxKey, 0.8f);
        set { PlayerPrefs.SetFloat(SfxKey, Mathf.Clamp01(value)); PlayerPrefs.Save(); Changed?.Invoke(); }
    }

    const string NoiseKey = "settings_screen_noise";
    /// <summary>깨진 화면 노이즈 연출 (기획서에 없는 추가 연출이라 끌 수 있게)</summary>
    public static bool ScreenNoise
    {
        get => PlayerPrefs.GetInt(NoiseKey, 1) == 1;
        set { PlayerPrefs.SetInt(NoiseKey, value ? 1 : 0); PlayerPrefs.Save(); Changed?.Invoke(); }
    }

    public static int Language
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(LangKey, 0), 0, Languages.Length - 1);
        set { PlayerPrefs.SetInt(LangKey, Mathf.Clamp(value, 0, Languages.Length - 1)); PlayerPrefs.Save(); Changed?.Invoke(); }
    }
}
