using UnityEngine;

/// <summary>바닥에 떨어진 아이템. E 상호작용으로 다시 줍습니다.</summary>
public class DroppedItem : MonoBehaviour, IInteractable
{
    public ItemType item;
    SpriteRenderer icon;
    void Awake() { SetupVisual(); }
    public void Setup(ItemType value) { item = value; SetupVisual(); }
    void SetupVisual()
    {
        gameObject.layer = LayerMask.NameToLayer("Interactable");
        if (icon == null) icon = gameObject.GetComponent<SpriteRenderer>() ?? gameObject.AddComponent<SpriteRenderer>();
        foreach (var set in Resources.FindObjectsOfTypeAll<ItemIconSet>())
        {
            var sprite = set.Get(item);
            if (sprite != null) { icon.sprite = sprite; break; }
        }
        icon.sortingOrder = 20;
        var col = GetComponent<CircleCollider2D>() ?? gameObject.AddComponent<CircleCollider2D>();
        col.isTrigger = true; col.radius = 0.38f;
    }
    public void Interact(PlayerController player) { if (player.Inventory.TryAdd(item)) Destroy(gameObject); }
}
