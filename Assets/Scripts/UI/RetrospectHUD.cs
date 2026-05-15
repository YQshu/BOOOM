using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 回溯模式HUD控制器。
/// 管理进度条、控制按钮、角色名显示和退出按钮的状态更新。
/// 通过CanvasGroup控制显隐，物体始终保持激活以确保事件订阅正常。
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class RetrospectHUD : MonoBehaviour
{
    [Header("信息显示")]
    [Tooltip("当前回溯的角色名称")]
    [SerializeField] private TMP_Text _characterNameText;
    [Tooltip("当前时间 / 总时长，格式：00:00 / 00:00")]
    [SerializeField] private TMP_Text _timeText;

    [Header("控制按钮")]
    [Tooltip("暂停/继续按钮")]
    [SerializeField] private Button _pauseButton;
    [Tooltip("暂停图标（播放中时显示）")]
    [SerializeField] private GameObject _pauseIcon;
    [Tooltip("继续图标（暂停时显示）")]
    [SerializeField] private GameObject _playIcon;
    [Tooltip("倍速按钮")]
    [SerializeField] private Button _speedButton;
    [Tooltip("倍速按钮文本")]
    [SerializeField] private TMP_Text _speedButtonText;
    [Tooltip("退出回溯按钮")]
    [SerializeField] private Button _exitButton;

    [Header("倍速配置")]
    [Tooltip("可用的播放倍速")]
    [SerializeField] private float[] _availableSpeeds = { 1f, 2f, 3f };

    [Header("引用")]
    [SerializeField] private RetrospectManager _retrospectManager;
    [SerializeField] private TimelineRewindManager _rewindManager;

    private CanvasGroup _canvasGroup;
    private int _currentSpeedIndex = 0;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        SetVisible(false);
    }

    private void Start()
    {
        if (_retrospectManager == null)
            _retrospectManager = RetrospectManager.Instance;
        if (_rewindManager == null)
            _rewindManager = FindObjectOfType<TimelineRewindManager>();

        if (_pauseButton != null)
            _pauseButton.onClick.AddListener(OnPauseClicked);
        if (_speedButton != null)
            _speedButton.onClick.AddListener(OnSpeedClicked);
        if (_exitButton != null)
            _exitButton.onClick.AddListener(OnExitClicked);

        if (_retrospectManager != null)
        {
            _retrospectManager.OnRetrospectEnter += OnRetrospectEnter;
            _retrospectManager.OnRetrospectExit += OnRetrospectExit;
        }

        // 订阅对话开始/结束事件
        if (InkDialogueManager.Instance != null)
        {
            InkDialogueManager.Instance.OnDialogueStart += OnDialogueStart;
            InkDialogueManager.Instance.OnDialogueEnd += OnDialogueEnd;
        }

        // 初始化倍速显示
        UpdateSpeedButtonText();
    }

    private void OnDestroy()
    {
        if (_retrospectManager != null)
        {
            _retrospectManager.OnRetrospectEnter -= OnRetrospectEnter;
            _retrospectManager.OnRetrospectExit -= OnRetrospectExit;
        }

        if (InkDialogueManager.Instance != null)
        {
            InkDialogueManager.Instance.OnDialogueStart -= OnDialogueStart;
            InkDialogueManager.Instance.OnDialogueEnd -= OnDialogueEnd;
        }
    }

    private void Update()
    {
        if (_canvasGroup.alpha < 0.01f) return;

        UpdateTimeDisplay();
        UpdatePauseIcon();
    }

    // ─── 公开接口 ────────────────────────────────────────────

    /// <summary>
    /// 设置角色名称（进入回溯时调用）。
    /// </summary>
    public void SetCharacterName(string name)
    {
        if (_characterNameText != null)
            _characterNameText.text = name;
    }

    // ─── 内部逻辑 ────────────────────────────────────────────

    private void OnRetrospectEnter()
    {
        SetVisible(true);
        // 进入回溯时重置为1倍速
        ResetSpeed();
    }

    private void OnRetrospectExit()
    {
        SetVisible(false);
        // 退出回溯时重置为1倍速
        ResetSpeed();
    }

    private void SetVisible(bool visible)
    {
        if (_canvasGroup == null) return;
        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.interactable = visible;
        _canvasGroup.blocksRaycasts = visible;
    }

    private void OnPauseClicked()
    {
        if (_rewindManager == null) return;

        if (_rewindManager.IsPaused())
            _rewindManager.Resume();
        else
            _rewindManager.Pause();
    }

    private void OnExitClicked()
    {
        if (_retrospectManager != null)
            _retrospectManager.ExitRetrospect();
    }

    private void OnDialogueStart()
    {
        // 对话开始时：重置为1倍速 + 禁用倍速按钮
        ResetSpeed();
        if (_speedButton != null)
        {
            _speedButton.interactable = false;
        }
    }

    private void OnDialogueEnd()
    {
        // 对话结束时：启用倍速按钮
        if (_speedButton != null)
        {
            _speedButton.interactable = true;
        }
    }

    private void UpdateTimeDisplay()
    {
        if (_timeText == null || _rewindManager == null) return;

        var director = _rewindManager.GetDirector();
        if (director == null) return;

        double current = director.time;
        double total = director.duration;
        _timeText.text = $"{FormatTime(current)} / {FormatTime(total)}";
    }

    private void UpdatePauseIcon()
    {
        if (_rewindManager == null) return;

        bool isPaused = _rewindManager.IsPaused() || _rewindManager.IsRewinding();
        if (_pauseIcon != null) _pauseIcon.SetActive(!isPaused);
        if (_playIcon != null) _playIcon.SetActive(isPaused);
    }

    private string FormatTime(double seconds)
    {
        int m = (int)(seconds / 60);
        int s = (int)(seconds % 60);
        return $"{m:00}:{s:00}";
    }

    // ─── 倍速控制 ────────────────────────────────────────────

    private void OnSpeedClicked()
    {
        if (_rewindManager == null) return;

        // 循环切换倍速
        _currentSpeedIndex = (_currentSpeedIndex + 1) % _availableSpeeds.Length;
        float newSpeed = _availableSpeeds[_currentSpeedIndex];

        // 设置播放速度
        _rewindManager.SetPlaybackSpeed(newSpeed);

        // 更新按钮显示
        UpdateSpeedButtonText();
    }

    private void UpdateSpeedButtonText()
    {
        if (_speedButtonText == null) return;

        float currentSpeed = _availableSpeeds[_currentSpeedIndex];
        _speedButtonText.text = $"{currentSpeed:F0}x";
    }

    /// <summary>
    /// 重置倍速为1x（公开方法，供外部调用）
    /// </summary>
    public void ResetSpeed()
    {
        _currentSpeedIndex = 0;

        if (_rewindManager != null)
        {
            _rewindManager.SetPlaybackSpeed(_availableSpeeds[0]);
        }

        UpdateSpeedButtonText();
    }
}
