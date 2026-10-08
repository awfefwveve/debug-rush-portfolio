using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// 버그 단말기. 요구사항(재료/아이템 1~2개)을 처리할 때마다 완료 게이지 1칸 증가.
/// 목표(2~8)만큼 채우면 수리 완료.
/// </summary>
public class BugTerminal : MonoBehaviour, IInteractable
{
    public int goal = 2;
    public int Completed { get; private set; }
    public bool IsDone => Completed >= goal;

    /// <summary>현재 남은 요구사항 (완료된 것은 목록에서 제거되어 보이지 않음)</summary>
    public readonly List<ItemType> Remaining = new List<ItemType>();
    public event Action Changed;

    // ErrorLump(오류 뭉텅이)는 요구사항에서 제외
    static readonly ItemType[] Pool = (ItemType[])Enum.GetValues(typeof(ItemType));

    public void Init(int goal)
    {
        this.goal = Mathf.Clamp(goal, 2, 8);
        Completed = 0;
        NewRequest();
    }

    void NewRequest()
    {
        Remaining.Clear();
        if (!IsDone)
        {
            int n = Random.Range(1, 3);                       // 1~2개
            for (int i = 0; i < n; i++)
            {
                ItemType t;
                do { t = Pool[Random.Range(0, Pool.Length)]; } while (t == ItemType.ErrorLump);
                Remaining.Add(t);
            }
        }
        Changed?.Invoke();
    }

    public void Interact(PlayerController player)
    {
        if (IsDone || Remaining.Count == 0) return;
        var inv = player.Inventory;
        bool delivered = false;

        for (int i = Remaining.Count - 1; i >= 0; i--)
        {
            if (!inv.Remove(Remaining[i])) continue;
            Remaining.RemoveAt(i);
            delivered = true;
        }
        if (!delivered) return;

        if (Remaining.Count == 0)
        {
            Completed++;
            NewRequest();      // 내부에서 Changed 호출
        }
        else Changed?.Invoke();
    }
}
