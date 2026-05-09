using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// NPC路径巡逻控制器（Timeline备选方案）。
/// 按顺序在Waypoint列表间移动，LoopManager切换周目时可切换路径组。
/// </summary>
public class NpcWaypointController : MonoBehaviour
{
    [System.Serializable]
    public class PatrolRoute
    {
        [Tooltip("对应的周目ID，为空表示所有周目均使用此路径")]
        public string loopId;
        [Tooltip("路径点列表（按顺序移动）")]
        public List<Transform> waypoints = new List<Transform>();
    }

    [Header("路径配置")]
    [SerializeField] private List<PatrolRoute> _routes = new List<PatrolRoute>();
    [SerializeField] private float _speed = 2f;
    [Tooltip("到达路径点后的等待时间（秒）")]
    [SerializeField] private float _waitTime = 1f;
    [Tooltip("是否循环巡逻")]
    [SerializeField] private bool _loop = true;
    [SerializeField] private bool _autoStart = true;

    [Header("动画参数名（需与Animator一致）")]
    [SerializeField] private string _moveXParam = "MoveX";
    [SerializeField] private string _moveYParam = "MoveY";
    [SerializeField] private string _isMovingParam = "IsMoving";

    private Animator _animator;
    private LoopManager _loopManager;
    private Coroutine _patrolCoroutine;
    private PatrolRoute _currentRoute;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    private void Start()
    {
        _loopManager = FindObjectOfType<LoopManager>();
        if (_loopManager != null)
            _loopManager.OnLoopChanged += OnLoopChanged;

        if (_autoStart)
            StartPatrolForCurrentLoop();
    }

    private void OnDestroy()
    {
        if (_loopManager != null)
            _loopManager.OnLoopChanged -= OnLoopChanged;
    }

    private void OnLoopChanged(string loopId)
    {
        StartPatrolForCurrentLoop();
    }

    private void StartPatrolForCurrentLoop()
    {
        string currentLoopId = _loopManager != null ? _loopManager.CurrentLoopId : string.Empty;

        // 优先匹配当前周目，其次找通用路径（loopId为空）
        PatrolRoute route = _routes.Find(r => r.loopId == currentLoopId)
                         ?? _routes.Find(r => string.IsNullOrEmpty(r.loopId));

        if (route == null || route.waypoints.Count == 0)
        {
            StopPatrol();
            return;
        }

        _currentRoute = route;
        StartPatrol();
    }

    public void StartPatrol()
    {
        if (_patrolCoroutine != null)
            StopCoroutine(_patrolCoroutine);
        _patrolCoroutine = StartCoroutine(PatrolRoutine());
    }

    public void StopPatrol()
    {
        if (_patrolCoroutine != null)
        {
            StopCoroutine(_patrolCoroutine);
            _patrolCoroutine = null;
        }
        SetAnimation(false, Vector2.zero);
    }

    private IEnumerator PatrolRoutine()
    {
        if (_currentRoute == null || _currentRoute.waypoints.Count == 0) yield break;

        int index = 0;
        while (true)
        {
            Transform target = _currentRoute.waypoints[index];
            if (target == null) { index = NextIndex(index); continue; }

            // 移动到目标点
            while (Vector2.Distance(transform.position, target.position) > 0.05f)
            {
                Vector2 dir = ((Vector2)target.position - (Vector2)transform.position).normalized;
                transform.position = Vector2.MoveTowards(
                    transform.position, target.position, _speed * Time.deltaTime);
                SetAnimation(true, dir);
                yield return null;
            }

            transform.position = target.position;
            SetAnimation(false, Vector2.zero);
            yield return new WaitForSeconds(_waitTime);

            index = NextIndex(index);
            if (index == 0 && !_loop) yield break;
        }
    }

    private int NextIndex(int current)
    {
        return (current + 1) % _currentRoute.waypoints.Count;
    }

    private void SetAnimation(bool moving, Vector2 dir)
    {
        if (_animator == null) return;
        _animator.SetBool(_isMovingParam, moving);
        if (moving)
        {
            _animator.SetFloat(_moveXParam, dir.x);
            _animator.SetFloat(_moveYParam, dir.y);
        }
    }
}
