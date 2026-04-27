using UnityEngine;

/// <summary>
/// 玩家二维俯视移动控制器，强绑定Rigidbody2D组件。
/// 负责读取输入、驱动 Rigidbody2D 移动，并同步动画参数。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2D : MonoBehaviour
{
    [Header("移动参数")]
    [Tooltip("角色移动速度，单位为单位/秒")]
    [SerializeField] private float _moveSpeed = 5f;

    [Header("动画")]
    [SerializeField] private Animator _animator;

    [Header("运行开关")]
    [SerializeField] private bool _enableMovement = true;

    private Rigidbody2D _rigidbody2D;
    private Vector2 _rawInput;
    private Vector2 _moveInput;
    private Vector2 _lastMoveDirection = Vector2.down;

    private static readonly int HorizontalHash = Animator.StringToHash("Horizontal");
    private static readonly int VerticalHash = Animator.StringToHash("Vertical");
    private static readonly int LastHorizontalHash = Animator.StringToHash("LastHorizontal");
    private static readonly int LastVerticalHash = Animator.StringToHash("LastVertical");

    private void Awake()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();
        if (_animator == null)
        {
            _animator = GetComponentInChildren<Animator>();
        }
    }

    private void Update()
    {
        ReadMovementInput();
        UpdateAnimatorParameters();
    }

    private void FixedUpdate()
    {
        ApplyMovement();
    }

    /// <summary>
    /// 读取水平与垂直输入并生成归一化移动向量。
    /// </summary>
    private void ReadMovementInput()
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
    /// 将输入向量转换为刚体速度。
    /// </summary>
    private void ApplyMovement()
    {
        _rigidbody2D.velocity = _moveInput * _moveSpeed;
    }
}
