using UnityEngine;

/// <summary>기획서 p.7: 플레이어가 들고 있는 자료(재료·아이템)를 머리 위에 표시.</summary>
public class CarryDisplay : MonoBehaviour
{
    public float height = 2.05f;
    public float scale = 0.6f;
    public float spacing = 0.62f;

    PlayerController player;
    ItemIconSet icons;
    SpriteRenderer[] slots;

    void Awake() => player = GetComponent<PlayerController>();

    void Start()
    {
        var hud = FindAnyObjectByType<StageHUD>();
        icons = hud != null ? hud.icons : null;
        slots = new SpriteRenderer[PlayerInventory.Max];
        for (int i = 0; i < slots.Length; i++)
        {
            var go = new GameObject("Carry" + i);
            go.transform.SetParent(player.visual != null ? player.visual : transform, false);
            go.transform.localScale = Vector3.one * scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 50;
            sr.enabled = false;
            slots[i] = sr;
        }
        player.Inventory.Changed += Refresh;
        Refresh();
    }

    void OnDestroy() { if (player != null && player.Inventory != null) player.Inventory.Changed -= Refresh; }

    void Refresh()
    {
        if (slots == null) return;
        var items = player.Inventory.items;
        int n = items.Count;
        for (int i = 0; i < slots.Length; i++)
        {
            bool on = i < n && icons != null;
            slots[i].enabled = on;
            if (!on) continue;
            slots[i].sprite = icons.Get(items[i]);
            float x = n == 1 ? 0f : (i - 0.5f) * spacing;
            slots[i].transform.localPosition = new Vector3(x, height + (n == 2 && i == 1 ? 0.08f : 0f), 0f);
        }
    }

    void LateUpdate()
    {
        if (slots == null) return;
        float bob = Mathf.Sin(Time.time * 9f) * 0.04f * (player.MoveInput.sqrMagnitude > 0.01f ? 1f : 0.3f);
        foreach (var s in slots)
            if (s.enabled) s.transform.localPosition = new Vector3(s.transform.localPosition.x, height + bob, 0f);
    }
}
