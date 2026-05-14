using UnityEngine;

/// <summary>
/// 玩家移动控制器。
/// WASD俯视角移动，复用已有8方向动画状态机。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("移动配置")]
    [SerializeField] private float _speed = 5f;

    [Header("动画参数名（需与Animator一致）")]
    [SerializeField] private string _moveXParam = "MoveX";
    [SerializeField] private string _moveYParam = "MoveY";
    [SerializeField] private string _isMovingParam = "IsMoving";

    [Header("调试")]
    [SerializeField] private bool _enableLog;

    private Rigidbody2D _rb;
    private Animator _animator;
    private Vector2 _input;
    private Vector2 _lastFacingDir = Vector2.down;

    // 外部可查询当前是否在移动
    public bool IsMoving => _input != Vector2.zero;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();

        // 俯视角：锁定旋转和Z轴
        _rb.gravityScale = 0f;
        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    private void Update()
    {
        _input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        if (_input != Vector2.zero)
            _lastFacingDir = _input.normalized;

        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        if (_input == Vector2.zero)
        {
            _rb.velocity = Vector2.zero;
            return;
        }

        _rb.MovePosition(_rb.position + _input.normalized * _speed * Time.fixedDeltaTime);
    }

    private void UpdateAnimation()
    {
        if (_animator == null) return;

        bool moving = _input != Vector2.zero;
        _animator.SetBool(_isMovingParam, moving);

        // 移动时更新朝向；静止时保持最后朝向（避免Idle方向跳变）
        Vector2 dir = moving ? _input.normalized : _lastFacingDir;
        _animator.SetFloat(_moveXParam, dir.x);
        _animator.SetFloat(_moveYParam, dir.y);
    }

    /// <summary>
    /// 外部调用：临时禁用/启用玩家输入（对话期间使用）。
    /// </summary>
    public void SetInputEnabled(bool enabled)
    {
        this.enabled = enabled;
        if (!enabled)
        {
            _input = Vector2.zero;
            _rb.velocity = Vector2.zero;
            if (_animator != null) _animator.SetBool(_isMovingParam, false);
        }

        if (_enableLog)
            Debug.Log($"[Player] 输入{(enabled ? "启用" : "禁用")}");
    }

    /// <summary>
    /// 获取当前位置（用于保存）。
    /// </summary>
    public Vector2 GetPosition()
    {
        return _rb.position;
    }

    /// <summary>
    /// 加载位置（存档恢复时调用）。
    /// </summary>
    public void LoadPosition(Vector2 position)
    {
        _rb.position = position;
        if (_enableLog)
            Debug.Log($"[Player] 位置已加载：{position}");
    }
}
