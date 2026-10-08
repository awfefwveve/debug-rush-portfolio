using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>제작대. 재료 2개 투입 → (숨겨진) 제작 시간 → 완성 아이템 아이콘 표시 → 수령</summary>
public class CraftingTable : MonoBehaviour, IInteractable
{
    public float craftTime = 3f;

    readonly List<ItemType> mats = new List<ItemType>();
    ItemType? output;
    bool crafting;

    /// <summary>완성 아이템 아이콘 표시용 (null이면 숨김). 제작 중에는 진행도를 표시하지 않음.</summary>
    public event Action<ItemType?> OutputChanged;

    public void Interact(PlayerController player)
    {
        if (crafting) return;
        var inv = player.Inventory;

        // 1) 완성품이 있으면 수령
        if (output.HasValue)
        {
            if (inv.TryAdd(output.Value))
            {
                output = null;
                OutputChanged?.Invoke(null);
            }
            return;
        }

        // 2) 들고 있는 재료 투입 (2개 들었으면 한 번에 모두)
        for (int i = inv.items.Count - 1; i >= 0 && mats.Count < 2; i--)
        {
            if (!inv.items[i].IsMaterial()) continue;
            mats.Add(inv.items[i]);
            inv.RemoveAt(i);
        }

        if (mats.Count == 2) StartCoroutine(CraftRoutine());
    }

    IEnumerator CraftRoutine()
    {
        crafting = true;
        yield return new WaitForSeconds(craftTime);
        output = ItemRules.Combine(mats[0], mats[1]);
        mats.Clear();
        crafting = false;
        OutputChanged?.Invoke(output);
    }
}
