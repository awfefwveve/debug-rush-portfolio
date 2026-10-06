using System.Collections.Generic;
using UnityEngine;

/// <summary>업적 저장/조회. 해금은 AchievementManager.Unlock("id") 한 줄로 호출.</summary>
public static class AchievementManager
{
    static string Key(string id) => "ach_" + id;

    public static bool IsUnlocked(string id) => PlayerPrefs.GetInt(Key(id), 0) == 1;

    public static void Unlock(string id)
    {
        if (IsUnlocked(id)) return;
        PlayerPrefs.SetInt(Key(id), 1);
        PlayerPrefs.Save();
        Debug.Log($"업적 획득: {id}");   // TODO: 토스트 UI
    }

    /// <summary>비밀 업적이고 미획득이면 설명 숨김</summary>
    public static string DescriptionFor(AchievementData a)
        => (a.secret && !IsUnlocked(a.id)) ? "???" : a.description;
}
