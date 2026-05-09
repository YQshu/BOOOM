using UnityEditor.U2D.Animation;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterCardController : MonoBehaviour
{
    [Header("UI组件")]
    public Image avatarImage;      // 头像
    public TMP_Text nameText;          // 角色名
    public TMP_Text descriptionText;   // 描述
    public Button cardButton;      // 卡片按钮

    [Header("状态指示器")]
    public GameObject newClueIndicator;  // 新线索指示器
    public TMP_Text clueCountText;           // 线索数量

    // 角色索引
    private int characterIndex = -1;

    // 引用
    private ClueWallManager clueWallManager;

    /// <summary>
    /// 初始化角色卡片
    /// </summary>
    public void Initialize(CharacterData characterData, int index, ClueWallManager manager)
    {
        characterIndex = index;
        clueWallManager = manager;

        // 设置UI
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

        // 设置按钮颜色
        if (cardButton != null)
        {
            var colors = cardButton.colors;
            colors.normalColor = characterData.themeColor * 0.8f;
            colors.highlightedColor = characterData.themeColor;
            colors.pressedColor = characterData.themeColor * 0.6f;
            cardButton.colors = colors;
        }

        // 绑定点击事件
        if (cardButton != null)
        {
            cardButton.onClick.RemoveAllListeners();
            cardButton.onClick.AddListener(OnCardClick);
        }

        // 更新线索指示器
        UpdateClueIndicator();
    }

    /// <summary>
    /// 卡片点击事件
    /// </summary>
    private void OnCardClick()
    {
        if (clueWallManager != null && characterIndex >= 0)
        {
            clueWallManager.OnSelectCharacter(characterIndex);
        }
    }

    /// <summary>
    /// 更新线索指示器
    /// </summary>
    public void UpdateClueIndicator()
    {
        // 这里可以添加逻辑来检查这个角色是否有新线索
        // 暂时设置为随机
        bool hasNewClues = Random.Range(0, 2) == 0;

        if (newClueIndicator != null)
        {
            newClueIndicator.SetActive(hasNewClues);
        }

        // 更新线索数量
        if (clueCountText != null)
        {
            int clueCount = Random.Range(1, 10);
            clueCountText.text = clueCount.ToString();
        }
    }
}
