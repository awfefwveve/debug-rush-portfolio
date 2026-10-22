using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// 버그 단말기. 요구사항(재료/아이템 1~2개)을 처리할 때마다 완료 게이지 1칸 증가.
/// 목표(2~8)만큼 채우면 수리 완료. 요구사항마다 남은 시간이 있고, 시간이 다 되면 요구사항이 바뀜.
/// </summary>
public class BugTerminal : MonoBehaviour, IInteractable
{
    public int goal = 2;
    public int Completed { get; private set; }
    public bool IsDone => Completed >= goal;

    [Header("요구사항 남은 시간")]
    [Tooltip("요구사항 하나당 제한 시간(초). 0이면 무제한")]
    public float requestTime = 30f;
    [Tooltip("시간 초과 시 완료 게이지 1칸 감소 (기획서에 규칙 없음 → 기본 꺼짐)")]
    public bool expirePenalty = false;

    /// <summary>현재 요구사항의 남은 시간(초)</summary>
    public float TimeLeft { get; private set; }
    /// <summary>남은 시간 비율 1 → 0</summary>
    public float TimeRatio => requestTime > 0f ? Mathf.Clamp01(TimeLeft / requestTime) : 1f;
    public bool HasTimer => requestTime > 0f && !IsDone && Remaining.Count > 0;

    /// <summary>현재 남은 요구사항 (완료된 것은 목록에서 제거되어 보이지 않음)</summary>
    public readonly List<ItemType> Remaining = new List<ItemType>();
    public event Action Changed;
    public event Action Expired;

    // ErrorLump(오류 뭉텅이)는 요구사항에서 제외
    static readonly ItemType[] Pool = (ItemType[])Enum.GetValues(typeof(ItemType));

    /// <summary>첫 요구사항을 재료 하나로 (1-1 첫 단말기: 시작하자마자 성공하는 경험)</summary>
    [NonSerialized] public bool easyFirst;

    public void Init(int goal)
    {
        this.goal = Mathf.Clamp(goal, 2, 8);
        Completed = 0;
        NewRequest();
    }

    void NewRequest()
    {
        Remaining.Clear();
        TimeLeft = requestTime;
        if (!IsDone)
        {
            if (easyFirst)
            {
                easyFirst = false;
                Remaining.Add((ItemType)Random.Range(0, (int)ItemType.QA + 1));   // 재료 하나 그대로
                Changed?.Invoke();
                return;
            }
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

    void Update()
    {
        if (!HasTimer) return;
        var stage = StageManager.I;
        if (stage != null && !stage.Running) return;      // 인트로/결과 화면에서는 멈춤

        TimeLeft -= Time.deltaTime;
        if (TimeLeft > 0f) return;

        if (expirePenalty && Completed > 0) Completed--;
        Expired?.Invoke();
        NewRequest();
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
