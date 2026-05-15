using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// 保存指示器。
/// 在屏幕右上角显示"存档保存中……"提示。
/// </summary>
public class SaveIndicator : MonoBehaviour
{
    [Header("UI 引用")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private TMP_Text _text;

    [Header("动画配置")]
    [Tooltip("淡入时间（秒）")]
    [SerializeField] private float _fadeInDuration = 0.3f;
    [Tooltip("显示时间（秒）")]
    [SerializeField] private float _displayDuration = 1.5f;
    [Tooltip("淡出时间（秒）")]
    [SerializeField] private float _fadeOutDuration = 0.5f;

    [Header("文本配置")]
    [SerializeField] private string _savingText = "存档保存中……";
    [SerializeField] private string _savedText = "存档已保存";

    private Coroutine _currentAnimation;

    private void Awake()
    {
        // 初始化为隐藏状态
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        // 订阅 SaveManager 事件
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.OnSaveStart += OnSaveStart;
            SaveManager.Instance.OnSaveComplete += OnSaveComplete;
        }
    }

    private void OnDestroy()
    {
        // 取消订阅
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.OnSaveStart -= OnSaveStart;
            SaveManager.Instance.OnSaveComplete -= OnSaveComplete;
        }
    }

    private void OnSaveStart()
    {
        ShowIndicator(_savingText, false);
    }

    private void OnSaveComplete()
    {
        ShowIndicator(_savedText, true);
    }

    /// <summary>
    /// 显示保存指示器。
    /// </summary>
    /// <param name="text">显示的文本</param>
    /// <param name="autoHide">是否自动隐藏</param>
    public void ShowIndicator(string text, bool autoHide = true)
    {
        if (_currentAnimation != null)
        {
            StopCoroutine(_currentAnimation);
        }

        if (_text != null)
        {
            _text.text = text;
        }

        _currentAnimation = StartCoroutine(ShowAnimation(autoHide));
    }

    /// <summary>
    /// 隐藏保存指示器。
    /// </summary>
    public void HideIndicator()
    {
        if (_currentAnimation != null)
        {
            StopCoroutine(_currentAnimation);
        }

        _currentAnimation = StartCoroutine(FadeOut());
    }

    private IEnumerator ShowAnimation(bool autoHide)
    {
        // 淡入
        yield return FadeIn();

        if (autoHide)
        {
            // 显示一段时间
            yield return new WaitForSeconds(_displayDuration);

            // 淡出
            yield return FadeOut();
        }
    }

    private IEnumerator FadeIn()
    {
        float elapsed = 0f;

        while (elapsed < _fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime; // 使用 unscaledDeltaTime 避免暂停影响
            float alpha = Mathf.Clamp01(elapsed / _fadeInDuration);

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = alpha;
            }

            yield return null;
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
        }
    }

    private IEnumerator FadeOut()
    {
        float elapsed = 0f;
        float startAlpha = _canvasGroup != null ? _canvasGroup.alpha : 1f;

        while (elapsed < _fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(startAlpha, 0f, elapsed / _fadeOutDuration);

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = alpha;
            }

            yield return null;
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
        }
    }
}
