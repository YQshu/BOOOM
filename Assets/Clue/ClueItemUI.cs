using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class ClueItemUI : MonoBehaviour
{
    [Header("UI组件")]
    public TMP_Text timeText;           // 时间文本
    public TMP_Text clueText;           // 线索文本
    public Image exclamationMark;   // 感叹号图标
    public Image clueIcon;          // 线索图标（可选）

    [Header("新线索设置")]
    public Color newClueColor = Color.red;  // 新线索的感叹号颜色
    public Color normalClueColor = Color.white; // 正常线索的颜色

    // 私有变量
    private float unlockTime;       // 线索解锁的时间
    private bool isNew = true;      // 是否是新线索
    private float newDuration = 5f; // 新线索持续时间

    /// <summary>
    /// 设置线索信息
    /// </summary>
    public void Setup(string time, string clue, Sprite icon = null, bool isNewClue = true)
    {
        // 设置文本
        if (timeText != null) timeText.text = time;
        if (clueText != null) clueText.text = clue;

        // 设置图标
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

        // 设置新线索状态
        unlockTime = Time.time;
        isNew = isNewClue;

        // 更新感叹号状态
        UpdateExclamationMark();

        // 如果不是新线索，立即更新一次状态
        if (!isNew)
        {
            exclamationMark.gameObject.SetActive(false);
        }
    }

    void Start()
    {
        // 如果感叹号组件为空，尝试查找
        if (exclamationMark == null)
        {
            exclamationMark = transform.Find("ExclamationMark")?.GetComponent<Image>();
        }

        // 初始化
        UpdateExclamationMark();
    }

    void Update()
    {
        // 检查感叹号状态
        CheckExclamationState();
    }

    /// <summary>
    /// 检查感叹号状态
    /// </summary>
    public void CheckExclamationState()
    {
        if (isNew && Time.time - unlockTime > newDuration)
        {
            isNew = false;
            UpdateExclamationMark();
        }
    }

    /// <summary>
    /// 更新感叹号显示状态
    /// </summary>
    private void UpdateExclamationMark()
    {
        if (exclamationMark != null)
        {
            bool shouldShow = isNew;
            exclamationMark.gameObject.SetActive(shouldShow);

            // 设置颜色
            exclamationMark.color = isNew ? newClueColor : normalClueColor;
        }
    }

    /// <summary>
    /// 设置新线索持续时间
    /// </summary>
    public void SetNewDuration(float duration)
    {
        newDuration = duration;
    }

    /// <summary>
    /// 手动标记为已查看（清除新线索状态）
    /// </summary>
    public void MarkAsViewed()
    {
        isNew = false;
        UpdateExclamationMark();
    }

    /// <summary>
    /// 获取线索文本
    /// </summary>
    public string GetClueText()
    {
        return clueText != null ? clueText.text : "";
    }

    /// <summary>
    /// 获取时间文本
    /// </summary>
    public string GetTimeText()
    {
        return timeText != null ? timeText.text : "";
    }

    /// <summary>
    /// 是否是新线索
    /// </summary>
    public bool IsNewClue()
    {
        return isNew;
    }
}
