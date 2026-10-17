using System.Collections.Generic;
using UnityEngine;

/// <summary>업적 저장/조회. 해금은 AchievementManager.Unlock("id") 한 줄로 호출.</summary>
public static class AchievementManager
{
    static string Key(string id) => "ach_" + id;
    const string ListKey = "ach__unlocked";            // 해금한 업적 id 목록 (초기화용)
    static readonly string[] KnownIds = { "first_clear", "no_damage", "chapter1_clear", "chapter2_clear", "chapter3_clear" };

    /// <summary>업적 전부 미획득으로</summary>
    public static void ResetAll()
    {
        foreach (var id in PlayerPrefs.GetString(ListKey, "").Split(',')) if (id.Length > 0) PlayerPrefs.DeleteKey(Key(id));
        foreach (var id in KnownIds) PlayerPrefs.DeleteKey(Key(id));
        PlayerPrefs.DeleteKey(ListKey);
    }

    public static bool IsUnlocked(string id) => PlayerPrefs.GetInt(Key(id), 0) == 1;

    public static void Unlock(string id)
    {
        if (IsUnlocked(id)) return;
        PlayerPrefs.SetInt(Key(id), 1);
        var list = PlayerPrefs.GetString(ListKey, "");
        if (!("," + list + ",").Contains("," + id + ",")) PlayerPrefs.SetString(ListKey, list.Length > 0 ? list + "," + id : id);
        PlayerPrefs.Save();
        Debug.Log($"업적 획득: {id}");   // TODO: 토스트 UI
    }

    /// <summary>비밀 업적이고 미획득이면 설명 숨김</summary>
    public static string DescriptionFor(AchievementData a)
        => (a.secret && !IsUnlocked(a.id)) ? "???" : a.description;
}
