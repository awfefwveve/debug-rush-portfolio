/// <summary>아이템 표시 이름 (기획서 p.10 표 명칭)</summary>
public static class ItemNames
{
    public static string Get(ItemType item)
    {
        switch (item)
        {
            case ItemType.Planning: return "기획";
            case ItemType.Programming: return "프로그래밍";
            case ItemType.Graphic: return "그래픽";
            case ItemType.QA: return "QA";
            case ItemType.ContentDoc: return "컨텐츠 기획서";
            case ItemType.SystemDoc: return "시스템 기획서";
            case ItemType.CharacterCode: return "캐릭터 코드";
            case ItemType.CodeFix: return "코드 수정본";
            case ItemType.GraphicFix: return "그래픽 수정본";
            case ItemType.GameBuild: return "게임 빌딩";
            case ItemType.ArtFinal: return "아트 작업물 완성본";
            default: return "오류뭉텅이";
        }
    }
}
