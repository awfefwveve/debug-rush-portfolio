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

    public static int Language
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(LangKey, 0), 0, Languages.Length - 1);
        set { PlayerPrefs.SetInt(LangKey, Mathf.Clamp(value, 0, Languages.Length - 1)); PlayerPrefs.Save(); Changed?.Invoke(); }
    }

    /// <summary>진행도(스테이지 클리어·업적) 전부 삭제. 설정(음량·언어)과 화면 설정은 유지. 시연용</summary>
    public static void ResetProgress()
    {
        for (int c = 1; c <= 3; c++)
            for (int s = 0; s <= 10; s++)   // 0 = 튜토리얼
                for (int d = 0; d < 3; d++) PlayerPrefs.DeleteKey($"clear_{c}_{s}_{d}");
        AchievementManager.ResetAll();
        PlayerPrefs.DeleteKey(ControlsGuide.SeenKey);   // 시연: 조작 안내 다시 보이게
        PlayerPrefs.Save();
    }
}
