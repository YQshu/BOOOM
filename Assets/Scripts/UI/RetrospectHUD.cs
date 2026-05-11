using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 回溯模式HUD控制器。
/// 管理进度条、控制按钮、角色名显示和退出按钮的状态更新。
/// </summary>
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
    [Tooltip("退出回溯按钮")]
    [SerializeField] private Button _exitButton;

    [Header("引用")]
    [SerializeField] private RetrospectManager _retrospectManager;
    [SerializeField] private TimelineRewindManager _rewindManager;

    private void Start()
    {
        if (_retrospectManager == null)
            _retrospectManager = RetrospectManager.Instance;
        if (_rewindManager == null)
            _rewindManager = FindObjectOfType<TimelineRewindManager>();

        // 绑定按钮事件
        if (_pauseButton != null)
            _pauseButton.onClick.AddListener(OnPauseClicked);
        if (_exitButton != null)
            _exitButton.onClick.AddListener(OnExitClicked);

        // 订阅回溯事件
        if (_retrospectManager != null)
        {
            _retrospectManager.OnRetrospectEnter += OnRetrospectEnter;
            _retrospectManager.OnRetrospectExit += OnRetrospectExit;
        }

        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_retrospectManager != null)
        {
            _retrospectManager.OnRetrospectEnter -= OnRetrospectEnter;
            _retrospectManager.OnRetrospectExit -= OnRetrospectExit;
        }
    }

    private void Update()
    {
        if (!gameObject.activeSelf) return;

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
        gameObject.SetActive(true);
    }

    private void OnRetrospectExit()
    {
        gameObject.SetActive(false);
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

    /// <summary>
    /// 更新时间显示文本。
    /// </summary>
    private void UpdateTimeDisplay()
    {
        if (_timeText == null || _rewindManager == null) return;

        var director = _rewindManager.GetDirector();
        if (director == null) return;

        double current = director.time;
        double total = director.duration;

        _timeText.text = $"{FormatTime(current)} / {FormatTime(total)}";
    }

    /// <summary>
    /// 根据播放状态切换暂停/继续图标。
    /// </summary>
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
}
