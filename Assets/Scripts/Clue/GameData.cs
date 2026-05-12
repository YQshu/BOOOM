using System;
using UnityEngine;

/// <summary>
/// 角色（嫌疑人）基础信息，挂在 SuspectEntry 里。
/// </summary>
[Serializable]
public class CharacterData
{
    [Tooltip("角色名称")]
    public string characterName;
    [Tooltip("头像（线索墙卡片用）")]
    public Sprite avatar;
    [Tooltip("全身像（详情面板左侧用）")]
    public Sprite fullBodySprite;
    [Tooltip("年龄")]
    public int age;
    [Tooltip("背景故事")]
    [TextArea(3, 6)]
    public string background;
    [Tooltip("简短描述（一句话）")]
    public string description;
}
