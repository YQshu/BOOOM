using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 单条线索 UI 条目。
/// 显示时间、线索摘要、图标和"新线索"红点。
/// </summary>
public class ClueItemUI : MonoBehaviour
{
    [Header("UI 组件")]
    public TMP_Text timeText;
    public TMP_Text clueText;
    public Image clueIcon;
    [Tooltip("新线索红点（isNew 时显示）")]
    public GameObject newBadge;

    /// <summary>
    /// 设置条目内容。
    /// </summary>
    public void Setup(string time, string clue, Sprite icon, bool isNewClue)
    {
        if (timeText != null) timeText.text = time;
        if (clueText != null) clueText.text = clue;

        if (clueIcon != null)
        {
            if (icon != null)
            {
                clueIcon.sprite = icon;
                clueIcon.gameObject.SetActive(true);
            }
            else
            {
                clueIcon.gameObject.SetActive(false);
            }
        }

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
