using UnityEngine;

/// <summary>
/// 玩家移动约束。
/// 将玩家限制在指定的Collider2D区域内（回溯模式下限制在NPC所在房间）。
/// 挂载在Player GameObject上。
/// </summary>
public class PlayerMovementConstraint : MonoBehaviour
{
    [Header("配置")]
    [Tooltip("越界时将玩家拉回边界的缓冲距离")]
    [SerializeField] private float _boundaryBuffer = 0.1f;

    /// <summary>是否正在约束中。</summary>
    public bool IsConstrained { get; private set; }

    private Collider2D _currentBounds;
    private Rigidbody2D _playerRb;

    private void Awake()
    {
        _playerRb = GetComponent<Rigidbody2D>();
    }

    // ─── 公开接口 ────────────────────────────────────────────

    /// <summary>
    /// 启用约束，传入允许移动的区域Collider2D。
    /// </summary>
    public void EnableConstraint(Collider2D bounds)
    {
        _currentBounds = bounds;
        IsConstrained = bounds != null;

        if (IsConstrained)
            Debug.Log("[Rewind] 玩家移动约束已启用。");
    }

    /// <summary>
    /// 更新约束区域（NPC换房间时调用）。
    /// </summary>
    public void UpdateBounds(Collider2D newBounds)
    {
        if (newBounds == null) return;

        _currentBounds = newBounds;

        // 若玩家不在新区域内，传送到区域中心
        if (_playerRb != null && !newBounds.OverlapPoint(_playerRb.position))
        {
            _playerRb.position = newBounds.bounds.center;
            Debug.Log("[Rewind] 玩家传送至新房间。");
        }
    }

    /// <summary>
    /// 禁用约束。
    /// </summary>
    public void DisableConstraint()
    {
        IsConstrained = false;
        _currentBounds = null;
        Debug.Log("[Rewind] 玩家移动约束已解除。");
    }

    // ─── 内部逻辑 ────────────────────────────────────────────

    private void LateUpdate()
    {
        if (!IsConstrained || _currentBounds == null || _playerRb == null) return;

        Vector2 playerPos = _playerRb.position;

        // 检测玩家是否在区域内
        if (!_currentBounds.OverlapPoint(playerPos))
        {
            // 找到最近的合法点，将玩家拉回
            Vector2 closestPoint = _currentBounds.ClosestPoint(playerPos);
            _playerRb.position = closestPoint;
        }
    }
}
