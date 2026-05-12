using UnityEngine;
using UnityEngine.UI;

public class MainGameController : MonoBehaviour
{
    [Header("UI引用")]
    public Button enterClueWallButton;
    public GameObject mainGameUI;

    [Header("管理器引用")]
    public ClueWallManager clueWallManager;

    void Start()
    {
        if (enterClueWallButton != null)
            enterClueWallButton.onClick.AddListener(OnEnterClueWallButtonClick);
    }

    private void OnEnterClueWallButtonClick()
    {
        if (clueWallManager != null)
            clueWallManager.OpenClueWall();
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
}
