using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 回溯时间管理器，负责处理回溯输入、状态切换、全局时间缩放与视觉效果。
/// </summary>
public class RewindTimeManager : MonoBehaviour
{
    [Header("单例")]
    [Tooltip("全局回溯管理器实例")]
    public static RewindTimeManager Instance;

    [Header("回溯设置")]
    [Tooltip("触发回溯的按键")]
    [SerializeField] private KeyCode _rewindKey = KeyCode.Space;
    [Tooltip("回溯时的时间倍率")]
    [SerializeField] private float _rewindSpeed = 2f;

    [Header("视觉效果")]
    [Tooltip("回溯状态下相机背景目标颜色")]
    [SerializeField] private Color _rewindCameraColor = new Color(0.5f, 0.3f, 0.6f);
    [Tooltip("相机颜色过渡速度")]
    [SerializeField] private float _rewindTransitionSpeed = 5f;

    [Header("调试")]
    [Tooltip("是否输出回溯状态调试日志")]
    [SerializeField] private bool _showDebugInfo = true;
    [Tooltip("调试日志刷新间隔（秒）")]
    [SerializeField] private float _updateInterval = 0.5f;

    /// <summary>
    /// 当前回溯状态。
    /// </summary>
    public RewindState CurrentState { get; private set; } = RewindState.Normal;

    /// <summary>
    /// 回溯进度（0-1）。
    /// </summary>
    public float RewindProgress { get; private set; }

    public System.Action OnRewindStarted;
    public System.Action OnRewindStopped;
    public System.Action OnRewindPaused;
    public System.Action OnRewindResumed;

    private readonly List<IRewindable> _rewindableObjects = new List<IRewindable>();
    private Camera _mainCamera;
    private Color _originalCameraColor;
    private float _debugTimer = 0f;
    private float _rewindTimer = 0f;
    private float _maxRewindTime = 0f;



    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        _mainCamera = Camera.main;
        if (_mainCamera != null)
        {
            _originalCameraColor = _mainCamera.backgroundColor;
        }
    }

    private void Start()
    {
        FindRewindableObjects();
    }

    private void Update()
    {
        HandleRewindInput();
        UpdateVisualEffects();

        if (_showDebugInfo)
        {
            UpdateDebugInfo();
        }
    }

    private void HandleRewindInput()
    {
        // 按下空格：开始回溯
        if (Input.GetKeyDown(_rewindKey) && CurrentState != RewindState.Rewinding)
        {
            StartRewind();
        }

        // 松开空格：暂停回溯
        if (Input.GetKeyUp(_rewindKey) && CurrentState == RewindState.Rewinding)
        {
            PauseRewind();
        }

        // ESC键：强制停止回溯
        if (Input.GetKeyDown(KeyCode.Escape) && CurrentState != RewindState.Normal)
        {
            StopRewind();
        }
    }

    /// <summary>
    /// 开始回溯；若当前为暂停态则恢复回溯。
    /// </summary>
    public void StartRewind()
    {
        if (CurrentState == RewindState.Rewinding) return;

        if (CurrentState == RewindState.Paused)
        {
            ResumeRewind();
            return;
        }

        CurrentState = RewindState.Rewinding;

        foreach (var obj in _rewindableObjects)
        {
            if (obj != null)
            {
                obj.SetRewindState(CurrentState);
            }
        }

        _maxRewindTime = CalculateMaxRewindTime();
        _rewindTimer = 0f;
        Time.timeScale = _rewindSpeed;

        OnRewindStarted?.Invoke();

        Debug.Log($"[RewindTimeManager] 开始回溯，最大回溯时间: {_maxRewindTime:F2}秒");
    }

    /// <summary>
    /// 暂停回溯并冻结时间。
    /// </summary>
    public void PauseRewind()
    {
        if (CurrentState != RewindState.Rewinding) return;

        CurrentState = RewindState.Paused;

        foreach (var obj in _rewindableObjects)
        {
            if (obj != null)
            {
                obj.SetRewindState(CurrentState);
            }
        }

        Time.timeScale = 0f;

        OnRewindPaused?.Invoke();

        Debug.Log("[RewindTimeManager] 回溯已暂停（空格已松开）");
    }

    /// <summary>
    /// 从暂停态恢复回溯。
    /// </summary>
    public void ResumeRewind()
    {
        if (CurrentState != RewindState.Paused) return;

        CurrentState = RewindState.Rewinding;

        // 通知所有可回溯物体
        foreach (var obj in _rewindableObjects)
        {
            if (obj != null)
            {
                obj.SetRewindState(CurrentState);
            }
        }

        Time.timeScale = _rewindSpeed;

        OnRewindResumed?.Invoke();

        Debug.Log("[RewindTimeManager] 恢复回溯");
    }

    /// <summary>
    /// 停止回溯并恢复正常时间。
    /// </summary>
    public void StopRewind()
    {
        if (CurrentState == RewindState.Normal) return;

        RewindState previousState = CurrentState;
        CurrentState = RewindState.Normal;

        foreach (var obj in _rewindableObjects)
        {
            if (obj != null)
            {
                obj.SetRewindState(CurrentState);
            }
        }

        Time.timeScale = 1f;
        _rewindTimer = 0f;
        RewindProgress = 0f;

        OnRewindStopped?.Invoke();

        Debug.Log($"[RewindTimeManager] 停止回溯，之前状态: {previousState}");
    }

    /// <summary>
    /// 注册可回溯对象。
    /// </summary>
    /// <param name="rewindable">目标对象。</param>
    public void RegisterRewindable(IRewindable rewindable)
    {
        if (!_rewindableObjects.Contains(rewindable))
        {
            _rewindableObjects.Add(rewindable);
        }
    }

    /// <summary>
    /// 反注册可回溯对象。
    /// </summary>
    /// <param name="rewindable">目标对象。</param>
    public void UnregisterRewindable(IRewindable rewindable)
    {
        _rewindableObjects.Remove(rewindable);
    }

    /// <summary>
    /// 更新视觉效果，以反映当前回溯状态。
    /// </summary>
    private void UpdateVisualEffects()
    {
        if (_mainCamera == null) return;

        Color targetColor = CurrentState != RewindState.Normal ? _rewindCameraColor : _originalCameraColor;

        _mainCamera.backgroundColor = Color.Lerp(
            _mainCamera.backgroundColor,
            targetColor,
            Time.unscaledDeltaTime * _rewindTransitionSpeed
        );

        if (CurrentState == RewindState.Rewinding)
        {
            _rewindTimer += Time.unscaledDeltaTime;
            RewindProgress = Mathf.Clamp01(_rewindTimer / Mathf.Max(_maxRewindTime, 0.1f));
        }
    }

    /// <summary>
    /// 计算所有回溯对象中的最大可回溯时长。
    /// </summary>
    /// <returns>可回溯时长（秒）</returns>
    private float CalculateMaxRewindTime()
    {
        float maxTime = 0f;
        foreach (var obj in _rewindableObjects)
        {
            RewindableObject rewindObj = obj as RewindableObject;
            if (rewindObj != null)
            {
                maxTime = rewindObj.GetMaxRewindTime() > maxTime ? rewindObj.GetMaxRewindTime() : maxTime;
            }
        }
        return maxTime;
    }

    /// <summary>
    /// 查找并缓存当前场景中的可回溯对象。
    /// </summary>
    private void FindRewindableObjects()
    {
        _rewindableObjects.Clear();

        var allRewindableObjects = FindObjectsOfType<RewindableObject>();
        foreach (var obj in allRewindableObjects)
        {
            _rewindableObjects.Add(obj);
        }

        Debug.Log($"[RewindTimeManager] 找到 {_rewindableObjects.Count} 个可回溯物体");
    }

    /// <summary>
    /// 按固定间隔输出调试信息。
    /// </summary>
    private void UpdateDebugInfo()
    {
        _debugTimer += Time.deltaTime;
        if (_debugTimer >= _updateInterval)
        {
            _debugTimer = 0f;

            string stateStr = CurrentState.ToString();
            string progressStr = $"进度: {RewindProgress * 100:F1}%";
            string objectsStr = $"物体数: {_rewindableObjects.Count}";

            Debug.Log($"[RewindTimeManager] 状态: {stateStr} | {progressStr} | {objectsStr}");
        }
    }
}
