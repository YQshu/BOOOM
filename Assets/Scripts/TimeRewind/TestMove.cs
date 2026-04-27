using UnityEngine;

/// <summary>
/// 测试移动脚本，按固定方向和时长移动物体，供回溯系统验证使用。
/// </summary>
public class TestMove : MonoBehaviour
{
    [Header("移动设置")]
    [Tooltip("移动方向")]
    [SerializeField] private Vector2 _direction = Vector2.right;
    [Tooltip("移动速度")]
    [SerializeField] private float _speed = 2f;
    [Tooltip("移动总时长（秒）")]
    [SerializeField] private float _moveDuration = 5f;

    [Header("调试")]
    [Tooltip("是否输出调试日志")]
    [SerializeField] private bool _showDebug = true;

    private float _elapsedTime = 0f;
    private float _remainingTime = 0f;
    private bool _isMoving = true;
    private Vector3 _startPosition;

    /// <summary>
    /// 获取剩余移动时间。
    /// </summary>
    /// <returns>剩余秒数。</returns>
    public float GetRemainingTime()
    {
        return Mathf.Max(0f, _moveDuration - _elapsedTime);
    }

    /// <summary>
    /// 设置剩余移动时间并同步内部计时。
    /// </summary>
    /// <param name="time">剩余时间（秒）。</param>
    public void SetRemainingTime(float time)
    {
        float clampedTime = Mathf.Clamp(time, 0f, _moveDuration);
        _elapsedTime = _moveDuration - clampedTime;
        _remainingTime = clampedTime;
    }

    private void Start()
    {
        _startPosition = transform.position;
        _remainingTime = _moveDuration;

        if (_showDebug)
        {
            Debug.Log($"[TestMove] {name} 开始移动: 方向={_direction}, 速度={_speed}, 时长={_moveDuration}s");
        }
    }

    private void Update()
    {
        if (!_isMoving) return;
        if (_remainingTime <= 0) return;

        float moveAmount = _speed * Time.deltaTime;
        transform.Translate(_direction * moveAmount);

        _elapsedTime += Time.deltaTime;
        _remainingTime = Mathf.Max(0f, _moveDuration - _elapsedTime);

        if (_remainingTime <= 0)
        {
            _isMoving = false;
            if (_showDebug)
            {
                Debug.Log($"[TestMove] {name} 移动完成! 位置: {transform.position}");
            }
        }
    }

    /// <summary>
    /// 重置移动状态并回到初始位置。
    /// </summary>
    public void ResetMovement()
    {
        transform.position = _startPosition;
        _elapsedTime = 0f;
        _remainingTime = _moveDuration;
        _isMoving = true;

        if (_showDebug)
        {
            Debug.Log($"[TestMove] {name} 已重置");
        }
    }

    private void OnEnable()
    {
        _isMoving = true;
    }

    private void OnDisable()
    {
        _isMoving = false;
    }
}
