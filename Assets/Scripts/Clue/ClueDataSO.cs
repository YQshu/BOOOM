using UnityEngine;

/// <summary>
/// 线索数据 ScriptableObject。
/// 在 Project 面板右键 → Create → BOOOM → Clue Data 创建。
/// </summary>
[CreateAssetMenu(fileName = "NewClue", menuName = "BOOOM/Clue Data")]
public class ClueDataSO : ScriptableObject
{
    [Tooltip("线索唯一ID（Ink tag 中使用此ID触发收集）")]
    public string clueId;
    [Tooltip("线索名称（展示台标题）")]
    public string clueName;
    [Tooltip("所属嫌疑人ID，与 SuspectEntry.suspectId 一致（如 SUSPECT_JESS）")]
    public string suspectId;
    [Tooltip("时间轴时间戳（如 00:05）")]
    public string time;
    [Tooltip("线索摘要（详细面板展示，预留）")]
    [TextArea(2, 4)]
    public string summary;
    [Tooltip("线索图标/贴图")]
    public Sprite icon;
}
