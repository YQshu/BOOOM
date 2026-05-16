using System;
using System.Collections.Generic;
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
    [Tooltip("全屏暗色遮罩物体（DarkOverlay SpriteRenderer）")]
    [SerializeField] private GameObject _darkOverlay;

    [Header("UI")]
    [Tooltip("回溯模式HUD面板（进度条、控制按钮等），需挂载CanvasGroup，始终保持激活")]
    [SerializeField] private GameObject _retrospectHUD;
    [Tooltip("HUD控制器（自动从_retrospectHUD获取）")]
    private RetrospectHUD _retrospectHUDController;
    [Tooltip("角色选择面板，需挂载CanvasGroup，始终保持激活")]
    [SerializeField] private GameObject _selectPanel;

    [Header("平行世界")]
    [Tooltip("主角线世界根节点（非回溯状态时激活，进入回溯时隐藏）")]
    [SerializeField] private GameObject _mainWorldRoot;
    [Tooltip("记忆损坏遮挡面板父节点（退出回溯时强制禁用所有子面板，防止遮挡视线）\n将所有记忆缺失面板作为子物体放在此节点下")]
    [SerializeField] private Transform _memoryGlitchPanelsParent;

    private CanvasGroup _selectCanvasGroup;
    /// <summary>当前激活的嫌疑人世界根节点（退出时用于关闭）。</summary>
    private GameObject _currentSuspectWorldRoot;

    [Header("配置")]
    [Tooltip("退出回溯的按键")]
    [SerializeField] private KeyCode _exitKey = KeyCode.Escape;
    [Tooltip("暂停/继续播放")]
    [SerializeField] private KeyCode _pauseKey = KeyCode.Space;
    [Tooltip("回溯（按住）")]
    [SerializeField] private KeyCode _rewindKey = KeyCode.Q;
    [Tooltip("快进（按住）")]
    [SerializeField] private KeyCode _fastForwardKey = KeyCode.E;
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
    /// <summary>对话开始前Timeline是否正在播放（决定对话结束后是否恢复）。</summary>
    private bool _wasPlayingBeforeDialogue;

    /// <summary>是否处于回溯模式。</summary>
    public bool IsInRetrospect { get; private set; }
    /// <summary>当前绑定的Director。</summary>
    public PlayableDirector CurrentDirector => _currentDirector;
    /// <summary>当前回溯的嫌疑人ID（对应 SuspectEntry.suspectId）。</summary>
    public string CurrentLoopId => _currentLoopId;

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

        // 初始化 SelectPanel CanvasGroup
        if (_selectPanel != null)
        {
            _selectCanvasGroup = _selectPanel.GetComponent<CanvasGroup>();
            if (_selectCanvasGroup == null)
                _selectCanvasGroup = _selectPanel.AddComponent<CanvasGroup>();
        }

        // 订阅对话结束事件
        if (InkDialogueManager.Instance != null)
            InkDialogueManager.Instance.OnDialogueEnd += ResumeFromDialogue;

        // RetrospectHUD 通过 CanvasGroup 自我管理，无需 SetActive
        if (_retrospectHUD != null)
        {
            if (_retrospectHUDController == null)
                _retrospectHUDController = _retrospectHUD.GetComponent<RetrospectHUD>();
        }
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
    /// <param name="suspectWorldRoot">嫌疑人世界根节点（进入时激活，退出时隐藏）</param>
    public void EnterRetrospect(string loopId, PlayableDirector director, Transform npcTransform,
                                string characterName = "", GameObject suspectWorldRoot = null)
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
        _currentSuspectWorldRoot = suspectWorldRoot;
        IsInRetrospect = true;

        // ── 平行世界切换 ──────────────────────────────────────
        if (_mainWorldRoot != null) _mainWorldRoot.SetActive(false);
        if (_currentSuspectWorldRoot != null) _currentSuspectWorldRoot.SetActive(true);

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
            if (_playerConstraint != null && _npcRoomTracker.CurrentRoomBounds.Count > 0)
                _playerConstraint.EnableConstraint(_npcRoomTracker.CurrentRoomBounds);
        }

        // 启用视野遮罩跟随NPC
        if (_darkOverlay != null) _darkOverlay.SetActive(true);
        if (_visionMask != null && npcTransform != null)
        {
            _visionMask.gameObject.SetActive(true);
            _visionMask.SetTarget(npcTransform);
        }

        // 显示回溯HUD（HUD通过事件订阅自我显示），隐藏选择面板
        if (_retrospectHUDController != null)
            _retrospectHUDController.SetCharacterName(characterName);
        SetSelectVisible(false);

        // 从头播放Timeline（播放前先应用条件Track）
        ApplyConditionalTracks(director);
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

        // 如果正在播放对话，强制停止
        if (InkDialogueManager.Instance != null && InkDialogueManager.Instance.IsPlaying)
        {
            InkDialogueManager.Instance.StopDialogue();
            Debug.Log("[Rewind] 退出回溯时强制停止对话。");
        }

        // 停止Timeline
        if (_currentDirector != null)
        {
            _currentDirector.Stop();

            // 强制禁用所有记忆损坏遮挡组件（保险措施）
            ForceDisableMemoryGlitchOverlays();
        }

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
        if (_darkOverlay != null)
            _darkOverlay.SetActive(false);

        // 隐藏回溯HUD（HUD通过事件订阅自我隐藏）

        // 恢复条件Track原始状态
        RestoreConditionalTracks();

        // ── 平行世界恢复 ──────────────────────────────────────
        if (_currentSuspectWorldRoot != null) _currentSuspectWorldRoot.SetActive(false);
        if (_mainWorldRoot != null) _mainWorldRoot.SetActive(true);

        // 恢复玩家移动
        PlayerController player = FindObjectOfType<PlayerController>();
        player?.SetInputEnabled(true);

        IsInRetrospect = false;
        _currentDirector = null;
        _currentNpcTransform = null;
        _currentSuspectWorldRoot = null;

        OnRetrospectExit?.Invoke();
        Debug.Log("[Rewind] 退出回溯。");
    }

    /// <summary>
    /// 对话开始时暂停Timeline（由InkDialogueManager或ClueSignalReceiver调用）。
    /// </summary>
    public void PauseForDialogue()
    {
        if (!IsInRetrospect) return;

        // 记录对话前的播放状态，对话结束后按原状态决定是否恢复
        _wasPlayingBeforeDialogue = _rewindManager != null && _rewindManager.IsPlaying();

        if (_rewindManager != null)
            _rewindManager.Pause();

        // 禁用进度条拖动，防止对话中跳转时间
        if (_progressBar != null)
            _progressBar.SetInteractable(false);

        Debug.Log($"[Rewind] 对话开始，Timeline 暂停（之前播放中：{_wasPlayingBeforeDialogue}）。");
    }

    /// <summary>
    /// 对话结束时，仅在对话开始前Timeline处于播放状态时才恢复。
    /// </summary>
    public void ResumeFromDialogue()
    {
        if (!IsInRetrospect) return;

        // 只有对话前正在播放的情况才恢复（Signal触发的对话）
        // 玩家主动交互时Timeline可能处于暂停状态，不应强制恢复
        if (_wasPlayingBeforeDialogue && _rewindManager != null)
            _rewindManager.Resume();

        // 恢复进度条交互
        if (_progressBar != null)
            _progressBar.SetInteractable(true);

        Debug.Log($"[Rewind] 对话结束，{(_wasPlayingBeforeDialogue ? "Timeline 恢复播放" : "Timeline 保持暂停")}。");
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
    private void OnNpcRoomChanged(List<Collider2D> newRoomBounds)
    {
        if (_playerConstraint != null)
            _playerConstraint.UpdateBounds(newRoomBounds);
    }

    private void SetSelectVisible(bool visible)
    {
        if (_selectCanvasGroup == null) return;
        _selectCanvasGroup.alpha = visible ? 1f : 0f;
        _selectCanvasGroup.interactable = visible;
        _selectCanvasGroup.blocksRaycasts = visible;
    }

    /// <summary>
    /// 从 director 所在 GameObject 查找 ConditionalTrackController 并应用条件。
    /// </summary>
    private void ApplyConditionalTracks(PlayableDirector director)
    {
        if (director == null) return;
        ConditionalTrackController ctrl = director.GetComponent<ConditionalTrackController>();
        ctrl?.ApplyConditions(director);
    }

    /// <summary>
    /// 从当前 director 所在 GameObject 查找并恢复条件Track状态。
    /// </summary>
    private void RestoreConditionalTracks()
    {
        if (_currentDirector == null) return;
        ConditionalTrackController ctrl = _currentDirector.GetComponent<ConditionalTrackController>();
        ctrl?.RestoreConditions();
    }

    /// <summary>
    /// 强制禁用所有记忆损坏遮挡面板（保险措施）。
    /// 防止在记忆损坏片段播放时退出回溯，导致遮挡面板依旧存活遮挡视线。
    /// </summary>
    private void ForceDisableMemoryGlitchOverlays()
    {
        if (_memoryGlitchPanelsParent == null) return;

        int disabledCount = 0;
        foreach (Transform child in _memoryGlitchPanelsParent)
        {
            if (child.gameObject.activeSelf)
            {
                child.gameObject.SetActive(false);
                disabledCount++;
            }
        }

        if (disabledCount > 0)
        {
            Debug.Log($"[Rewind] 强制禁用了 {disabledCount} 个记忆损坏遮挡面板。");
        }
    }
}
