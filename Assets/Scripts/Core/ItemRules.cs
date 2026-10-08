public static class ItemRules
{
    public static bool IsMaterial(this ItemType t) => t <= ItemType.QA;

    static bool Pair(ItemType a, ItemType b, ItemType x, ItemType y)
        => (a == x && b == y) || (a == y && b == x);

    /// <summary>재료 2개 조합 결과. 순서 무관.</summary>
    public static ItemType Combine(ItemType a, ItemType b)
    {
        if (!a.IsMaterial() || !b.IsMaterial()) return ItemType.ErrorLump;
        const ItemType P = ItemType.Planning, G = ItemType.Programming,
                       A = ItemType.Graphic, Q = ItemType.QA;

        if (Pair(a, b, P, P)) return ItemType.ContentDoc;
        if (Pair(a, b, P, G)) return ItemType.SystemDoc;
        if (Pair(a, b, G, A)) return ItemType.CharacterCode;
        if (Pair(a, b, G, Q)) return ItemType.CodeFix;
        if (Pair(a, b, A, Q)) return ItemType.GraphicFix;
        if (Pair(a, b, G, G)) return ItemType.GameBuild;
        if (Pair(a, b, A, A)) return ItemType.ArtFinal;
        return ItemType.ErrorLump;
    }
}
