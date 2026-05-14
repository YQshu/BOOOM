using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 主菜单控制器。挂在 MainMenu 场景的 Canvas 根节点上。
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("按钮")]
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _continueButton;
    [SerializeField] private Button _settingsButton;
    [SerializeField] private Button _quitButton;

    [Header("按钮文本（可选）")]
    [Tooltip("开始按钮的文本组件，用于切换’开始游戏’/’新的开始’")]
    [SerializeField] private TMP_Text _startButtonText;

    [Header("面板（需要 CanvasGroup 组件）")]
    [Tooltip("主菜单面板的 CanvasGroup，用于与设置面板切换")]
    [SerializeField] private CanvasGroup _mainMenuGroup;
    [Tooltip("设置面板的 CanvasGroup")]
    [SerializeField] private CanvasGroup _settingsGroup;

    [Header("场景")]
    [SerializeField] private string _gameSceneName = "SampleScene";

    [Header("淡入效果（可选）")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private float _fadeInDuration = 1f;

    [Header("调试")]
    [SerializeField] private bool _enableLog = true;

    private void Start()
    {
        // 确保面板初始状态正确
        if (_mainMenuGroup != null)
            SetGroupVisible(_mainMenuGroup, true);
        if (_settingsGroup != null)
            SetGroupVisible(_settingsGroup, false);

        // 检查存档状态
        bool hasSave = SaveManager.Instance != null && SaveManager.Instance.HasSaveFile();

        if (hasSave)
        {
            // 有存档：显示"新的开始"和"继续游戏"
            if (_startButtonText != null)
                _startButtonText.text = "新的开始";

            if (_continueButton != null)
                _continueButton.gameObject.SetActive(true);

            _startButton?.onClick.AddListener(StartNewGame);
            _continueButton?.onClick.AddListener(ContinueGame);

            if (_enableLog)
                Debug.Log("[MainMenu] 检测到存档，显示继续游戏选项");
        }
        else
        {
            // 无存档：只显示"开始游戏"
            if (_startButtonText != null)
                _startButtonText.text = "开始游戏";

            if (_continueButton != null)
                _continueButton.gameObject.SetActive(false);

            _startButton?.onClick.AddListener(StartNewGame);

            if (_enableLog)
                Debug.Log("[MainMenu] 无存档，显示开始游戏");
        }

        if (_settingsButton != null)
            _settingsButton.onClick.AddListener(ShowSettings);
        _quitButton?.onClick.AddListener(QuitGame);

        if (_canvasGroup != null && _fadeInDuration > 0f)
            StartCoroutine(FadeIn());
    }

    /// <summary>
    /// 显示主菜单面板（供 SettingsPanel 返回时调用）。
    /// </summary>
    public void ShowMainMenu()
    {
        if (_mainMenuGroup != null)
            SetGroupVisible(_mainMenuGroup, true);
        if (_settingsGroup != null)
            SetGroupVisible(_settingsGroup, false);

        // 返回主菜单时刷新存档状态
        RefreshSaveStatus();
    }

    /// <summary>
    /// 显示设置面板，隐藏主菜单。
    /// </summary>
    public void ShowSettings()
    {
        if (_mainMenuGroup != null)
            SetGroupVisible(_mainMenuGroup, false);
        if (_settingsGroup != null)
            SetGroupVisible(_settingsGroup, true);
    }

    /// <summary>
    /// 刷新存档状态（清除存档后调用）。
    /// </summary>
    public void RefreshSaveStatus()
    {
        bool hasSave = SaveManager.Instance != null && SaveManager.Instance.HasSaveFile();

        if (hasSave)
        {
            // 有存档：显示"新的开始"和"继续游戏"
            if (_startButtonText != null)
                _startButtonText.text = "新的开始";

            if (_continueButton != null)
                _continueButton.gameObject.SetActive(true);

            if (_enableLog)
                Debug.Log("[MainMenu] 刷新状态：检测到存档");
        }
        else
        {
            // 无存档：只显示"开始游戏"
            if (_startButtonText != null)
                _startButtonText.text = "开始游戏";

            if (_continueButton != null)
                _continueButton.gameObject.SetActive(false);

            if (_enableLog)
                Debug.Log("[MainMenu] 刷新状态：无存档");
        }
    }

    private void StartNewGame()
    {
        // 删除旧存档
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.DeleteSave();
        }

        // 清除加载标记
        GameSceneInitializer.ClearLoadSaveFlag();

        if (_enableLog)
            Debug.Log("[MainMenu] 开始新游戏");

        SceneManager.LoadScene(_gameSceneName);
    }

    private void ContinueGame()
    {
        // 设置加载存档标记
        GameSceneInitializer.SetLoadSaveFlag();

        if (_enableLog)
            Debug.Log("[MainMenu] 继续游戏");

        SceneManager.LoadScene(_gameSceneName);
    }

    private void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private IEnumerator FadeIn()
    {
        _canvasGroup.alpha = 0f;
        float elapsed = 0f;
        while (elapsed < _fadeInDuration)
        {
            elapsed += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Clamp01(elapsed / _fadeInDuration);
            yield return null;
        }
        _canvasGroup.alpha = 1f;
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
