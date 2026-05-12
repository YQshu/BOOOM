using UnityEngine;

/// <summary>
/// 盲区解锁区域控制器。
/// 根据"当前回溯的嫌疑人ID"与"已收集线索"决定区域可见性和可进入状态。
/// 进入/退出回溯时自动刷新状态。
/// </summary>
public class MemoryLockZone : MonoBehaviour
{
    [Header("解锁条件")]
    [Tooltip("允许该区域显示/解锁的嫌疑人ID（与 SuspectEntry.suspectId 一致），为空表示不限制")]
    [SerializeField] private string _requiredSuspectId = "";
    [Tooltip("解锁该区域所需线索ID，为空表示不需要线索")]
    [SerializeField] private string _requiredClueId = "";

    [Header("引用对象")]
    [Tooltip("用于控制区域可见性的渲染器，不指定则自动获取子物体中的 SpriteRenderer")]
    [SerializeField] private SpriteRenderer _targetRenderer;
    [Tooltip("用于控制区域可通行性的碰撞器，不指定则自动获取当前物体上的 Collider2D")]
    [SerializeField] private Collider2D _targetCollider;

    [Header("状态配置")]
    [Tooltip("锁定状态下渲染器是否可见")]
    [SerializeField] private bool _rendererVisibleWhenLocked;
    [Tooltip("锁定状态下碰撞器是否启用")]
    [SerializeField] private bool _colliderEnabledWhenLocked = true;

    [Header("调试选项")]
    [SerializeField] private bool _enableLog = true;

    private void Awake()
    {
        if (_targetRenderer == null)
            _targetRenderer = GetComponentInChildren<SpriteRenderer>();
        if (_targetCollider == null)
            _targetCollider = GetComponent<Collider2D>();
    }

    private void Start()
    {
        if (RetrospectManager.Instance != null)
        {
            RetrospectManager.Instance.OnRetrospectEnter += RefreshState;
            RetrospectManager.Instance.OnRetrospectExit += RefreshState;
        }
        RefreshState();
    }

    private void OnDestroy()
    {
        if (RetrospectManager.Instance != null)
        {
            RetrospectManager.Instance.OnRetrospectEnter -= RefreshState;
            RetrospectManager.Instance.OnRetrospectExit -= RefreshState;
        }
    }

    /// <summary>
    /// 手动刷新当前区域锁定状态。
    /// </summary>
    public void RefreshState()
    {
        bool isUnlocked = CheckUnlocked();
        ApplyState(isUnlocked);
    }

    private bool CheckUnlocked()
    {
        // 检查嫌疑人ID条件
        if (!string.IsNullOrWhiteSpace(_requiredSuspectId))
        {
            string currentId = RetrospectManager.Instance != null
                ? RetrospectManager.Instance.CurrentLoopId
                : string.Empty;
            if (currentId != _requiredSuspectId) return false;
        }

        // 检查线索条件
        if (!string.IsNullOrWhiteSpace(_requiredClueId))
        {
            if (ClueManager.Instance == null || !ClueManager.Instance.HasClue(_requiredClueId))
                return false;
        }

        return true;
    }

    private void ApplyState(bool isUnlocked)
    {
        if (_targetRenderer != null)
            _targetRenderer.enabled = isUnlocked || _rendererVisibleWhenLocked;
        if (_targetCollider != null)
            _targetCollider.enabled = isUnlocked || _colliderEnabledWhenLocked;

        if (_enableLog)
            Debug.Log($"[MemoryLock] {gameObject.name} 状态：{(isUnlocked ? "已解锁" : "锁定中")}", this);
    }
}
