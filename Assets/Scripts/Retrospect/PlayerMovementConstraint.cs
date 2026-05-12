using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 玩家移动约束。
/// 将玩家限制在指定的Collider2D区域内（回溯模式下限制在NPC所在房间）。
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

    private Collider2D _currentBounds;
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
    /// 更新约束区域（NPC换房间时调用），带屏幕淡黑过渡。
    /// </summary>
    public void UpdateBounds(Collider2D newBounds)
    {
        if (newBounds == null) return;

        bool needsTeleport = _playerRb != null && !newBounds.OverlapPoint(_playerRb.position);

        if (needsTeleport && _fadeImage != null)
        {
            // 有淡黑图：走过渡协程
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(RoomTransitionRoutine(newBounds));
        }
        else
        {
            // 无淡黑图或玩家已在新房间内：直接更新
            _currentBounds = newBounds;
            if (needsTeleport && _playerRb != null)
                _playerRb.position = newBounds.bounds.center;
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
        _currentBounds = null;
        Debug.Log("[Rewind] 玩家移动约束已解除。");
    }

    // ─── 内部逻辑 ────────────────────────────────────────────

    private void LateUpdate()
    {
        if (!IsConstrained || _currentBounds == null || _playerRb == null) return;

        // 过渡期间不做边界检测，防止和传送位置冲突
        if (_fadeCoroutine != null) return;

        Vector2 playerPos = _playerRb.position;
        if (!_currentBounds.OverlapPoint(playerPos))
        {
            Vector2 closestPoint = _currentBounds.ClosestPoint(playerPos);
            _playerRb.position = closestPoint;
        }
    }

    /// <summary>
    /// 换房间过渡：淡黑 → 传送 → 淡出。
    /// </summary>
    private IEnumerator RoomTransitionRoutine(Collider2D newBounds)
    {
        // 淡入（变黑）
        yield return StartCoroutine(Fade(0f, 1f, _fadeDuration));

        // 传送并更新约束
        _currentBounds = newBounds;
        if (_playerRb != null)
            _playerRb.position = newBounds.bounds.center;

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
