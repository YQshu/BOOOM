using UnityEngine;

public class TestMove : MonoBehaviour
{
    [Header("移动设置")]
    [SerializeField] private Vector2 _direction = Vector2.right;
    [SerializeField] private float _speed = 2f;
    [SerializeField] private float _moveDuration = 5f;  // 移动总时长

    [Header("调试")]
    [SerializeField] private bool _showDebug = true;

    private float _elapsedTime = 0f;
    private float _remainingTime = 0f;
    private bool _isMoving = true;
    private Vector3 _startPosition;

    public float GetRemainingTime()
    {
        return Mathf.Max(0, _moveDuration - _elapsedTime);
    }

    public void SetRemainingTime(float time)
    {
        _elapsedTime = _moveDuration - Mathf.Clamp(time, 0, _moveDuration);
        _remainingTime = time;
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

        // 计算帧移动量
        float moveAmount = _speed * Time.deltaTime;
        transform.Translate(_direction * moveAmount);

        // 更新时间
        _elapsedTime += Time.deltaTime;
        _remainingTime = Mathf.Max(0, _moveDuration - _elapsedTime);

        // 移动结束
        if (_remainingTime <= 0)
        {
            _isMoving = false;
            if (_showDebug)
            {
                Debug.Log($"[TestMove] {name} 移动完成! 位置: {transform.position}");
            }
        }
    }

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
