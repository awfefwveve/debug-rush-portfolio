using System.Collections.Generic;
using UnityEngine;

/// <summary>투입된 재료 아이콘과 제작 완료 아이콘을 표시합니다. 제작 중 임시 게이지는 표시하지 않습니다.</summary>
public class CraftingTableView : MonoBehaviour
{
    public CraftingTable table;
    public ItemIconSet icons;
    public SpriteRenderer outputIcon;

    [Header("본체 3상태 (대기 / 제작 중 / 완료)")]
    public Sprite idleSprite;
    public Sprite workingSprite;
    public Sprite doneSprite;
    SpriteRenderer body;

    [Header("투입 재료 (비우면 outputIcon 옆에 자동 생성)")]
    public SpriteRenderer[] materialIcons;
    public float materialSpacing = 0.35f;
    public float materialScale = 0.7f;

    void Awake()
    {
        if ((materialIcons == null || materialIcons.Length == 0) && outputIcon != null)
            materialIcons = CreateMaterialIcons();
    }

    void OnEnable()
    {
        if (table == null) return;
        table.OutputChanged += Show;
        table.MaterialsChanged += ShowMaterials;
    }

    void OnDisable()
    {
        if (table == null) return;
        table.OutputChanged -= Show;
        table.MaterialsChanged -= ShowMaterials;
    }

    void Update()
    {
        if (body == null) body = table != null ? table.GetComponent<SpriteRenderer>() : null;
        if (body == null) return;
        Sprite s = table.HasOutput ? doneSprite : table.IsCrafting ? workingSprite : idleSprite;
        if (s != null) body.sprite = s;
        // 제작 중에는 살짝 들썩임
        float k = table.IsCrafting ? 1f + Mathf.Sin(Time.time * 14f) * 0.025f : 1f;
        body.transform.localScale = new Vector3(k, 2f - k, 1f);
    }

    void Start()
    {
        if (outputIcon != null) outputIcon.enabled = false;
        ShowMaterials(table != null ? table.Materials : null);
    }

    void Show(ItemType? item)
    {
        if (outputIcon == null) return;
        outputIcon.enabled = item.HasValue;
        if (item.HasValue && icons != null) outputIcon.sprite = icons.Get(item.Value);
    }

    void ShowMaterials(IReadOnlyList<ItemType> mats)
    {
        if (materialIcons == null) return;
        for (int i = 0; i < materialIcons.Length; i++)
        {
            var sr = materialIcons[i];
            if (sr == null) continue;
            bool has = mats != null && i < mats.Count;
            sr.enabled = has;
            if (has && icons != null) sr.sprite = icons.Get(mats[i]);
        }
    }

    SpriteRenderer[] CreateMaterialIcons()
    {
        var result = new SpriteRenderer[2];
        var src = outputIcon.transform;
        for (int i = 0; i < result.Length; i++)
        {
            var go = new GameObject("MaterialIcon" + i);
            go.transform.SetParent(src.parent, false);
            go.transform.localPosition = src.localPosition + Vector3.right * ((i - 0.5f) * 2f * materialSpacing);
            go.transform.localRotation = src.localRotation;
            go.transform.localScale = src.localScale * materialScale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingLayerID = outputIcon.sortingLayerID;
            sr.sortingOrder = outputIcon.sortingOrder;
            sr.enabled = false;
            result[i] = sr;
        }
        return result;
    }
}
