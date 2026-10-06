public enum ItemType
{
    // 재료 (기획서 p.6)
    Planning, Programming, Graphic, QA,
    // 아이템 (조합 결과)
    ContentDoc,     // 기획 + 기획
    SystemDoc,      // 기획 + 프로그래밍
    CharacterCode,  // 프로그래밍 + 그래픽
    CodeFix,        // 프로그래밍 + QA
    GraphicFix,     // 그래픽 + QA
    GameBuild,      // 프로그래밍 + 프로그래밍
    ArtFinal,       // 그래픽 + 그래픽
    ErrorLump       // 그 외 모든 조합
}

public enum Difficulty { Easy, Normal, Hard }
