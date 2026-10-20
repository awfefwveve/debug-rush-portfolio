using TMPro;
using UnityEngine;

/// <summary>맵 위 글자(재료대·제작대 이름, 단말기 남은 시간)가 밝은 바닥에서도 읽히게 뒤에 반투명 검은 판을 깜.</summary>
public static class LabelBacking
{
    static Sprite white;

    public static void ApplyAll()
    {
        foreach (var t in Object.FindObjectsByType<TextMeshPro>(FindObjectsSortMode.None))
            if (t.name == "Label" || t.name == "TimerText") Add(t);
    }

    public static void Add(TextMeshPro t)
    {
        if (t == null || t.transform.Find("Backing") != null) return;
        if (white == null) white = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        t.ForceMeshUpdate();
        var size = t.textBounds.size;
        if (size.x <= 0.01f) size = new Vector2(1.2f, 0.5f);
        var g = new GameObject("Backing");
        g.transform.SetParent(t.transform, false);
        g.transform.localPosition = (Vector3)(Vector2)t.textBounds.center;
        g.transform.localScale = new Vector3((size.x + 0.3f) / t.transform.lossyScale.x, (size.y + 0.12f) / t.transform.lossyScale.y, 1f);
        var sr = g.AddComponent<SpriteRenderer>();
        sr.sprite = white; sr.color = new Color(0.03f, 0.05f, 0.1f, 0.8f);
        sr.sortingOrder = t.GetComponent<MeshRenderer>().sortingOrder - 1;
    }
}
