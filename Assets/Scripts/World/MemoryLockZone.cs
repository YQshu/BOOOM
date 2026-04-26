using UnityEngine;

/// <summary>
/// 盲区解锁区域控制器。
/// 负责根据周目与线索条件决定区域可见性和可进入状态。
/// </summary>
public class MemoryLockZone : MonoBehaviour
{
    [Header("解锁条件")]
    [Tooltip("允许该区域显示/解锁的周目ID，为空表示不限制周目")]
    [SerializeField] private string _requiredLoopId = "LOOP_SUSPECT_B";
    [Tooltip("解锁该区域所需线索ID")]
    [SerializeField] private string _requiredClueId = "CLUE_DEMO_01";

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
    [Tooltip("是否输出状态变化日志")]
    [SerializeField] private bool _enableLog = true;

    private LoopManager _loopManager;

    private void Awake()
    {
        CacheReferences();
    }

    private void Start()
    {
        _loopManager = FindObjectOfType<LoopManager>();
        if (_loopManager != null)
        {
            _loopManager.OnLoopChanged += HandleLoopChanged;
        }

        RefreshState();
    }

    private void OnDestroy()
    {
        if (_loopManager != null)
        {
            _loopManager.OnLoopChanged -= HandleLoopChanged;
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

    /// <summary>
    /// 处理周目切换事件并刷新状态。
    /// </summary>
    /// <param name="loopId">切换后的周目ID。</param>
    private void HandleLoopChanged(string loopId)
    {
        RefreshState();
    }

    /// <summary>
    /// 校验当前是否满足解锁条件。
    /// </summary>
    /// <returns>满足条件返回 true，否则返回 false。</returns>
    private bool CheckUnlocked()
    {
        bool loopMatched = IsLoopMatched();
        bool clueMatched = IsClueMatched();
        return loopMatched && clueMatched;
    }

    /// <summary>
    /// 判断当前周目是否满足条件。
    /// </summary>
    /// <returns>满足返回 true，不满足返回 false。</returns>
    private bool IsLoopMatched()
    {
        if (string.IsNullOrWhiteSpace(_requiredLoopId))
        {
            return true;
        }

        if (_loopManager == null)
        {
            return false;
        }

        return _loopManager.CurrentLoopId == _requiredLoopId;
    }

    /// <summary>
    /// 判断线索条件是否满足。
    /// </summary>
    /// <returns>满足返回 true，不满足返回 false。</returns>
    private bool IsClueMatched()
    {
        if (string.IsNullOrWhiteSpace(_requiredClueId))
        {
            return true;
        }

        if (ClueManager.Instance == null)
        {
            return false;
        }

        return ClueManager.Instance.HasClue(_requiredClueId);
    }

    /// <summary>
    /// 应用锁定或解锁状态到渲染与碰撞组件。
    /// </summary>
    /// <param name="isUnlocked">是否为解锁状态。</param>
    private void ApplyState(bool isUnlocked)
    {
        if (_targetRenderer != null)
        {
            _targetRenderer.enabled = isUnlocked || _rendererVisibleWhenLocked;
        }

        if (_targetCollider != null)
        {
            _targetCollider.enabled = isUnlocked || _colliderEnabledWhenLocked;
        }

        if (_enableLog)
        {
            string stateText = isUnlocked ? "已解锁" : "锁定中";
            Debug.Log($"[MemoryLock] 区域状态：{stateText}", this);
        }
    }

    /// <summary>
    /// 缓存区域控制所需组件引用。
    /// </summary>
    private void CacheReferences()
    {
        if (_targetRenderer == null)
        {
            _targetRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (_targetCollider == null)
        {
            _targetCollider = GetComponent<Collider2D>();
        }
    }
}
