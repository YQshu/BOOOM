using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 可回溯对象组件，负责记录历史快照并在回溯时还原状态。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class RewindableObject : MonoBehaviour, IRewindable
{
    /// <summary>
    /// 对象状态快照，用于回溯时恢复位置、旋转与运动信息。
    /// </summary>
    [System.Serializable]
    public class Snapshot
    {
        public float timestamp;
        public Vector2 position;
        public float rotation;
        public Vector2 velocity;
        public float angularVelocity;
        public float remainingTime;

        /// <summary>
        /// 构造快照并记录当前对象状态。
        /// </summary>
        /// <param name="time">记录时间戳。</param>
        /// <param name="transform">目标 Transform。</param>
        /// <param name="rb">目标 Rigidbody2D。</param>
        /// <param name="mover">可选移动脚本。</param>
        public Snapshot(float time, Transform transform, Rigidbody2D rb, TestMove mover = null)
        {
            timestamp = time;
            position = transform.position;
            rotation = transform.eulerAngles.z;

            if (rb != null)
            {
                velocity = rb.velocity;
                angularVelocity = rb.angularVelocity;
            }

            if (mover != null)
            {
                remainingTime = mover.GetRemainingTime();
            }
        }

        /// <summary>
        /// 将快照状态应用到目标对象。
        /// </summary>
        /// <param name="transform">目标 Transform。</param>
        /// <param name="rb">目标 Rigidbody2D。</param>
        /// <param name="mover">可选移动脚本。</param>
        public void Apply(Transform transform, Rigidbody2D rb, TestMove mover = null)
        {
            transform.position = position;
            Vector3 euler = transform.eulerAngles;
            euler.z = rotation;
            transform.eulerAngles = euler;

            if (rb != null)
            {
                rb.velocity = velocity;
                rb.angularVelocity = angularVelocity;
            }

            if (mover != null)
            {
                mover.SetRemainingTime(remainingTime);
            }
        }
    }

    [Header("记录设置")]
    [Tooltip("最多保留的历史记录时长（秒）")]
    [SerializeField] private float _maxRecordTime = 5f;
    [Tooltip("记录快照的时间间隔（秒）")]
    [SerializeField] private float _recordInterval = 0.02f;

    [Header("组件引用")]
    [Tooltip("目标刚体组件，为空时自动获取")]
    [SerializeField] private Rigidbody2D _rb;

    [Header("调试")]
    [Tooltip("是否启用调试输出与轨迹显示")]
    [SerializeField] private bool _showDebug = true;
    [Tooltip("轨迹 Gizmos 颜色")]
    [SerializeField] private Color _debugColor = Color.cyan;
    [Tooltip("轨迹点尺寸")]
    [SerializeField] private float _pointSize = 0.1f;

    private readonly LinkedList<Snapshot> _snapshots = new LinkedList<Snapshot>();
    private float _recordTimer = 0f;
    private RewindState _currentState = RewindState.Normal;
    private TestMove _mover;
    private float _lastRecordTime = 0f;
    private float _timeInCurrentState = 0f;

    /// <summary>
    /// 获取当前可回溯时长。
    /// </summary>
    /// <returns>可回溯秒数。</returns>
    public float GetMaxRewindTime()
    {
        if (_snapshots.Count == 0) return 0f;
        return Mathf.Min(_maxRecordTime, _snapshots.Last.Value.timestamp - _snapshots.First.Value.timestamp);
    }

    private void Awake()
    {
        if (_rb == null) _rb = GetComponent<Rigidbody2D>();
        _mover = GetComponent<TestMove>();
    }

    private void Start()
    {
        RecordSnapshot();
        _lastRecordTime = Time.unscaledTime;
    }

    private void Update()
    {
        UpdateState();
        UpdateDebugInfo();
    }

    /// <summary>
    /// 更新当前状态对应的逻辑。
    /// </summary>
    private void UpdateState()
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
    /// 正常状态：按间隔记录快照并启用运动。
    /// </summary>
    private void HandleNormalState()
    {
        _recordTimer += Time.unscaledDeltaTime;
        if (_recordTimer >= _recordInterval)
        {
            RecordSnapshot();
            _recordTimer = 0f;
        }

        if (_rb != null)
        {
            _rb.simulated = true;
        }

        if (_mover != null)
        {
            _mover.enabled = true;
        }
    }

    /// <summary>
    /// 回溯状态：按历史快照倒退并禁用运动。
    /// </summary>
    private void HandleRewindingState()
    {
        if (_snapshots.Count <= 1)
        {
            Debug.LogWarning($"[RewindableObject] {name} 没有足够的历史数据");
            return;
        }

        _snapshots.RemoveLast();

        if (_snapshots.Count > 0)
        {
            Snapshot snapshot = _snapshots.Last.Value;
            snapshot.Apply(transform, _rb, _mover);
        }

        if (_rb != null)
        {
            _rb.simulated = false;
        }

        if (_mover != null)
        {
            _mover.enabled = false;
        }
    }

    /// <summary>
    /// 暂停状态：冻结当前位置与运动。
    /// </summary>
    private void HandlePausedState()
    {
        if (_rb != null)
        {
            _rb.velocity = Vector2.zero;
            _rb.angularVelocity = 0f;
            _rb.simulated = false;
        }

        if (_mover != null)
        {
            _mover.enabled = false;
        }
    }

    /// <summary>
    /// 记录当前对象快照，并按最大时长裁剪历史。
    /// </summary>
    private void RecordSnapshot()
    {
        Snapshot snapshot = new Snapshot(
            Time.unscaledTime,
            transform,
            _rb,
            _mover
        );

        _snapshots.AddLast(snapshot);
        _lastRecordTime = Time.unscaledTime;

        while (_snapshots.Count > 0 &&
               _snapshots.Last.Value.timestamp - _snapshots.First.Value.timestamp > _maxRecordTime)
        {
            _snapshots.RemoveFirst();
        }
    }

    /// <summary>
    /// 设置回溯状态。
    /// </summary>
    /// <param name="newState">新的回溯状态。</param>
    public void SetRewindState(RewindState newState)
    {
        if (_currentState == newState) return;

        RewindState oldState = _currentState;
        _currentState = newState;
        _timeInCurrentState = 0f;

        Debug.Log($"[RewindableObject] {name} 状态变更: {oldState} -> {newState}");
    }

    /// <summary>
    /// 清空历史并记录当前状态作为新起点。
    /// </summary>
    public void ClearHistory()
    {
        _snapshots.Clear();
        RecordSnapshot();
    }

    /// <summary>
    /// 输出调试信息。
    /// </summary>
    private void UpdateDebugInfo()
    {
        if (!_showDebug) return;

        _timeInCurrentState += Time.deltaTime;

        if (_timeInCurrentState >= 1f)
        {
            _timeInCurrentState = 0f;

            int snapshotCount = _snapshots.Count;
            float historyDuration = snapshotCount > 1 ?
                _snapshots.Last.Value.timestamp - _snapshots.First.Value.timestamp : 0f;

            string moverInfo = _mover != null ? $" | 移动脚本: {_mover.enabled}" : "";

            Debug.Log($"[RewindableObject] {name} | 状态: {_currentState} | " +
                     $"快照: {snapshotCount} | 时长: {historyDuration:F2}s{moverInfo}");
        }
    }

    /// <summary>
    /// 绘制快照轨迹调试 Gizmos。
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!_showDebug || _snapshots == null || _snapshots.Count == 0) return;

        Gizmos.color = _debugColor;

        foreach (var snapshot in _snapshots)
        {
            Gizmos.DrawWireSphere(snapshot.position, _pointSize);
        }

        Vector2[] positions = _snapshots.Select(s => s.position).ToArray();
        for (int i = 1; i < positions.Length; i++)
        {
            Gizmos.DrawLine(positions[i - 1], positions[i]);
        }
    }
}
