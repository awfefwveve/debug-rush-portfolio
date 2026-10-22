using System;
using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public float invincibleTime = 1.5f;

    public int MaxHp { get; private set; }
    public int Hp { get; private set; }
    public bool IsInvincible { get; private set; }

    public event Action<int, int> HpChanged;   // (현재, 최대)
    public event Action Died;

    void Awake()
    {
        int chapter = (GameManager.I != null && GameManager.I.CurrentStage != null)
            ? GameManager.I.CurrentStage.chapter : 1;
        MaxHp = GameManager.ChapterHp[Mathf.Clamp(chapter - 1, 0, GameManager.ChapterHp.Length - 1)];
        Hp = MaxHp;
    }

    public void TakeDamage(int dmg = 1)
    {
        if (IsInvincible || Hp <= 0) return;
        Hp = Mathf.Max(0, Hp - dmg);
        HpChanged?.Invoke(Hp, MaxHp);
        if (Hp <= 0) Died?.Invoke();
        else StartCoroutine(InvincibleRoutine());
    }

    IEnumerator InvincibleRoutine()
    {
        IsInvincible = true;
        // 캐릭터 그림(visual)만 반투명 깜빡임 (그림자·머리 위 아이템은 그대로)
        var pc = GetComponent<PlayerController>();
        var sr = pc != null && pc.visual != null ? pc.visual.GetComponent<SpriteRenderer>() : GetComponentInChildren<SpriteRenderer>();
        float t = 0f;
        while (t < invincibleTime)
        {
            t += Time.deltaTime;
            if (sr) { var c = sr.color; c.a = Mathf.FloorToInt(t * 12f) % 2 == 0 ? 1f : 0.25f; sr.color = c; }
            yield return null;
        }
        if (sr) { var c = sr.color; c.a = 1f; sr.color = c; }
        IsInvincible = false;
    }
}
