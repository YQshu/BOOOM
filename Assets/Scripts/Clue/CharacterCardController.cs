using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 角色卡片控制器。
/// 负责角色展示、点击跳转，以及线索数量与新线索提示刷新。
/// </summary>
public class CharacterCardController : MonoBehaviour
{
    [Header("UI组件")]
    [Tooltip("角色头像")]
    public Image avatarImage;
    [Tooltip("角色名文本")]
    public TMP_Text nameText;
    [Tooltip("角色描述文本")]
    public TMP_Text descriptionText;
    [Tooltip("角色卡片按钮")]
    public Button cardButton;

    [Header("状态指示器")]
    [Tooltip("新线索指示器")]
    public GameObject newClueIndicator;
    [Tooltip("线索数量文本")]
    public TMP_Text clueCountText;

    private int characterIndex = -1;
    private ClueWallManager clueWallManager;

    /// <summary>
    /// 初始化角色卡片。
    /// </summary>
    /// <param name="characterData">角色数据。</param>
    /// <param name="index">角色索引。</param>
    /// <param name="manager">线索墙管理器。</param>
    public void Initialize(CharacterData characterData, int index, ClueWallManager manager)
    {
        characterIndex = index;
        clueWallManager = manager;

        if (avatarImage != null && characterData.avatar != null)
        {
            avatarImage.sprite = characterData.avatar;
        }

        if (nameText != null)
        {
            nameText.text = characterData.characterName;
        }

        if (descriptionText != null)
        {
            descriptionText.text = characterData.description;
        }

        if (cardButton != null)
        {
            ColorBlock colors = cardButton.colors;
            colors.normalColor = characterData.themeColor * 0.8f;
            colors.highlightedColor = characterData.themeColor;
            colors.pressedColor = characterData.themeColor * 0.6f;
            cardButton.colors = colors;

            cardButton.onClick.RemoveAllListeners();
            cardButton.onClick.AddListener(OnCardClick);
        }

        UpdateClueIndicator();
    }

    /// <summary>
    /// 卡片点击事件。
    /// </summary>
    private void OnCardClick()
    {
        if (clueWallManager != null && characterIndex >= 0)
        {
            clueWallManager.OnSelectCharacter(characterIndex);
        }
    }

    /// <summary>
    /// 更新线索指示器。
    /// </summary>
    public void UpdateClueIndicator()
    {
        int collectedCount = 0;

        if (ClueManager.Instance != null && clueWallManager != null)
        {
            var collectedClueIds = ClueManager.Instance.GetCollectedClueIds();
            for (int i = 0; i < collectedClueIds.Count; i++)
            {
                int ownerCharacterIndex = clueWallManager.GetCharacterIndexByClueId(collectedClueIds[i]);
                if (ownerCharacterIndex == characterIndex)
                {
                    collectedCount++;
                }
            }
        }

        if (clueCountText != null)
        {
            clueCountText.text = collectedCount.ToString();
        }

        if (newClueIndicator != null)
        {
            newClueIndicator.SetActive(collectedCount > 0);
        }
    }
}
