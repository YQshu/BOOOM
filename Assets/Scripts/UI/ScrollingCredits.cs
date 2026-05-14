using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// 电影式滚动字幕效果。
/// 每个文本条目独立向上移动，随着移动透明度逐渐降低，最终销毁。
/// </summary>
public class ScrollingCredits : MonoBehaviour
{
    [Header("滚动设置")]
    [Tooltip("每句话弹出的间隔时间（秒）")]
    [SerializeField] private float _lineInterval = 2f;
    [Tooltip("每个条目向上移动的速度（像素/秒）")]
    [SerializeField] private float _scrollSpeed = 30f;
    [Tooltip("条目移动多少距离后开始渐隐（像素）")]
    [SerializeField] private float _fadeStartDistance = 400f;
    [Tooltip("渐隐区域高度（像素）")]
    [SerializeField] private float _fadeHeight = 200f;

    [Header("彩蛋设置")]
    [Tooltip("彩蛋文本（仅指认外域黑客时显示）")]
    [SerializeField] private string _easterEggText = "真相永远藏在数据的深处...";
    [Tooltip("最后一条文本消失后，等待多久显示彩蛋（秒）")]
    [SerializeField] private float _easterEggDelay = 3f;
    [Tooltip("彩蛋文本显示时长（秒）")]
    [SerializeField] private float _easterEggDuration = 5f;
    [Tooltip("彩蛋文本UI（TMP_Text，显示在屏幕中央）")]
    [SerializeField] private TMP_Text _easterEggTextUI;

    [Header("UI 引用")]
    [Tooltip("文本条目预制体（需含 TMP_Text + CanvasGroup）")]
    [SerializeField] private GameObject _lineItemPrefab;
    [Tooltip("文本条目的父节点（ScrollRect Content）")]
    [SerializeField] private RectTransform _contentParent;

    private List<Coroutine> _activeCoroutines = new List<Coroutine>();
    private CanvasGroup _easterEggCG;
    private int _activeLineCount = 0;

    private void Awake()
    {
        // 初始化彩蛋UI的CanvasGroup
        if (_easterEggTextUI != null)
        {
            _easterEggCG = _easterEggTextUI.GetComponent<CanvasGroup>();
            if (_easterEggCG == null)
                _easterEggCG = _easterEggTextUI.gameObject.AddComponent<CanvasGroup>();

            _easterEggCG.alpha = 0f;
        }
    }

    /// <summary>
    /// 开始播放滚动字幕。
    /// </summary>
    /// <param name="text">完整文本，用 '·' 分隔每句话</param>
    /// <param name="hasEasterEgg">是否在结束后显示彩蛋</param>
    public void PlayCredits(string text, bool hasEasterEgg = false)
    {
        StopCredits();

        // 按 '·' 分割文本
        string[] lines = text.Split(new char[] { '·' }, System.StringSplitOptions.RemoveEmptyEntries);

        StartCoroutine(PlayCreditsCoroutine(lines, hasEasterEgg));
    }

    /// <summary>
    /// 停止所有滚动字幕。
    /// </summary>
    public void StopCredits()
    {
        StopAllCoroutines();
        _activeCoroutines.Clear();
        ClearLines();
        _activeLineCount = 0;
    }

    /// <summary>
    /// 清空所有文本条目。
    /// </summary>
    private void ClearLines()
    {
        if (_contentParent == null) return;

        foreach (Transform child in _contentParent)
            Destroy(child.gameObject);
    }

    private IEnumerator PlayCreditsCoroutine(string[] lines, bool hasEasterEgg)
    {
        _activeLineCount = 0;

        // 逐句弹出
        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            CreateAndAnimateLineItem(line.Trim());
            yield return new WaitForSeconds(_lineInterval);
        }

        // 等待所有条目消失
        while (_activeLineCount > 0)
        {
            yield return null;
        }

        // 如果有彩蛋，等待后显示
        if (hasEasterEgg)
        {
            yield return new WaitForSeconds(_easterEggDelay);
            yield return StartCoroutine(ShowEasterEgg());
        }
    }

    private void CreateAndAnimateLineItem(string text)
    {
        if (_lineItemPrefab == null || _contentParent == null) return;

        GameObject item = Instantiate(_lineItemPrefab, _contentParent);
        TMP_Text tmpText = item.GetComponentInChildren<TMP_Text>();
        if (tmpText != null)
            tmpText.text = text;

        // 确保有 CanvasGroup
        CanvasGroup cg = item.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = item.AddComponent<CanvasGroup>();

        RectTransform rt = item.GetComponent<RectTransform>();
        if (rt != null)
        {
            // 初始位置在底部
            rt.anchoredPosition = Vector2.zero;
        }

        // 启动该条目的独立移动协程
        _activeLineCount++;
        Coroutine coroutine = StartCoroutine(AnimateLineItem(item, rt, cg));
        _activeCoroutines.Add(coroutine);
    }

    /// <summary>
    /// 单个文本条目的移动和渐隐动画。
    /// </summary>
    private IEnumerator AnimateLineItem(GameObject item, RectTransform rt, CanvasGroup cg)
    {
        if (rt == null || cg == null) yield break;

        float traveledDistance = 0f;
        cg.alpha = 1f;

        while (item != null)
        {
            // 向上移动
            float moveAmount = _scrollSpeed * Time.deltaTime;
            Vector2 pos = rt.anchoredPosition;
            pos.y += moveAmount;
            rt.anchoredPosition = pos;

            traveledDistance += moveAmount;

            // 计算透明度（移动一定距离后开始渐隐）
            if (traveledDistance > _fadeStartDistance)
            {
                float fadeProgress = (traveledDistance - _fadeStartDistance) / _fadeHeight;
                cg.alpha = Mathf.Clamp01(1f - fadeProgress);

                // 完全透明后销毁
                if (cg.alpha <= 0f)
                {
                    Destroy(item);
                    _activeLineCount--;
                    yield break;
                }
            }

            yield return null;
        }

        _activeLineCount--;
    }

    /// <summary>
    /// 显示彩蛋文本（淡入 → 停留 → 淡出）。
    /// </summary>
    private IEnumerator ShowEasterEgg()
    {
        if (_easterEggTextUI == null || _easterEggCG == null) yield break;

        _easterEggTextUI.text = _easterEggText;

        // 淡入（1秒）
        float fadeInTime = 1f;
        float elapsed = 0f;
        while (elapsed < fadeInTime)
        {
            elapsed += Time.deltaTime;
            _easterEggCG.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInTime);
            yield return null;
        }
        _easterEggCG.alpha = 1f;

        // 停留
        yield return new WaitForSeconds(_easterEggDuration);

        // 淡出（1秒）
        float fadeOutTime = 1f;
        elapsed = 0f;
        while (elapsed < fadeOutTime)
        {
            elapsed += Time.deltaTime;
            _easterEggCG.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutTime);
            yield return null;
        }
        _easterEggCG.alpha = 0f;
    }
}
