using UnityEngine;

/// <summary>
/// 玩家二维俯视移动控制器，强绑定Rigidbody2D组件。
/// 负责读取输入、驱动 Rigidbody2D 移动，并处理角色旋转。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2D : MonoBehaviour
{
    [Header("移动参数")]
    [Tooltip("角色移动速度，单位为单位/秒")]
    [SerializeField] private float _moveSpeed = 5f;
    [Tooltip("角色旋转速度，单位为度/秒")]
    [SerializeField] private float _rotateSpeed = 200f;

    [Header("输入按键")]
    [SerializeField] private KeyCode _rotateLeftKey = KeyCode.Q;
    [SerializeField] private KeyCode _rotateRightKey = KeyCode.E;

    [Header("运行开关")]
    [SerializeField] private bool _enableMovement = true;
    [SerializeField] private bool _enableRotation = true;

    private Rigidbody2D _rigidbody2D;
    private Vector2 _moveInput;

    private void Awake()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        ReadMovementInput();
        HandleRotation();
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
            _moveInput = Vector2.zero;
            return;
        }

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        _moveInput = new Vector2(horizontal, vertical).normalized;
    }

    /// <summary>
    /// 处理角色左右旋转输入。
    /// </summary>
    private void HandleRotation()
    {
        if (!_enableRotation)
        {
            return;
        }

        if (Input.GetKey(_rotateLeftKey))
        {
            transform.Rotate(0f, 0f, _rotateSpeed * Time.deltaTime);
        }

        if (Input.GetKey(_rotateRightKey))
        {
            transform.Rotate(0f, 0f, -_rotateSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// 将输入向量转换为刚体速度。
    /// </summary>
    private void ApplyMovement()
    {
        _rigidbody2D.velocity = _moveInput * _moveSpeed;
    }
}
