using UnityEngine;

/// <summary>제작 완료 시에만 완성 아이템 아이콘 표시 (제작 시간은 UI에 노출하지 않음)</summary>
public class CraftingTableView : MonoBehaviour
{
    public CraftingTable table;
    public ItemIconSet icons;
    public SpriteRenderer outputIcon;

    void OnEnable() => table.OutputChanged += Show;
    void OnDisable() => table.OutputChanged -= Show;
    void Start() => outputIcon.enabled = false;

    void Show(ItemType? t)
    {
        outputIcon.enabled = t.HasValue;
        if (t.HasValue) outputIcon.sprite = icons.Get(t.Value);
    }
}
