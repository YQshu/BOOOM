using UnityEngine;

[CreateAssetMenu(fileName = "NewClue", menuName = "游戏/线索数据")]
public class ClueData : ScriptableObject
{
    [Header("基本信息")]
    public string clueId;           // 唯一ID
    public string clueName;         // 线索名称
    [TextArea(3, 5)]
    public string clueDescription;   // 线索描述

    [Header("图片")]
    public Sprite clueIcon;          // 线索图标（小）
    public Sprite clueImage;         // 线索大图

    [Header("关联")]
    public string[] relatedClues;    // 关联的线索ID

    [Header("解锁条件")]
    public bool isUnlockedByDefault; // 是否默认解锁
    public string unlockCondition;   // 解锁条件（剧情节点ID）
}
