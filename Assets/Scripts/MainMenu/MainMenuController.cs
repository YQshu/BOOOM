using InnsmouthCafe.Audio;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 主菜单控制器。
    /// 管理开始游戏、设置、退出三个按钮以及主界面与设置面板之间的切换。
    /// 使用 CanvasGroup 控制面板可见性，避免 SetActive(false) 导致脚本失活的问题。
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        // ── Inspector 字段 ────────────────────────────────────────

        [Header("按钮")]
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _quitButton;

        [Header("面板（需要 CanvasGroup 组件）")]
        [SerializeField] private CanvasGroup _mainMenuGroup;
        [SerializeField] private CanvasGroup _settingsGroup;

        [Header("场景")]
        [SerializeField] private string _gameSceneName = "SampleScene";

        // ── 生命周期 ──────────────────────────────────────────────

        private void Start()
        {
            // 启动时加载并应用显示设置
            SettingsManager.Load();

            // 确保初始状态正确
            SetGroupVisible(_mainMenuGroup, true);
            SetGroupVisible(_settingsGroup, false);

            // 绑定按钮事件
            _startButton.onClick.AddListener(OnStartClicked);
            _settingsButton.onClick.AddListener(OnSettingsClicked);
            _quitButton.onClick.AddListener(OnQuitClicked);
        }

        // ── 公开方法 ──────────────────────────────────────────────

        /// <summary>
        /// 显示主菜单面板（供 SettingsPanel 返回时调用）。
        /// </summary>
        public void ShowMainMenu()
        {
            SetGroupVisible(_mainMenuGroup, true);
            SetGroupVisible(_settingsGroup, false);
        }

        /// <summary>
        /// 显示设置面板，隐藏主菜单。
        /// </summary>
        public void ShowSettings()
        {
            SetGroupVisible(_mainMenuGroup, false);
            SetGroupVisible(_settingsGroup, true);
        }

        // ── 按钮回调 ──────────────────────────────────────────────

        private void OnStartClicked()
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySfx(SoundId.ButtonClick);

            SceneManager.LoadScene(_gameSceneName);
        }

        private void OnSettingsClicked()
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySfx(SoundId.ButtonClick);

            ShowSettings();
        }

        private void OnQuitClicked()
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySfx(SoundId.ButtonClick);

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ── 工具方法 ──────────────────────────────────────────────

        /// <summary>
        /// 通过 CanvasGroup 控制面板的可见性与交互性。
        /// alpha=0 + interactable=false + blocksRaycasts=false = 完全隐藏且不响应输入。
        /// </summary>
        private static void SetGroupVisible(CanvasGroup group, bool visible)
        {
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }
    }
}
