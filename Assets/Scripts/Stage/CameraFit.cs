using UnityEngine;

/// <summary>
/// 화면 비율(16:9, 16:10, 21:9 …)과 관계없이 방 전체가 상단 HUD 바 아래로 화면을 꽉 채우도록 카메라 크기·위치 자동 조정.
/// 단위는 HUD 캔버스 기준(세로 1080).
/// </summary>
[DefaultExecutionOrder(-50)]
[RequireComponent(typeof(Camera))]
public class CameraFit : MonoBehaviour
{
    public static Vector3 Base { get; private set; } = new Vector3(0f, 0f, -10f);
    public static CameraFit I { get; private set; }

    public float roomWidth = WallBuilder.DefaultW, roomHeight = WallBuilder.DefaultH;   // 칸 (벽 포함). 스테이지 데이터가 있으면 그 크기
    public float bandTop = 56f, bandBottom = 0f;               // 상단 HUD 바 (캔버스 단위)
    [Tooltip("오른쪽에 비워 둘 폭 (캔버스 단위). 조합표는 맵 위에 겹치므로 0")] public float sidebar = 0f;

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
        var l = StageManager.I != null && StageManager.I.Data != null ? StageManager.I.Data.layout : null;
        if (l != null && l.use && l.walls != null && l.walls.Count > 0) { roomWidth = l.width; roomHeight = l.height; }
        float scale = Screen.height / 1080f;
        float availW = Mathf.Max(100f, Screen.width - sidebarNow * scale);
        float availH = Mathf.Max(100f, Screen.height - (bandTop + bandBottom) * scale);
        float ppu = Mathf.Min(availW / roomWidth, availH / roomHeight);
        cam.orthographicSize = Screen.height / (2f * ppu);
        float roomH = roomHeight * ppu;                                // 방을 상단 바 바로 아래에 붙임
        float cx = availW * 0.5f, cy = Screen.height - bandTop * scale - roomH * 0.5f;
        float dx = cx - Screen.width * 0.5f, dy = cy - Screen.height * 0.5f;
        // 방 중심 (홀수 칸이면 반 칸 어긋남)
        int w = Mathf.RoundToInt(roomWidth), h = Mathf.RoundToInt(roomHeight);
        float rx = WallBuilder.MinX(w) + w * 0.5f, ry = WallBuilder.MinY(h) + h * 0.5f;
        Base = new Vector3(rx - dx / ppu, ry - dy / ppu, -10f);
    }

    void OnDestroy() { if (I == this) I = null; }
}
