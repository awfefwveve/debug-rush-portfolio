using UnityEngine;

/// <summary>
/// 화면 비율(16:9, 16:10, 21:9 …)과 관계없이 방 전체가 HUD 띠·조합표 패널에 가리지 않고 보이도록 카메라 크기·위치 자동 조정.
/// 단위는 HUD 캔버스 기준(세로 1080).
/// </summary>
[DefaultExecutionOrder(-50)]
[RequireComponent(typeof(Camera))]
public class CameraFit : MonoBehaviour
{
    public static Vector3 Base { get; private set; } = new Vector3(0f, 0f, -10f);
    public static CameraFit I { get; private set; }

    public float roomWidth = 28.4f, roomHeight = 12.4f;       // 칸 (벽 포함 + 여유)
    public float bandTop = 140f, bandBottom = 140f;            // HUD 띠 (캔버스 단위)
    [Tooltip("오른쪽 조합표 패널 폭 (캔버스 단위)")] public float sidebar = 340f;

    float sidebarNow;
    Camera cam;

    void Awake() { I = this; cam = GetComponent<Camera>(); sidebarNow = sidebar; Fit(); }

    void LateUpdate()
    {
        sidebarNow = Mathf.MoveTowards(sidebarNow, sidebar, Time.unscaledDeltaTime * 1600f);
        Fit();
        if (StageFX.I == null || !StageFX.I.Shaking) transform.position = Base;
    }

    void Fit()
    {
        float scale = Screen.height / 1080f;
        float availW = Mathf.Max(100f, Screen.width - sidebarNow * scale);
        float availH = Mathf.Max(100f, Screen.height - (bandTop + bandBottom) * scale);
        float ppu = Mathf.Min(availW / roomWidth, availH / roomHeight);
        cam.orthographicSize = Screen.height / (2f * ppu);
        float cx = availW * 0.5f, cy = bandBottom * scale + availH * 0.5f;
        float dx = cx - Screen.width * 0.5f, dy = cy - Screen.height * 0.5f;
        Base = new Vector3(-dx / ppu, -dy / ppu, -10f);
    }

    void OnDestroy() { if (I == this) I = null; }
}
