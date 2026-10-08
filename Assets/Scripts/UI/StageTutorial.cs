using UnityEngine;

/// <summary>스테이지 진입 시 조작법을 한 번 보여주는 안내판.</summary>
public class StageTutorial : MonoBehaviour
{
    bool visible = true;
    void Update() { if (visible && StageManager.I != null && StageManager.I.Running && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.E))) visible = false; }
    void OnGUI()
    {
        KoreanIMGUIFont.Apply();
        if (!visible || (StageManager.I != null && !StageManager.I.Running)) return;
        float w = 470, h = 250;
        var r = new Rect((Screen.width-w)*.5f, (Screen.height-h)*.5f, w, h);
        GUI.Box(r, "플레이 안내");
        GUILayout.BeginArea(new Rect(r.x+28, r.y+42, w-56, h-54));
        GUILayout.Label("WASD  이동\nE  주변 오브젝트와 상호작용 / 아이템 줍기\nQ  마지막 소지 아이템 버리기\nSpace  점프 / 패링\nESC  계속하기 · 설정 · 타이틀로 나가기", GUI.skin.label);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("확인 (Enter / E)")) visible = false;
        GUILayout.EndArea();
    }
}
