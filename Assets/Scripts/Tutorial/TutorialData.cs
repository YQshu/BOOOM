using UnityEngine;

/// <summary>
/// 教程数据 ScriptableObject。
/// 存储单个教程页的图片和文字说明。
/// </summary>
[CreateAssetMenu(fileName = "TutorialData", menuName = "Game/Tutorial Data")]
public class TutorialData : ScriptableObject
{
    [Header("教程内容")]
    [Tooltip("教程图片")]
    public Sprite tutorialImage;

    [Tooltip("教程说明文字")]
    [TextArea(3, 10)]
    public string tutorialText;
}
