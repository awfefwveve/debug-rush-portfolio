using System.Collections;
using UnityEngine;

/// <summary>학년 마지막 스테이지 클리어 후 한 장: "n학년 클리어! / 다음 학년은 준비 중". 아무 키나 클릭으로 닫힘.</summary>
public class ChapterEndCard : MonoBehaviour
{
    int chapter;
    bool done;
    float openedAt;

    public static IEnumerator Show(int chapter)
    {
        var c = new GameObject("ChapterEndCard").AddComponent<ChapterEndCard>();
        c.chapter = chapter; c.openedAt = Time.unscaledTime;
        while (!c.done) yield return null;
        Destroy(c.gameObject);
    }

    void Update()
    {
        if (Time.unscaledTime - openedAt > 0.6f && (Input.anyKeyDown || Input.GetMouseButtonDown(0))) done = true;
    }

    void OnGUI()
    {
        KoreanIMGUIFont.Apply();
        float s = Screen.height / 1080f;
        float a = Mathf.Clamp01((Time.unscaledTime - openedAt) / 0.4f);
        var prev = GUI.color;
        GUI.color = new Color(0.02f, 0.03f, 0.07f, 0.92f * a);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = prev;
        var art = UiArt.Tex("icon");
        if (art != null) { GUI.color = new Color(1f, 1f, 1f, a); GUI.DrawTexture(new Rect((Screen.width - 220f * s) * 0.5f, Screen.height * 0.16f, 220f * s, 220f * s), art, ScaleMode.ScaleToFit); GUI.color = prev; }
        var gold = new Color(1f, 0.82f, 0.25f, a);
        var ink = new Color(0.9f, 0.93f, 1f, a);
        UiArt.Label(new Rect(0, Screen.height * 0.40f, Screen.width, 110f * s), $"{chapter}학년 클리어!", 84f * s, gold, TextAnchor.MiddleCenter, true);
        UiArt.Label(new Rect(0, Screen.height * 0.52f, Screen.width, 60f * s), $"{chapter + 1}학년은 준비 중이에요", 40f * s, ink, TextAnchor.MiddleCenter, true);
        UiArt.Label(new Rect(0, Screen.height * 0.59f, Screen.width, 50f * s), "To be continued...", 30f * s, new Color(1f, 0.31f, 0.55f, a), TextAnchor.MiddleCenter, true);
        if (Time.unscaledTime - openedAt > 0.6f)
            UiArt.Label(new Rect(0, Screen.height * 0.82f, Screen.width, 40f * s), "아무 키나 누르세요", 22f * s, new Color(0.6f, 0.65f, 0.78f, a), TextAnchor.MiddleCenter);
    }
}
