using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>재료/아이템 합쳐서 최대 2개까지 소지</summary>
public class PlayerInventory : MonoBehaviour
{
    public const int Max = 2;
    public readonly List<ItemType> items = new List<ItemType>();
    public event Action Changed;

    public bool IsFull => items.Count >= Max;

    public bool TryAdd(ItemType t)
    {
        if (IsFull) return false;
        items.Add(t);
        Changed?.Invoke();
        return true;
    }

    public bool Remove(ItemType t)
    {
        int i = items.IndexOf(t);
        if (i < 0) return false;
        items.RemoveAt(i);
        Changed?.Invoke();
        return true;
    }

    public void RemoveAt(int index)
    {
        items.RemoveAt(index);
        Changed?.Invoke();
    }
}
