using UnityEngine;

/// <summary>제작 중 테이블 위에 진행 게이지, 제작 완료 시 완성 아이콘을 표시합니다.</summary>
public class CraftingTableView : MonoBehaviour
{
    public CraftingTable table;
    public ItemIconSet icons;
    public SpriteRenderer outputIcon;

    [Header("제작 진행 게이지")]
    public Vector2 progressBarSize = new Vector2(1.15f, 0.14f);
    public Vector3 progressBarOffset = new Vector3(0f, 0.78f, 0f);
    public Color progressBarBackground = new Color(0.08f, 0.11f, 0.16f, 0.95f);
    public Color progressBarFill = new Color(0.25f, 0.9f, 0.65f, 1f);

    Sprite progressSprite;
    SpriteRenderer progressBackground;
    SpriteRenderer progressFill;
    void Awake()
    {
        CreateProgressBar();
    }

    void OnEnable()
    {
        if (table == null) return;
        table.OutputChanged += Show;
        table.CraftProgressChanged += SetProgress;
    }

    void OnDisable()
    {
        if (table == null) return;
        table.OutputChanged -= Show;
        table.CraftProgressChanged -= SetProgress;
    }

    void Start()
    {
        if (outputIcon != null) outputIcon.enabled = false;
        SetProgress(-1f);
    }

    void OnDestroy()
    {
        if (progressSprite != null) Destroy(progressSprite);
    }

    void CreateProgressBar()
    {
        progressSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f), 1f);
        progressSprite.name = "Crafting Progress Bar Sprite";

        var barRoot = new GameObject("Crafting Progress Bar");
        barRoot.layer = gameObject.layer;
        barRoot.transform.SetParent(transform, false);
        barRoot.transform.localPosition = progressBarOffset;

        progressBackground = barRoot.AddComponent<SpriteRenderer>();
        progressBackground.sprite = progressSprite;
        progressBackground.color = progressBarBackground;
        progressBackground.sortingLayerID = GetComponent<SpriteRenderer>().sortingLayerID;
        progressBackground.sortingOrder = GetComponent<SpriteRenderer>().sortingOrder + 10;
        progressBackground.transform.localScale = progressBarSize;

        var fillObject = new GameObject("Fill");
        fillObject.layer = gameObject.layer;
        fillObject.transform.SetParent(barRoot.transform, false);
        progressFill = fillObject.AddComponent<SpriteRenderer>();
        progressFill.sprite = progressSprite;
        progressFill.color = progressBarFill;
        progressFill.sortingLayerID = progressBackground.sortingLayerID;
        progressFill.sortingOrder = progressBackground.sortingOrder + 1;
        progressFill.transform.localScale = new Vector3(0.94f, 0.48f, 1f);

        SetProgress(-1f);
    }

    void SetProgress(float value)
    {
        float progress = Mathf.Clamp01(value);
        bool visible = value >= 0f;
        if (progressBackground != null) progressBackground.enabled = visible;
        if (progressFill != null) progressFill.enabled = visible;
        if (!visible || progressFill == null) return;

        float width = 0.94f * progress;
        progressFill.transform.localScale = new Vector3(width, 0.48f, 1f);
        progressFill.transform.localPosition = new Vector3(-0.47f + width * 0.5f, 0f, -0.01f);
    }

    void Show(ItemType? item)
    {
        if (outputIcon == null) return;
        outputIcon.enabled = item.HasValue;
        if (item.HasValue) outputIcon.sprite = icons.Get(item.Value);
    }
}
