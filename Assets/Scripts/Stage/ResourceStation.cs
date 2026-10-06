using UnityEngine;

/// <summary>재료 획득 장소 (기획 / 프로그래밍 / 그래픽 / QA)</summary>
public class ResourceStation : MonoBehaviour, IInteractable
{
    public ItemType material = ItemType.Planning;

    void OnValidate()
    {
        if (!material.IsMaterial()) material = ItemType.Planning;
    }

    public void Interact(PlayerController player)
    {
        if (!player.Inventory.TryAdd(material))
            Debug.Log("손이 가득 찼습니다 (최대 2개)");   // TODO: 경고 SFX/UI
    }
}
