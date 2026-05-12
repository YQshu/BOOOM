using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 角色卡片展示组件（可选）。
/// 新版线索墙由 ClueWallManager 直接管理卡片数组，本组件仅作辅助展示用途。
/// </summary>
public class CharacterCardController : MonoBehaviour
{
    [Header("UI组件")]
    public Image avatarImage;
    public TMP_Text nameText;
    public TMP_Text descriptionText;
    public GameObject newClueIndicator;
    public TMP_Text clueCountText;

    /// <summary>
    /// 初始化卡片展示内容。
    /// </summary>
    public void Initialize(CharacterData characterData)
    {
        if (avatarImage != null && characterData.avatar != null)
            avatarImage.sprite = characterData.avatar;

        if (nameText != null)
            nameText.text = characterData.characterName;

        if (descriptionText != null)
            descriptionText.text = characterData.description;
    }

    /// <summary>
    /// 刷新线索数量和新线索红点。
    /// </summary>
    public void RefreshClueCount(int collected, bool hasNew)
    {
        if (clueCountText != null)
            clueCountText.text = collected.ToString();

        if (newClueIndicator != null)
            newClueIndicator.SetActive(hasNew);
    }
}
