using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// NPC路径巡逻控制器（主角线NPC在非回溯状态下的备选驱动方案）。
/// 按顺序在Waypoint列表间循环移动。
/// </summary>
public class NpcWaypointController : MonoBehaviour
{
    [Header("路径配置")]
    [Tooltip("路径点列表（按顺序移动）")]
    [SerializeField] private List<Transform> _waypoints = new List<Transform>();
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
    private Coroutine _patrolCoroutine;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    private void Start()
    {
        if (_autoStart) StartPatrol();
    }

    public void StartPatrol()
    {
        if (_waypoints.Count == 0) return;
        if (_patrolCoroutine != null) StopCoroutine(_patrolCoroutine);
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
        int index = 0;
        while (true)
        {
            Transform target = _waypoints[index];
            if (target == null) { index = NextIndex(index); continue; }

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

    private int NextIndex(int current) => (current + 1) % _waypoints.Count;

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
