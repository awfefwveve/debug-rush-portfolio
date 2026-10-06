using UnityEngine;

[CreateAssetMenu(menuName = "DebugRush/Item Icon Set")]
public class ItemIconSet : ScriptableObject
{
    [Tooltip("ItemType enum 순서대로 12개")]
    public Sprite[] icons;
    public Sprite Get(ItemType t) => (int)t < icons.Length ? icons[(int)t] : null;
}
