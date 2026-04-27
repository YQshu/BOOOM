using UnityEngine;

/// <summary>
/// 玩家二维俯视移动控制器，强绑定Rigidbody2D组件。
/// 负责读取输入、驱动 Rigidbody2D 移动，并同步动画参数。
/// 支持时间回溯系统。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2D : MonoBehaviour, IRewindable
{
    [Header("移动参数")]
    [Tooltip("角色移动速度，单位为单位/秒")]
    [SerializeField] private float _moveSpeed = 5f;

    [Header("动画")]
    [SerializeField] private Animator _animator;

    [Header("回溯控制")]
    [Tooltip("是否在回溯时禁用玩家控制")]
    [SerializeField] private bool _disableControlDuringRewind = true;
    [Tooltip("回溯时是否让玩家呈现特殊颜色")]
    [SerializeField] private bool _showRewindEffect = true;
    [SerializeField] private Color _rewindColor = new Color(0.7f, 0.5f, 0.8f, 1f);

    [Header("运行开关")]
    [SerializeField] private bool _enableMovement = true;

    [Header("状态显示")]
    [SerializeField] private bool _showStateDebug = true;

    // 组件引用
    private Rigidbody2D _rigidbody2D;
    private SpriteRenderer _spriteRenderer;
    private Color _originalColor;

    // 状态
    private Vector2 _moveInput;
    private RewindState _currentState = RewindState.Normal;
    private float _stateChangeTime = 0f;
    private float _lastDebugTime = 0f;
    private const float DebugInterval = 1f;
    private Vector2 _rawInput;
    private Vector2 _lastMoveDirection = Vector2.down;

    private static readonly int HorizontalHash = Animator.StringToHash("Horizontal");
    private static readonly int VerticalHash = Animator.StringToHash("Vertical");
    private static readonly int LastHorizontalHash = Animator.StringToHash("LastHorizontal");
    private static readonly int LastVerticalHash = Animator.StringToHash("LastVertical");

    private void Awake()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_animator == null)
        {
            _animator = GetComponentInChildren<Animator>();
        }

        if (_spriteRenderer != null)
        {
            _originalColor = _spriteRenderer.color;
        }
    }

    private void Start()
    {
        // 注册到回溯管理器
        if (RewindTimeManager.Instance != null)
        {
            RewindTimeManager.Instance.RegisterRewindable(this);
        }

        Debug.Log($"[PlayerController2D] {name} 已初始化");
    }

    private void OnDestroy()
    {
        if (RewindTimeManager.Instance != null)
        {
            RewindTimeManager.Instance.UnregisterRewindable(this);
        }
    }

    private void Update()
    {
        // 根据状态处理输入
        HandleStateLogic();

        // 状态调试信息
        if (_showStateDebug)
        {
            ShowStateDebug();
        }

        UpdateAnimatorParameters();
    }

    private void FixedUpdate()
    {
        ApplyMovement();
    }

    /// <summary>
    /// 根据当前状态处理逻辑
    /// </summary>
    private void HandleStateLogic()
    {
        switch (_currentState)
        {
            case RewindState.Normal:
                HandleNormalState();
                break;

            case RewindState.Rewinding:
                HandleRewindingState();
                break;

            case RewindState.Paused:
                HandlePausedState();
                break;
        }
    }

    /// <summary>
    /// 正常状态：处理玩家输入
    /// </summary>
    private void HandleNormalState()
    {
        if (!_enableMovement)
        {
            _rawInput = Vector2.zero;
            _moveInput = Vector2.zero;
            return;
        }

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        _rawInput = new Vector2(horizontal, vertical);
        _moveInput = _rawInput.sqrMagnitude > 1f ? _rawInput.normalized : _rawInput;

        if (_rawInput.sqrMagnitude > 0f)
        {
            _lastMoveDirection = _rawInput;
        }

        if (_spriteRenderer != null && _showRewindEffect)
        {
            _spriteRenderer.color = _originalColor;
        }
    }

    /// <summary>
    /// 回溯状态：禁用玩家控制
    /// </summary>
    private void HandleRewindingState()
    {
        if (_disableControlDuringRewind)
        {
            _rawInput = Vector2.zero;
            _moveInput = Vector2.zero;
        }

        // 应用回溯颜色
        if (_spriteRenderer != null && _showRewindEffect)
        {
            _spriteRenderer.color = _rewindColor;
        }

    }

    /// <summary>
    /// 暂停状态：完全停止
    /// </summary>
    private void HandlePausedState()
    {
        _rawInput = Vector2.zero;
        _moveInput = Vector2.zero;

        if (_spriteRenderer != null && _showRewindEffect)
        {
            _spriteRenderer.color = _rewindColor;
        }
    }

    /// <summary>
    /// 同步动画控制器参数。
    /// </summary>
    private void UpdateAnimatorParameters()
    {
        if (_animator == null)
        {
            return;
        }

        _animator.SetFloat(HorizontalHash, _rawInput.x);
        _animator.SetFloat(VerticalHash, _rawInput.y);
        _animator.SetFloat(LastHorizontalHash, _lastMoveDirection.x);
        _animator.SetFloat(LastVerticalHash, _lastMoveDirection.y);
    }
    /// <summary>
    /// 将输入向量转换为刚体速度
    /// </summary>
    private void ApplyMovement()
    {
        // 如果在回溯状态且禁用了控制，强制停止
        if (_disableControlDuringRewind &&
            (_currentState == RewindState.Rewinding ||
             _currentState == RewindState.Paused))
        {
            _rigidbody2D.velocity = Vector2.zero;
            _rigidbody2D.angularVelocity = 0f;
        }
        else
        {
            _rigidbody2D.velocity = _moveInput * _moveSpeed;
        }
    }

    /// <summary>
    /// 设置回溯状态（实现IRewindable接口）
    /// </summary>
    public void SetRewindState(RewindState newState)
    {
        if (_currentState == newState) return;

        RewindState oldState = _currentState;
        _currentState = newState;
        _stateChangeTime = Time.time;

        Debug.Log($"[PlayerController2D] {name} 状态变更: {oldState} -> {newState}");

        // 状态变更时立即停止移动
        if (_disableControlDuringRewind &&
            (newState == RewindState.Rewinding ||
             newState == RewindState.Paused))
        {
            _moveInput = Vector2.zero;
            _rigidbody2D.velocity = Vector2.zero;
            _rigidbody2D.angularVelocity = 0f;
        }
    }

    /// <summary>
    /// 清空历史（实现IRewindable接口）
    /// </summary>
    public void ClearHistory()
    {
        // 玩家控制器通常不需要清空历史，但需要实现接口
        Debug.Log($"[PlayerController2D] {name} 历史已清空");
    }

    /// <summary>
    /// 显示状态调试信息
    /// </summary>
    private void ShowStateDebug()
    {
        float currentTime = Time.time;
        if (currentTime - _lastDebugTime >= DebugInterval)
        {
            _lastDebugTime = currentTime;

            float timeInState = currentTime - _stateChangeTime;
            string stateStr = _currentState.ToString();
            string inputStr = $"输入: ({_moveInput.x:F2}, {_moveInput.y:F2})";
            string velocityStr = $"速度: ({_rigidbody2D.velocity.x:F2}, {_rigidbody2D.velocity.y:F2})";

            Debug.Log($"[PlayerController2D] {name} | 状态: {stateStr} ({timeInState:F1}s) | {inputStr} | {velocityStr}");
        }
    }

    /// <summary>
    /// 临时方法：强制设置移动输入（用于调试）
    /// </summary>
    public void SetMoveInput(Vector2 input)
    {
        _moveInput = input.normalized;
    }

    /// <summary>
    /// 临时方法：获取当前状态
    /// </summary>
    public RewindState GetCurrentState()
    {
        return _currentState;
    }
}
