using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
/// <summary>
/// 设置面板 UI 控制器。
/// 管理音量滑条、显示模式下拉框、分辨率下拉框、返回按钮。
/// </summary>
public class SettingsPanel : MonoBehaviour
{
    // ── Inspector 字段 ────────────────────────────────────────

    [Header("音量")]
    [SerializeField] private Slider _bgmSlider;
    [SerializeField] private Slider _sfxSlider;

    [Header("显示")]
    [SerializeField] private TMP_Dropdown _displayModeDropdown;
    [SerializeField] private TMP_Dropdown _resolutionDropdown;

    [Header("导航")]
    [SerializeField] private Button _backButton;
    [SerializeField] private Button _clearSaveButton;
    [SerializeField] private Button _returnToMainMenuButton;

    [Header("依赖")]
    [Tooltip("主菜单控制器（仅主菜单场景使用）")]
    [SerializeField] private MainMenuController _mainMenuController;
    [Tooltip("游戏控制器（仅游戏场景使用）")]
    [SerializeField] private MainGameController _mainGameController;

    // ── 运行时字段 ────────────────────────────────────────────

    /// <summary>缓存的可用分辨率列表（去重后）。</summary>
    private List<Resolution> _availableResolutions;

    /// <summary>标记是否已完成首次初始化。</summary>
    private bool _initialized;

    // ── 生命周期 ──────────────────────────────────────────────

    private void Start()
    {
        InitSliders();
        InitDisplayModeDropdown();
        RefreshResolutionDropdown();
        BindEvents();
        UpdateButtonVisibility();
        _initialized = true;
    }

    /// <summary>
    /// 根据场景更新按钮可见性。
    /// 主菜单场景：隐藏"返回主菜单"按钮
    /// 游戏场景：显示"返回主菜单"按钮
    /// </summary>
    private void UpdateButtonVisibility()
    {
        if (_returnToMainMenuButton != null)
        {
            // 如果在游戏场景（有 MainGameController），显示返回主菜单按钮
            bool isInGameScene = _mainGameController != null;
            _returnToMainMenuButton.gameObject.SetActive(isInGameScene);
        }
    }

    /// <summary>
    /// 每次面板变为可见时，刷新 UI 控件的值以反映最新设置。
    /// 首次由 Start 处理，后续由 CanvasGroup 可见性切换触发。
    /// </summary>
    public void OnPanelShown()
    {
        if (!_initialized) return;
        _bgmSlider.SetValueWithoutNotify(SettingsManager.GetBgmVolume());
        _sfxSlider.SetValueWithoutNotify(SettingsManager.GetSfxVolume());
        _displayModeDropdown.SetValueWithoutNotify(SettingsManager.GetDisplayMode());
        RefreshResolutionDropdown();
    }

    // ── 初始化 ────────────────────────────────────────────────

    private void InitSliders()
    {
        _bgmSlider.SetValueWithoutNotify(SettingsManager.GetBgmVolume());
        _sfxSlider.SetValueWithoutNotify(SettingsManager.GetSfxVolume());
    }

    private void InitDisplayModeDropdown()
    {
        _displayModeDropdown.ClearOptions();
        _displayModeDropdown.AddOptions(new List<string> { "全屏", "窗口" });
        _displayModeDropdown.SetValueWithoutNotify(SettingsManager.GetDisplayMode());
    }

    /// <summary>
    /// 刷新分辨率下拉框。
    /// 窗口模式下填充系统可用分辨率；全屏 / 无边框模式下清空并禁用交互。
    /// </summary>
    private void RefreshResolutionDropdown()
    {
        int mode = SettingsManager.GetDisplayMode();
        bool isWindowed = mode == 1;

        _resolutionDropdown.ClearOptions();
        _resolutionDropdown.interactable = isWindowed;

        if (!isWindowed)
        {
            _resolutionDropdown.AddOptions(new List<string> { "跟随系统" });
            return;
        }

        // 收集并去重可用分辨率（只保留宽×高，忽略刷新率差异）
        _availableResolutions = new List<Resolution>();
        var seen = new HashSet<string>();

        foreach (var res in Screen.resolutions)
        {
            string key = $"{res.width}x{res.height}";
            if (seen.Add(key))
            {
                _availableResolutions.Add(res);
            }
        }

        // 按分辨率从大到小排列
        _availableResolutions.Sort((a, b) =>
        {
            int cmp = b.width.CompareTo(a.width);
            return cmp != 0 ? cmp : b.height.CompareTo(a.height);
        });

        var options = new List<string>();
        int currentW = SettingsManager.GetResolutionWidth();
        int currentH = SettingsManager.GetResolutionHeight();
        int selectedIndex = 0;

        for (int i = 0; i < _availableResolutions.Count; i++)
        {
            var r = _availableResolutions[i];
            options.Add($"{r.width} x {r.height}");
            if (r.width == currentW && r.height == currentH)
                selectedIndex = i;
        }

        _resolutionDropdown.AddOptions(options);
        _resolutionDropdown.SetValueWithoutNotify(selectedIndex);
    }

    // ── 事件绑定 ──────────────────────────────────────────────

    private void BindEvents()
    {
        _bgmSlider.onValueChanged.AddListener(OnBgmVolumeChanged);
        _sfxSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        _displayModeDropdown.onValueChanged.AddListener(OnDisplayModeChanged);
        _resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        _backButton.onClick.AddListener(OnBackClicked);

        if (_clearSaveButton != null)
            _clearSaveButton.onClick.AddListener(OnClearSaveClicked);

        if (_returnToMainMenuButton != null)
            _returnToMainMenuButton.onClick.AddListener(OnReturnToMainMenuClicked);

        // SFX 滑条松手时播放测试音效，让用户听到实际效果
        AddPointerUpHandler(_sfxSlider.gameObject, OnSfxSliderPointerUp);
    }

    // ── 回调 ──────────────────────────────────────────────────

    private void OnBgmVolumeChanged(float value)
    {
        SettingsManager.SetBgmVolume(value);
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetBgmVolume(value);
    }

    private void OnSfxVolumeChanged(float value)
    {
        SettingsManager.SetSfxVolume(value);
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetSfxVolume(value);
    }

    /// <summary>SFX 滑条松手时播放一个 ButtonClick 音效作为试听反馈。</summary>
    private void OnSfxSliderPointerUp()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx(SoundId.ButtonClick);
    }

    private void OnDisplayModeChanged(int index)
    {
        SettingsManager.SetDisplayMode(index);
        RefreshResolutionDropdown();
        SettingsManager.Apply();
    }

    private void OnResolutionChanged(int index)
    {
        if (_availableResolutions == null || index < 0 || index >= _availableResolutions.Count)
            return;

        var res = _availableResolutions[index];
        SettingsManager.SetResolution(res.width, res.height);
        SettingsManager.Apply();
    }

    private void OnBackClicked()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx(SoundId.ButtonClick);

        // 根据场景判断调用哪个控制器
        if (_mainMenuController != null)
        {
            // 主菜单场景
            _mainMenuController.ShowMainMenu();
        }
        else if (_mainGameController != null)
        {
            // 游戏场景
            _mainGameController.CloseSettings();
        }
        else
        {
            Debug.LogWarning("[SettingsPanel] 未找到控制器引用");
        }
    }

    private void OnClearSaveClicked()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx(SoundId.ButtonClick);

        // 删除存档
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.DeleteSave();
            Debug.Log("[Settings] 存档已清除");
        }
        else
        {
            Debug.LogWarning("[Settings] SaveManager 未找到，无法清除存档");
        }
    }

    /// <summary>
    /// 返回主菜单按钮点击事件。
    /// </summary>
    private void OnReturnToMainMenuClicked()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx(SoundId.ButtonClick);

        // 恢复时间缩放（防止暂停状态）
        Time.timeScale = 1f;

        // 加载主菜单场景
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    // ── 工具方法 ──────────────────────────────────────────────

    /// <summary>
    /// 为目标 GameObject 添加 PointerUp 事件监听（用于检测滑条松手）。
    /// </summary>
    private static void AddPointerUpHandler(GameObject target, System.Action callback)
    {
        var trigger = target.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = target.AddComponent<EventTrigger>();

        var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        entry.callback.AddListener(_ => callback());
        trigger.triggers.Add(entry);
    }
}

