using UnityEngine;

/// <summary>제작 완료 아이콘만 표시합니다. 제작 중 임시 게이지는 표시하지 않습니다.</summary>
public class CraftingTableView : MonoBehaviour
{
    public CraftingTable table;
    public ItemIconSet icons;
    public SpriteRenderer outputIcon;

    void OnEnable()
    {
        if (table != null) table.OutputChanged += Show;
    }

    void OnDisable()
    {
        if (table != null) table.OutputChanged -= Show;
    }

    void Start()
    {
        if (outputIcon != null) outputIcon.enabled = false;
    }

    void Show(ItemType? item)
    {
        if (outputIcon == null) return;
        outputIcon.enabled = item.HasValue;
        if (item.HasValue && icons != null) outputIcon.sprite = icons.Get(item.Value);
    }
}
