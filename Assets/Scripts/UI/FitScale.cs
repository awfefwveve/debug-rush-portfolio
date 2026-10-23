using UnityEngine;

/// <summary>1920×1080 기준 UI 묶음을 부모(캔버스) 안에 비율 그대로 꽉 맞게 통째로 확대·축소 (남는 쪽은 비움). 안의 배치는 기준 좌표 그대로</summary>
[RequireComponent(typeof(RectTransform))]
public class FitScale : MonoBehaviour
{
    public Vector2 reference = new Vector2(1920f, 1080f);
    RectTransform rt, parent;

    void Awake()
    {
        rt = (RectTransform)transform;
        parent = rt.parent as RectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero; rt.sizeDelta = reference;
        Apply();
    }

    void LateUpdate() => Apply();

    void Apply()
    {
        if (parent == null) return;
        var size = parent.rect.size;
        float s = Mathf.Min(size.x / reference.x, size.y / reference.y);
        if (s > 0f && !Mathf.Approximately(rt.localScale.x, s)) rt.localScale = new Vector3(s, s, 1f);
    }
}
