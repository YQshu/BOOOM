using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 屏幕淡入淡出工具。
/// 使用 CanvasGroup 控制透明度和交互遮挡。
/// </summary>
public class ScreenFader : MonoBehaviour
{
    [Header("UI 引用")]
    [Tooltip("CanvasGroup 组件")]
    [SerializeField] private CanvasGroup _canvasGroup;

    [Header("淡入淡出配置")]
    [Tooltip("淡入淡出持续时间（秒）")]
    [SerializeField] private float _fadeDuration = 1f;

    private Coroutine _fadeCoroutine;

    private void Awake()
    {
        // 如果没有配置 canvasGroup，尝试自动查找
        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();

        if (_canvasGroup == null)
        {
            Debug.LogWarning("[ScreenFader] 未找到 CanvasGroup 组件");
        }
    }

    /// <summary>
    /// 淡出到黑屏（alpha 从 0 到 1）。
    /// </summary>
    public void FadeOut(Action onComplete = null)
    {
        if (_fadeCoroutine != null)
            StopCoroutine(_fadeCoroutine);

        _fadeCoroutine = StartCoroutine(FadeRoutine(0f, 1f, true, onComplete));
    }

    /// <summary>
    /// 从黑屏淡入（alpha 从 1 到 0）。
    /// </summary>
    public void FadeIn(Action onComplete = null)
    {
        if (_fadeCoroutine != null)
            StopCoroutine(_fadeCoroutine);

        _fadeCoroutine = StartCoroutine(FadeRoutine(1f, 0f, false, onComplete));
    }

    /// <summary>
    /// 立即设置为黑屏。
    /// </summary>
    public void SetBlack()
    {
        if (_fadeCoroutine != null)
            StopCoroutine(_fadeCoroutine);

        SetCanvasGroup(1f, true);
    }

    /// <summary>
    /// 立即设置为透明（无遮罩）。
    /// </summary>
    public void SetClear()
    {
        if (_fadeCoroutine != null)
            StopCoroutine(_fadeCoroutine);

        SetCanvasGroup(0f, false);
    }

    /// <summary>
    /// 淡入淡出协程。
    /// </summary>
    private IEnumerator FadeRoutine(float startAlpha, float endAlpha, bool blockRaycasts, Action onComplete)
    {
        if (_canvasGroup == null)
        {
            Debug.LogWarning("[ScreenFader] canvasGroup 为空，无法执行淡入淡出");
            onComplete?.Invoke();
            yield break;
        }

        // 开始淡入淡出时，根据目标状态设置交互
        if (startAlpha > 0f)
        {
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
        }

        float elapsed = 0f;

        while (elapsed < _fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _fadeDuration);
            float alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            _canvasGroup.alpha = alpha;
            yield return null;
        }

        // 设置最终状态
        SetCanvasGroup(endAlpha, blockRaycasts);
        _fadeCoroutine = null;
        onComplete?.Invoke();
    }

    /// <summary>
    /// 设置 CanvasGroup 状态。
    /// </summary>
    private void SetCanvasGroup(float alpha, bool blockRaycasts)
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = alpha;
            _canvasGroup.interactable = blockRaycasts;
            _canvasGroup.blocksRaycasts = blockRaycasts;
        }
    }
}
