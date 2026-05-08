using UnityEngine;
using UnityEngine.UI;

public class MainGameController : MonoBehaviour
{
    [Header("UI引用")]
    public Button enterClueWallButton;  // 进入线索墙按钮
    public GameObject mainGameUI;       // 主游戏UI

    [Header("测试按钮")]
    public Button addClueButton;        // 添加测试线索按钮
    public Button clearCluesButton;     // 清空线索按钮

    [Header("管理器引用")]
    public ClueWallManager clueWallManager;

    void Start()
    {
        // 绑定按钮事件
        if (enterClueWallButton != null)
        {
            enterClueWallButton.onClick.AddListener(OnEnterClueWallButtonClick);
        }

        if (addClueButton != null)
        {
            addClueButton.onClick.AddListener(OnAddTestClueClick);
        }

        if (clearCluesButton != null)
        {
            clearCluesButton.onClick.AddListener(OnClearCluesClick);
        }
    }

    /// <summary>
    /// 进入线索墙按钮点击事件
    /// </summary>
    private void OnEnterClueWallButtonClick()
    {
        if (clueWallManager != null)
        {
            clueWallManager.OnEnterClueWall();
        }
    }

    /// <summary>
    /// 添加测试线索按钮点击事件
    /// </summary>
    private void OnAddTestClueClick()
    {
        if (clueWallManager != null)
        {
            clueWallManager.AddTestClues();
        }
    }

    /// <summary>
    /// 清空线索按钮点击事件
    /// </summary>
    private void OnClearCluesClick()
    {
        if (clueWallManager != null)
        {
            clueWallManager.ClearAllClues();
        }
    }

    /// <summary>
    /// 从线索墙返回主游戏
    /// </summary>
    public void ReturnFromClueWall()
    {
        if (clueWallManager != null)
        {
            clueWallManager.OnExitClueWall();
        }

        // 显示主游戏UI
        if (mainGameUI != null)
        {
            mainGameUI.SetActive(true);
        }
    }
}
