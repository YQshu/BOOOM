using System;
using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// 回溯模式总协调器。
/// 管理进入/退出回溯、协调时间控制、玩家约束、线索触发等子系统。
/// </summary>
public class RetrospectManager : Singleton<RetrospectManager>
{
    /// <summary>进入回溯时触发。</summary>
    public event Action OnRetrospectEnter;
    /// <summary>退出回溯时触发。</summary>
    public event Action OnRetrospectExit;

    [Header("子系统引用")]
    [Tooltip("时间控制管理器")]
    [SerializeField] private TimelineRewindManager _rewindManager;
    [Tooltip("进度条UI")]
    [SerializeField] private TimeProgressBar _progressBar;
    [Tooltip("NPC房间追踪器")]
    [SerializeField] private NpcRoomTracker _npcRoomTracker;
    [Tooltip("玩家移动约束")]
    [SerializeField] private PlayerMovementConstraint _playerConstraint;
    [Tooltip("视野遮罩控制器")]
    [SerializeField] private VisionMaskController _visionMask;

    [Header("UI")]
    [Tooltip("回溯模式HUD面板（进度条、控制按钮等）")]
    [SerializeField] private GameObject _retrospectHUD;
    [Tooltip("HUD控制器（自动从_retrospectHUD获取）")]
    private RetrospectHUD _retrospectHUDController;
    [Tooltip("角色选择面板（进入回溯时隐藏）")]
    [SerializeField] private GameObject _selectPanel;

    [Header("配置")]
    [Tooltip("退出回溯的按键")]
    [SerializeField] private KeyCode _exitKey = KeyCode.Escape;
    [Tooltip("暂停/继续播放")]
    [SerializeField] private KeyCode _pauseKey = KeyCode.Space;
    [Tooltip("回溯（按住）")]
    [SerializeField] private KeyCode _rewindKey = KeyCode.LeftArrow;
    [Tooltip("快进（按住）")]
    [SerializeField] private KeyCode _fastForwardKey = KeyCode.RightArrow;
    [Tooltip("快进倍速")]
    [SerializeField] private float _fastForwardSpeed = 3f;

    /// <summary>当前回溯使用的 PlayableDirector。</summary>
    private PlayableDirector _currentDirector;
    /// <summary>当前回溯的NPC Transform。</summary>
    private Transform _currentNpcTransform;
    /// <summary>当前周目ID。</summary>
    private string _currentLoopId;
    /// <summary>是否正在快进中。</summary>
    private bool _isFastForwarding;

    /// <summary>是否处于回溯模式。</summary>
    public bool IsInRetrospect { get; private set; }
    /// <summary>当前绑定的Director。</summary>
    public PlayableDirector CurrentDirector => _currentDirector;

    // ─── 生命周期 ────────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        // 自动查找未赋值的引用
        if (_rewindManager == null)
            _rewindManager = FindObjectOfType<TimelineRewindManager>();
        if (_progressBar == null)
            _progressBar = FindObjectOfType<TimeProgressBar>();

        // 订阅对话结束事件
        if (InkDialogueManager.Instance != null)
            InkDialogueManager.Instance.OnDialogueEnd += ResumeFromDialogue;

        // 初始隐藏HUD
        if (_retrospectHUD != null)
            _retrospectHUD.SetActive(false);
    }

    private void OnDestroy()
    {
        if (InkDialogueManager.Instance != null)
            InkDialogueManager.Instance.OnDialogueEnd -= ResumeFromDialogue;
    }

    private void Update()
    {
        if (!IsInRetrospect) return;

        // 对话进行中不响应快捷键
        if (InkDialogueManager.Instance != null && InkDialogueManager.Instance.IsPlaying)
            return;

        // 退出回溯
        if (Input.GetKeyDown(_exitKey))
        {
            ExitRetrospect();
            return;
        }

        // 暂停/继续
        if (Input.GetKeyDown(_pauseKey))
        {
            TogglePause();
        }

        // 回溯（按住）
        if (Input.GetKeyDown(_rewindKey))
        {
            if (_rewindManager != null)
                _rewindManager.StartRewinding();
        }
        if (Input.GetKeyUp(_rewindKey))
        {
            if (_rewindManager != null)
                _rewindManager.StopRewinding();
        }

        // 快进（按住）
        if (Input.GetKeyDown(_fastForwardKey))
        {
            StartFastForward();
        }
        if (Input.GetKeyUp(_fastForwardKey))
        {
            StopFastForward();
        }
    }

    // ─── 公开接口 ────────────────────────────────────────────

    /// <summary>
    /// 进入回溯模式。
    /// </summary>
    /// <param name="loopId">周目ID</param>
    /// <param name="director">嫌疑人对应的 PlayableDirector</param>
    /// <param name="npcTransform">嫌疑人NPC的Transform（用于房间追踪和视野遮罩）</param>
    /// <param name="characterName">角色显示名称（显示在HUD上）</param>
    public void EnterRetrospect(string loopId, PlayableDirector director, Transform npcTransform, string characterName = "")
    {
        if (IsInRetrospect)
        {
            Debug.LogWarning("[Rewind] 已在回溯模式中，忽略重复进入。");
            return;
        }

        if (director == null)
        {
            Debug.LogError("[Rewind] director 为空，无法进入回溯。");
            return;
        }

        _currentLoopId = loopId;
        _currentDirector = director;
        _currentNpcTransform = npcTransform;
        IsInRetrospect = true;

        // 绑定时间控制
        if (_rewindManager != null)
            _rewindManager.BindDirector(director);
        if (_progressBar != null)
            _progressBar.BindDirector(director);

        // 绑定NPC房间追踪
        if (_npcRoomTracker != null && npcTransform != null)
        {
            _npcRoomTracker.BindNpc(npcTransform);
            _npcRoomTracker.OnNpcRoomChanged += OnNpcRoomChanged;

            // 用初始房间启用玩家约束
            if (_playerConstraint != null && _npcRoomTracker.CurrentRoomBounds != null)
                _playerConstraint.EnableConstraint(_npcRoomTracker.CurrentRoomBounds);
        }

        // 启用视野遮罩跟随NPC
        if (_visionMask != null && npcTransform != null)
        {
            _visionMask.gameObject.SetActive(true);
            _visionMask.SetTarget(npcTransform);
        }

        // 显示回溯HUD，隐藏选择面板
        if (_retrospectHUD != null)
        {
            _retrospectHUD.SetActive(true);
            // 获取HUD控制器并传入角色名
            if (_retrospectHUDController == null)
                _retrospectHUDController = _retrospectHUD.GetComponent<RetrospectHUD>();
            if (_retrospectHUDController != null)
                _retrospectHUDController.SetCharacterName(characterName);
        }
        if (_selectPanel != null) _selectPanel.SetActive(false);

        // 从头播放Timeline
        director.time = 0;
        director.Play();

        OnRetrospectEnter?.Invoke();
        Debug.Log($"[Rewind] 进入回溯：{loopId}");
    }

    /// <summary>
    /// 退出回溯模式。
    /// </summary>
    public void ExitRetrospect()
    {
        if (!IsInRetrospect) return;

        // 停止Timeline
        if (_currentDirector != null)
            _currentDirector.Stop();

        // 解除子系统绑定
        if (_npcRoomTracker != null)
        {
            _npcRoomTracker.OnNpcRoomChanged -= OnNpcRoomChanged;
            _npcRoomTracker.Unbind();
        }
        if (_playerConstraint != null)
            _playerConstraint.DisableConstraint();
        if (_visionMask != null)
            _visionMask.gameObject.SetActive(false);

        // 隐藏回溯HUD
        if (_retrospectHUD != null) _retrospectHUD.SetActive(false);

        // 恢复玩家移动
        PlayerController player = FindObjectOfType<PlayerController>();
        player?.SetInputEnabled(true);

        IsInRetrospect = false;
        _currentDirector = null;
        _currentNpcTransform = null;

        OnRetrospectExit?.Invoke();
        Debug.Log("[Rewind] 退出回溯。");
    }

    /// <summary>
    /// 对话开始时暂停Timeline（由InkDialogueManager或ClueSignalReceiver调用）。
    /// </summary>
    public void PauseForDialogue()
    {
        if (!IsInRetrospect) return;

        if (_rewindManager != null)
            _rewindManager.Pause();

        // 禁用进度条拖动，防止对话中跳转时间
        if (_progressBar != null)
            _progressBar.SetInteractable(false);

        Debug.Log("[Rewind] 对话开始，Timeline 暂停。");
    }

    /// <summary>
    /// 对话结束时恢复Timeline播放。
    /// </summary>
    public void ResumeFromDialogue()
    {
        if (!IsInRetrospect) return;

        if (_rewindManager != null)
            _rewindManager.Resume();

        // 恢复进度条交互
        if (_progressBar != null)
            _progressBar.SetInteractable(true);

        Debug.Log("[Rewind] 对话结束，Timeline 恢复。");
    }

    // ─── 内部回调 ────────────────────────────────────────────

    /// <summary>
    /// 切换暂停/播放状态。
    /// </summary>
    private void TogglePause()
    {
        if (_currentDirector == null) return;

        if (_currentDirector.state == PlayState.Playing)
        {
            if (_rewindManager != null) _rewindManager.Pause();
            else _currentDirector.Pause();
            Debug.Log("[Rewind] 暂停。");
        }
        else
        {
            if (_rewindManager != null) _rewindManager.Resume();
            else _currentDirector.Resume();
            Debug.Log("[Rewind] 继续播放。");
        }
    }

    /// <summary>
    /// 开始快进。
    /// </summary>
    private void StartFastForward()
    {
        if (_currentDirector == null) return;

        _isFastForwarding = true;

        // 确保正在播放
        if (_currentDirector.state != PlayState.Playing)
        {
            if (_rewindManager != null) _rewindManager.Resume();
            else _currentDirector.Resume();
        }

        // 加速播放
        if (_currentDirector.playableGraph.IsValid())
            _currentDirector.playableGraph.GetRootPlayable(0).SetSpeed(_fastForwardSpeed);

        Debug.Log($"[Rewind] 快进 x{_fastForwardSpeed}");
    }

    /// <summary>
    /// 停止快进，恢复正常速度。
    /// </summary>
    private void StopFastForward()
    {
        if (_currentDirector == null || !_isFastForwarding) return;

        _isFastForwarding = false;

        if (_currentDirector.playableGraph.IsValid())
            _currentDirector.playableGraph.GetRootPlayable(0).SetSpeed(1.0);

        Debug.Log("[Rewind] 恢复正常速度。");
    }

    /// <summary>
    /// NPC切换房间时更新玩家移动约束区域。
    /// </summary>
    private void OnNpcRoomChanged(Collider2D newRoomBounds)
    {
        if (_playerConstraint != null)
            _playerConstraint.UpdateBounds(newRoomBounds);
    }
}
