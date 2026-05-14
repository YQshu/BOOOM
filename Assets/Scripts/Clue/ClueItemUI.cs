using UnityEngine;
using TMPro;

/// <summary>
/// 单条线索 UI 条目（条式布局）。
/// 显示线索ID、时间、线索描述和"新线索"红点。
/// </summary>
public class ClueItemUI : MonoBehaviour
{
    [Header("UI 组件")]
    public TMP_Text clueIdText;
    public TMP_Text timeText;
    public TMP_Text clueText;
    [Tooltip("新线索红点（isNew 时显示）")]
    public GameObject newBadge;

    /// <summary>
    /// 设置条目内容。
    /// </summary>
    public void Setup(string clueId, string time, string clue, bool isNewClue)
    {
        if (clueIdText != null) clueIdText.text = clueId;
        if (timeText != null) timeText.text = time;
        if (clueText != null) clueText.text = clue;
        if (newBadge != null) newBadge.SetActive(isNewClue);
    }

    /// <summary>
    /// 手动清除新线索红点。
    /// </summary>
    public void ClearNewBadge()
    {
        if (newBadge != null) newBadge.SetActive(false);
    }
}
