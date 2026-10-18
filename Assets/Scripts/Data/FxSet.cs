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

    [Header("벽 연결 조각 (32×32, 기본 방향: 끝=아래, 직선=좌우, 코너=오른쪽+아래, T=좌·우·아래)")]
    public Sprite wallSingle, wallEnd, wallStraight, wallCorner, wallT, wallCross;

    [Header("입체(3/4 시점) 타일 — 있으면 위 6조각 대신 사용. 모서리 순서: 좌상·우상·좌하·우하")]
    public Sprite[] tileFloor;          // tile_floor_01~03 (칸마다 고정 무작위)
    public Sprite tileWorkFloor;        // 제작대 주변 바닥
    public Sprite wallTopTile;          // tile_wall_top
    public Sprite[] wallFront;          // tile_wall_front_01~02 (아래가 바닥인 벽)
    public Sprite[] cornerOut = new Sprite[4], cornerIn = new Sprite[4];

    public bool Has3Q => wallTopTile != null;

    static FxSet cached;
    public static FxSet Get()
    {
        if (cached == null) cached = Resources.Load<FxSet>("FxSet");
        return cached;
    }
}
