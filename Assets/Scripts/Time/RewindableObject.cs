using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(Rigidbody2D))]
public class RewindableObject : MonoBehaviour, RewindTimeManager.IRewindable
{
    [System.Serializable]
    public class Snapshot
    {
        public float timestamp;      // 记录时间
        public Vector2 position;     // 位置
        public float rotation;       // 旋转
        public Vector2 velocity;     // 速度
        public float angularVelocity;// 角速度
        public float remainingTime;  // TestMove剩余移动时间

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
    [SerializeField] private float _maxRecordTime = 5f;  // 最多记录5秒
    [SerializeField] private float _recordInterval = 0.02f;  // 50次/秒

    [Header("组件引用")]
    [SerializeField] private Rigidbody2D _rb;

    [Header("调试")]
    [SerializeField] private bool _showDebug = true;
    [SerializeField] private Color _debugColor = Color.cyan;
    [SerializeField] private float _pointSize = 0.1f;

    private LinkedList<Snapshot> _snapshots = new LinkedList<Snapshot>();
    private float _recordTimer = 0f;
    private RewindTimeManager.RewindState _currentState = RewindTimeManager.RewindState.Normal;
    private TestMove _mover;
    private Vector2[] _debugPositions = new Vector2[0];
    private float _lastRecordTime = 0f;
    private float _timeInCurrentState = 0f;

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
        // 记录初始状态
        RecordSnapshot();
        _lastRecordTime = Time.unscaledTime;
    }

    private void Update()
    {
        UpdateState();
        UpdateDebugInfo();
    }

    private void UpdateState()
    {
        switch (_currentState)
        {
            case RewindTimeManager.RewindState.Normal:
                HandleNormalState();
                break;

            case RewindTimeManager.RewindState.Rewinding:
                HandleRewindingState();
                break;

            case RewindTimeManager.RewindState.Paused:
                HandlePausedState();
                break;
        }
    }

    private void HandleNormalState()
    {
        // 正常状态：记录快照
        _recordTimer += Time.unscaledDeltaTime;
        if (_recordTimer >= _recordInterval)
        {
            RecordSnapshot();
            _recordTimer = 0f;
        }

        // 启用物理
        if (_rb != null)
        {
            _rb.simulated = true;
        }

        // 启用移动
        if (_mover != null)
        {
            _mover.enabled = true;
        }
    }

    private void HandleRewindingState()
    {
        // 回溯状态：应用历史快照
        if (_snapshots.Count <= 1)
        {
            Debug.LogWarning($"[RewindableObject] {name} 没有足够的历史数据");
            return;
        }

        // 移除最新快照，应用前一个
        _snapshots.RemoveLast();

        if (_snapshots.Count > 0)
        {
            Snapshot snapshot = _snapshots.Last.Value;
            snapshot.Apply(transform, _rb, _mover);
        }

        // 禁用物理（完全由我们控制）
        if (_rb != null)
        {
            _rb.simulated = false;
        }

        // 禁用移动脚本
        if (_mover != null)
        {
            _mover.enabled = false;
        }
    }

    private void HandlePausedState()
    {
        // 暂停状态：保持当前位置，什么都不做
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

        // 限制历史长度
        while (_snapshots.Count > 0 &&
               _snapshots.Last.Value.timestamp - _snapshots.First.Value.timestamp > _maxRecordTime)
        {
            _snapshots.RemoveFirst();
        }
    }

    public void SetRewindState(RewindTimeManager.RewindState newState)
    {
        if (_currentState == newState) return;

        RewindTimeManager.RewindState oldState = _currentState;
        _currentState = newState;
        _timeInCurrentState = 0f;

        Debug.Log($"[RewindableObject] {name} 状态变更: {oldState} -> {newState}");
    }

    public void ClearHistory()
    {
        _snapshots.Clear();
        RecordSnapshot(); // 记录当前状态
    }

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

    private void OnDrawGizmos()
    {
        if (!_showDebug || _snapshots == null || _snapshots.Count == 0) return;

        Gizmos.color = _debugColor;

        // 绘制轨迹点
        foreach (var snapshot in _snapshots)
        {
            Gizmos.DrawWireSphere(snapshot.position, _pointSize);
        }

        // 绘制轨迹线
        Vector2[] positions = _snapshots.Select(s => s.position).ToArray();
        for (int i = 1; i < positions.Length; i++)
        {
            Gizmos.DrawLine(positions[i - 1], positions[i]);
        }
    }
}
