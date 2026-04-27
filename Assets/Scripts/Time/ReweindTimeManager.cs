using UnityEngine;
using System.Collections.Generic;

public class RewindTimeManager : MonoBehaviour
{
    [Header("单例")]
    public static RewindTimeManager Instance;

    [Header("回溯设置")]
    [SerializeField] private KeyCode _rewindKey = KeyCode.Space;
    [SerializeField] private float _rewindSpeed = 2f;  // 回溯速度倍率

    [Header("视觉效果")]
    [SerializeField] private Color _rewindCameraColor = new Color(0.5f, 0.3f, 0.6f);
    [SerializeField] private float _rewindTransitionSpeed = 5f;

    [Header("调试")]
    [SerializeField] private bool _showDebugInfo = true;
    [SerializeField] private float _updateInterval = 0.5f;

    public enum RewindState
    {
        Normal,      // 正常状态
        Rewinding,   // 正在回溯
        Paused       // 暂停（空格松开）
    }

    public RewindState CurrentState { get; private set; } = RewindState.Normal;
    public float RewindProgress { get; private set; }  // 0-1，已回溯的时间比例

    public System.Action OnRewindStarted;
    public System.Action OnRewindStopped;
    public System.Action OnRewindPaused;
    public System.Action OnRewindResumed;

    private List<IRewindable> _rewindableObjects = new List<IRewindable>();
    private Camera _mainCamera;
    private Color _originalCameraColor;
    private float _debugTimer = 0f;
    private float _rewindTimer = 0f;
    private float _maxRewindTime = 0f;

    public interface IRewindable
    {
        void SetRewindState(RewindState state);
        void ClearHistory();
    }

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

    public void StartRewind()
    {
        if (CurrentState == RewindState.Rewinding) return;

        // 从暂停恢复
        if (CurrentState == RewindState.Paused)
        {
            ResumeRewind();
            return;
        }

        // 从正常状态开始回溯
        CurrentState = RewindState.Rewinding;

        // 通知所有可回溯物体
        foreach (var obj in _rewindableObjects)
        {
            if (obj != null)
            {
                obj.SetRewindState(CurrentState);
            }
        }

        // 计算最大回溯时间
        _maxRewindTime = CalculateMaxRewindTime();
        _rewindTimer = 0f;

        // 时间缩放调整
        Time.timeScale = _rewindSpeed;

        OnRewindStarted?.Invoke();

        Debug.Log($"[RewindTimeManager] 开始回溯，最大回溯时间: {_maxRewindTime:F2}秒");
    }

    public void PauseRewind()
    {
        if (CurrentState != RewindState.Rewinding) return;

        CurrentState = RewindState.Paused;

        // 通知所有可回溯物体
        foreach (var obj in _rewindableObjects)
        {
            if (obj != null)
            {
                obj.SetRewindState(CurrentState);
            }
        }

        // 完全暂停
        Time.timeScale = 0f;

        OnRewindPaused?.Invoke();

        Debug.Log("[RewindTimeManager] 回溯已暂停（空格已松开）");
    }

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

        // 恢复回溯速度
        Time.timeScale = _rewindSpeed;

        OnRewindResumed?.Invoke();

        Debug.Log("[RewindTimeManager] 恢复回溯");
    }

    public void StopRewind()
    {
        if (CurrentState == RewindState.Normal) return;

        RewindState previousState = CurrentState;
        CurrentState = RewindState.Normal;

        // 通知所有可回溯物体
        foreach (var obj in _rewindableObjects)
        {
            if (obj != null)
            {
                obj.SetRewindState(CurrentState);
            }
        }

        // 恢复正常时间
        Time.timeScale = 1f;

        // 重置回溯进度
        _rewindTimer = 0f;
        RewindProgress = 0f;

        OnRewindStopped?.Invoke();

        Debug.Log($"[RewindTimeManager] 停止回溯，之前状态: {previousState}");
    }

    private void UpdateVisualEffects()
    {
        if (_mainCamera == null) return;

        Color targetColor = CurrentState != RewindState.Normal ?
            _rewindCameraColor : _originalCameraColor;

        _mainCamera.backgroundColor = Color.Lerp(
            _mainCamera.backgroundColor,
            targetColor,
            Time.unscaledDeltaTime * _rewindTransitionSpeed
        );

        // 更新回溯进度
        if (CurrentState == RewindState.Rewinding)
        {
            _rewindTimer += Time.unscaledDeltaTime;
            RewindProgress = Mathf.Clamp01(_rewindTimer / Mathf.Max(_maxRewindTime, 0.1f));
        }
    }

    private float CalculateMaxRewindTime()
    {
        float maxTime = 0f;
        foreach (var obj in _rewindableObjects)
        {
            RewindableObject rewindObj = obj as RewindableObject;
            if (rewindObj != null)
            {
                float objMaxTime = rewindObj.GetMaxRewindTime();
                if (objMaxTime > maxTime)
                {
                    maxTime = objMaxTime;
                }
            }
        }
        return maxTime;
    }

    private void FindRewindableObjects()
    {
        _rewindableObjects.Clear();

        var allObjects = FindObjectsOfType<RewindableObject>();
        foreach (var obj in allObjects)
        {
            _rewindableObjects.Add(obj);
        }

        Debug.Log($"[RewindTimeManager] 找到 {_rewindableObjects.Count} 个可回溯物体");
    }

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

    public void RegisterRewindable(IRewindable rewindable)
    {
        if (!_rewindableObjects.Contains(rewindable))
        {
            _rewindableObjects.Add(rewindable);
        }
    }

    public void UnregisterRewindable(IRewindable rewindable)
    {
        _rewindableObjects.Remove(rewindable);
    }
}
