using UnityEngine;

/// <summary>단말기 월드 UI: 요구사항 아이콘 + 완료 게이지 칸(최대 8)</summary>
public class TerminalView : MonoBehaviour
{
    public BugTerminal terminal;
    public ItemIconSet icons;
    public SpriteRenderer[] requestIcons;      // 2개
    public SpriteRenderer[] gaugeCells;        // 8개
    public Color filled = new Color(1f, 0.8f, 0.25f);
    public Color empty = new Color(0.35f, 0.35f, 0.35f);

    void OnEnable() => terminal.Changed += Refresh;
    void OnDisable() => terminal.Changed -= Refresh;
    void Start() => Refresh();

    void Refresh()
    {
        for (int i = 0; i < requestIcons.Length; i++)
        {
            bool on = i < terminal.Remaining.Count;
            requestIcons[i].enabled = on;
            if (on) requestIcons[i].sprite = icons.Get(terminal.Remaining[i]);
        }
        for (int i = 0; i < gaugeCells.Length; i++)
        {
            gaugeCells[i].enabled = i < terminal.goal;
            gaugeCells[i].color = i < terminal.Completed ? filled : empty;
        }
    }
}
