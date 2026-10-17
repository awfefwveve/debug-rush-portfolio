using UnityEngine;

/// <summary>공용 전투 그림 묶음 (팀 규격 "패턴·몬스터 이펙트"). Resources/FxSet.asset</summary>
[CreateAssetMenu(menuName = "DebugRush/Fx Set")]
public class FxSet : ScriptableObject
{
    [Header("탄환 (일반 16×16 빨강 / 패링 24×24 분홍 마름모)")]
    public Sprite bulletNormal;
    public Sprite bulletParry;

    [Header("이펙트")]
    public Sprite[] fireFx;      // 발사 64×64 4프레임
    public Sprite[] hitFx;       // 피격 64×64 4프레임
    public Sprite[] vanishFx;    // 소멸 128×128 6프레임

    [Header("공격 예고")]
    public Sprite warnCircleFill;    // 원형 예고 128×128 채움
    public Sprite warnCircleBorder;  // 원형 예고 128×128 테두리
    public Sprite warnLine;          // 직선 예고 32×128 아래 중앙 기준

    static FxSet cached;
    public static FxSet Get()
    {
        if (cached == null) cached = Resources.Load<FxSet>("FxSet");
        return cached;
    }
}
