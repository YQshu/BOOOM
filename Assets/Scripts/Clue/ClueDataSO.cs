using UnityEngine;

/// <summary>
/// 线索数据 ScriptableObject。
/// 在 Project 面板右键 → Create → BOOOM → Clue Data 创建。
/// </summary>
[CreateAssetMenu(fileName = "NewClue", menuName = "BOOOM/Clue Data")]
public class ClueDataSO : ScriptableObject
{
    [Tooltip("线索唯一ID（支持中文，如 凯-1-B）")]
    public string clueId;
    [Tooltip("所属嫌疑人ID，与 SuspectEntry.suspectId 一致（如 SUSPECT_KAI）")]
    public string suspectId;
    [Tooltip("时间轴时间戳（如 00:05）")]
    public string time;
    [Tooltip("线索描述（详细面板展示）")]
    [TextArea(2, 4)]
    public string summary;
}
