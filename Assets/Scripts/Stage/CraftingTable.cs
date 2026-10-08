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

    /// <summary>0~1은 제작 진행률, -1은 게이지 숨김을 뜻합니다.</summary>
    public event Action<float> CraftProgressChanged;

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

        // 들고 있는 재료 투입 (2개 들었으면 한 번에 모두)
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
        CraftProgressChanged?.Invoke(0f);

        float duration = Mathf.Max(0f, craftTime);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            yield return null;
            elapsed += Time.deltaTime;
            CraftProgressChanged?.Invoke(Mathf.Clamp01(elapsed / duration));
        }

        output = ItemRules.Combine(mats[0], mats[1]);
        mats.Clear();
        crafting = false;
        CraftProgressChanged?.Invoke(-1f);
        OutputChanged?.Invoke(output);
    }
}
