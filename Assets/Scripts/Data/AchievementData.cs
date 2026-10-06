using UnityEngine;

[CreateAssetMenu(menuName = "DebugRush/Achievement")]
public class AchievementData : ScriptableObject
{
    public string id;
    public string title;
    [TextArea] public string description;
    public Sprite icon;
    [Tooltip("true면 미획득 시 상세 설명을 숨김")] public bool secret;
}
