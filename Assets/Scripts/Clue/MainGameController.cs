using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 主游戏控制器。
/// 管理游戏HUD，包括线索墙入口、设置面板等。
/// </summary>
public class MainGameController : MonoBehaviour
{
    [Header("UI引用")]
    public Button enterClueWallButton;
    public Button settingsButton;
    public GameObject mainGameUI;

    [Header("管理器引用")]
    public ClueWallManager clueWallManager;

    [Header("玩家引用")]
    [Tooltip("玩家控制器")]
    [SerializeField] private PlayerController _playerController;

    [Header("设置面板")]
    [Tooltip("设置面板的 CanvasGroup")]
    [SerializeField] private CanvasGroup _settingsGroup;

    private bool _isSettingsOpen = false;

    void Start()
    {
        if (enterClueWallButton != null)
            enterClueWallButton.onClick.AddListener(OnEnterClueWallButtonClick);

        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettingsButtonClick);

        // 确保设置面板初始隐藏
        if (_settingsGroup != null)
            SetGroupVisible(_settingsGroup, false);
    }

    void Update()
    {
        // ESC 键关闭设置面板
        if (_isSettingsOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseSettings();
        }
    }

    private void OnEnterClueWallButtonClick()
    {
        if (clueWallManager != null)
            clueWallManager.OpenClueWall();
    }

    private void OnSettingsButtonClick()
    {
        if (_isSettingsOpen)
            CloseSettings();
        else
            OpenSettings();
    }

    /// <summary>
    /// 打开设置面板。
    /// </summary>
    public void OpenSettings()
    {
        if (_settingsGroup != null)
        {
            SetGroupVisible(_settingsGroup, true);
            _isSettingsOpen = true;

            // 暂停游戏
            Time.timeScale = 0f;

            // 禁用玩家输入
            if (_playerController != null)
                _playerController.SetInputEnabled(false);

            Debug.Log("[MainGame] 设置面板已打开");
        }
    }

    /// <summary>
    /// 关闭设置面板（供 SettingsPanel 调用）。
    /// </summary>
    public void CloseSettings()
    {
        if (_settingsGroup != null)
        {
            SetGroupVisible(_settingsGroup, false);
            _isSettingsOpen = false;

            // 恢复游戏
            Time.timeScale = 1f;

            // 恢复玩家输入
            if (_playerController != null)
                _playerController.SetInputEnabled(true);

            Debug.Log("[MainGame] 设置面板已关闭");
        }
    }

    /// <summary>
    /// 从线索墙返回主游戏（外部调用）。
    /// </summary>
    public void ReturnFromClueWall()
    {
        if (clueWallManager != null)
            clueWallManager.CloseClueWall();

        if (mainGameUI != null)
            mainGameUI.SetActive(true);
    }

    /// <summary>
    /// 通过 CanvasGroup 控制面板的可见性与交互性。
    /// </summary>
    private static void SetGroupVisible(CanvasGroup group, bool visible)
    {
        if (group == null) return;
        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }
}
