using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>제작대. 재료 2개 투입 → 제작 진행 → 완성 아이템 수령</summary>
public class CraftingTable : MonoBehaviour, IInteractable
{
    public float craftTime = 3f;

    readonly List<ItemType> mats = new List<ItemType>();
    ItemType? output;
    bool crafting;

    /// <summary>완성품 아이콘 표시용 (null이면 숨김).</summary>
    public event Action<ItemType?> OutputChanged;

    /// <summary>투입된 재료 아이콘 표시용 (0~2개).</summary>
    public event Action<IReadOnlyList<ItemType>> MaterialsChanged;
    public IReadOnlyList<ItemType> Materials => mats;

    public void Interact(PlayerController player)
    {
        if (crafting) return;
        var inv = player.Inventory;

        // 완성품이 있으면 수령
        if (output.HasValue)
        {
            if (inv.TryAdd(output.Value))
            {
                output = null;
                OutputChanged?.Invoke(null);
            }
            return;
        }

        // 재료 1개만 넣은 상태에서 빈손으로 E → 회수
        if (mats.Count == 1 && inv.items.Count == 0)
        {
            if (inv.TryAdd(mats[0]))
            {
                mats.Clear();
                MaterialsChanged?.Invoke(mats);
            }
            return;
        }

        // 들고 있는 재료 투입 (2개 들었으면 한 번에 모두)
        int before = mats.Count;
        for (int i = inv.items.Count - 1; i >= 0 && mats.Count < 2; i--)
        {
            if (!inv.items[i].IsMaterial()) continue;
            mats.Add(inv.items[i]);
            inv.RemoveAt(i);
        }
        if (mats.Count != before) MaterialsChanged?.Invoke(mats);

        if (mats.Count == 2) StartCoroutine(CraftRoutine());
    }

    IEnumerator CraftRoutine()
    {
        crafting = true;

        float duration = Mathf.Max(0f, craftTime);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            yield return null;
            elapsed += Time.deltaTime;
        }

        output = ItemRules.Combine(mats[0], mats[1]);
        mats.Clear();
        crafting = false;
        MaterialsChanged?.Invoke(mats);
        OutputChanged?.Invoke(output);
    }
}
