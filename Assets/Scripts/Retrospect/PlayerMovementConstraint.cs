using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 玩家移动约束。
/// 将玩家限制在指定的Collider2D区域内（回溯模式下限制在NPC所在房间）。
/// 支持NPC处于多个房间交界处时，玩家可以在所有这些房间内移动。
/// 挂载在Player GameObject上。
/// NPC换房间时通过屏幕淡黑过渡代替硬瞬移。
/// </summary>
public class PlayerMovementConstraint : MonoBehaviour
{
    [Header("配置")]
    [Tooltip("越界时将玩家拉回边界的缓冲距离")]
    [SerializeField] private float _boundaryBuffer = 0.1f;

    [Header("换房间过渡")]
    [Tooltip("换房间时屏幕淡黑的Image（挂在Canvas下，覆盖全屏）")]
    [SerializeField] private Image _fadeImage;
    [Tooltip("淡入/淡出各自的持续时间（秒）")]
    [SerializeField] private float _fadeDuration = 0.2f;

    /// <summary>是否正在约束中。</summary>
    public bool IsConstrained { get; private set; }

    private List<Collider2D> _currentBounds = new List<Collider2D>();
    private Rigidbody2D _playerRb;
    private Coroutine _fadeCoroutine;

    private void Awake()
    {
        _playerRb = GetComponent<Rigidbody2D>();

        // 自动查找FadeImage（若未手动赋值）
        if (_fadeImage == null)
        {
            GameObject fadeObj = GameObject.Find("RoomFade");
            if (fadeObj != null)
                _fadeImage = fadeObj.GetComponent<Image>();
        }

        if (_fadeImage != null)
            _fadeImage.color = new Color(0, 0, 0, 0);
    }

    // ─── 公开接口 ────────────────────────────────────────────

    /// <summary>
    /// 启用约束，传入允许移动的区域Collider2D列表。
    /// </summary>
    public void EnableConstraint(List<Collider2D> bounds)
    {
        _currentBounds = bounds ?? new List<Collider2D>();
        IsConstrained = _currentBounds.Count > 0;

        if (IsConstrained)
        {
            if (_currentBounds.Count > 1)
                Debug.Log($"[Rewind] 玩家移动约束已启用（{_currentBounds.Count} 个房间）。");
            else
                Debug.Log("[Rewind] 玩家移动约束已启用。");
        }
    }

    /// <summary>
    /// 更新约束区域（NPC换房间时调用），带屏幕淡黑过渡。
    /// </summary>
    public void UpdateBounds(List<Collider2D> newBounds)
    {
        if (newBounds == null || newBounds.Count == 0) return;

        // 检查玩家是否在任何新房间内
        bool playerInAnyNewRoom = _playerRb != null && newBounds.Any(b => b.OverlapPoint(_playerRb.position));

        if (!playerInAnyNewRoom && _fadeImage != null)
        {
            // 玩家不在任何新房间内：走过渡协程
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(RoomTransitionRoutine(newBounds));
        }
        else
        {
            // 玩家已在新房间内：直接更新
            _currentBounds = newBounds;
            if (!playerInAnyNewRoom && _playerRb != null)
            {
                // 传送到第一个房间中心
                _playerRb.position = newBounds[0].bounds.center;
            }
        }
    }

    /// <summary>
    /// 禁用约束。
    /// </summary>
    public void DisableConstraint()
    {
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = null;
        }
        if (_fadeImage != null)
            _fadeImage.color = new Color(0, 0, 0, 0);

        IsConstrained = false;
        _currentBounds.Clear();
        Debug.Log("[Rewind] 玩家移动约束已解除。");
    }

    // ─── 内部逻辑 ────────────────────────────────────────────

    private void LateUpdate()
    {
        if (!IsConstrained || _currentBounds.Count == 0 || _playerRb == null) return;

        // 过渡期间不做边界检测，防止和传送位置冲突
        if (_fadeCoroutine != null) return;

        Vector2 playerPos = _playerRb.position;

        // 检查玩家是否在任何允许的房间内
        bool inAnyRoom = _currentBounds.Any(b => b.OverlapPoint(playerPos));

        if (!inAnyRoom)
        {
            // 玩家越界，找到最近的房间边界并拉回
            Vector2 closestPoint = FindClosestPointInBounds(playerPos);
            _playerRb.position = closestPoint;
        }
    }

    /// <summary>
    /// 找到玩家在所有允许房间中的最近点。
    /// </summary>
    private Vector2 FindClosestPointInBounds(Vector2 playerPos)
    {
        Vector2 closestPoint = playerPos;
        float minDistance = float.MaxValue;

        foreach (var bounds in _currentBounds)
        {
            Vector2 point = bounds.ClosestPoint(playerPos);
            float distance = Vector2.Distance(playerPos, point);

            if (distance < minDistance)
            {
                minDistance = distance;
                closestPoint = point;
            }
        }

        return closestPoint;
    }

    /// <summary>
    /// 换房间过渡：淡黑 → 传送 → 淡出。
    /// </summary>
    private IEnumerator RoomTransitionRoutine(List<Collider2D> newBounds)
    {
        // 淡入（变黑）
        yield return StartCoroutine(Fade(0f, 1f, _fadeDuration));

        // 传送并更新约束（传送到第一个房间中心）
        _currentBounds = newBounds;
        if (_playerRb != null && newBounds.Count > 0)
            _playerRb.position = newBounds[0].bounds.center;

        // 淡出（变透明）
        yield return StartCoroutine(Fade(1f, 0f, _fadeDuration));

        _fadeCoroutine = null;
        Debug.Log("[Rewind] 玩家换房间过渡完成。");
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(from, to, elapsed / duration);
            _fadeImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
        _fadeImage.color = new Color(0, 0, 0, to);
    }
}
