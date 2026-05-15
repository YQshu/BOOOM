using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 教程面板控制器。
/// 显示教程内容，支持翻页浏览。
/// </summary>
public class TutorialPanel : MonoBehaviour
{
    [Header("UI 引用")]
    [Tooltip("教程图片")]
    [SerializeField] private Image _tutorialImage;

    [Tooltip("教程文字")]
    [SerializeField] private TMP_Text _tutorialText;

    [Tooltip("下一个/关闭按钮")]
    [SerializeField] private Button _nextButton;

    [Tooltip("按钮文字")]
    [SerializeField] private TMP_Text _nextButtonText;

    [Tooltip("页码指示器（可选）")]
    [SerializeField] private TMP_Text _pageIndicator;

    [Header("教程数据")]
    [Tooltip("教程数据列表")]
    [SerializeField] private List<TutorialData> _tutorialPages = new List<TutorialData>();

    [Header("面板控制")]
    [Tooltip("面板的 CanvasGroup")]
    [SerializeField] private CanvasGroup _canvasGroup;

    /// <summary>教程结束时触发。</summary>
    public event Action OnTutorialComplete;

    private int _currentPageIndex = 0;

    private void Awake()
    {
        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();

        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void Start()
    {
        if (_nextButton != null)
            _nextButton.onClick.AddListener(OnNextButtonClick);

        // 初始隐藏
        SetVisible(false);
    }

    /// <summary>
    /// 开始显示教程。
    /// </summary>
    public void StartTutorial()
    {
        if (_tutorialPages == null || _tutorialPages.Count == 0)
        {
            Debug.LogWarning("[TutorialPanel] 没有配置教程数据");
            OnTutorialComplete?.Invoke();
            return;
        }

        _currentPageIndex = 0;
        SetVisible(true);
        ShowCurrentPage();
    }

    /// <summary>
    /// 显示当前页。
    /// </summary>
    private void ShowCurrentPage()
    {
        if (_currentPageIndex < 0 || _currentPageIndex >= _tutorialPages.Count)
            return;

        TutorialData currentPage = _tutorialPages[_currentPageIndex];

        // 更新图片
        if (_tutorialImage != null && currentPage.tutorialImage != null)
        {
            _tutorialImage.sprite = currentPage.tutorialImage;
            _tutorialImage.gameObject.SetActive(true);
        }
        else if (_tutorialImage != null)
        {
            _tutorialImage.gameObject.SetActive(false);
        }

        // 更新文字
        if (_tutorialText != null)
            _tutorialText.text = currentPage.tutorialText;

        // 更新按钮文字
        bool isLastPage = _currentPageIndex >= _tutorialPages.Count - 1;
        if (_nextButtonText != null)
            _nextButtonText.text = isLastPage ? "关闭" : "下一个";

        // 更新页码指示器
        if (_pageIndicator != null)
            _pageIndicator.text = $"{_currentPageIndex + 1} / {_tutorialPages.Count}";

        Debug.Log($"[TutorialPanel] 显示教程页 {_currentPageIndex + 1}/{_tutorialPages.Count}");
    }

    /// <summary>
    /// 下一个按钮点击事件。
    /// </summary>
    private void OnNextButtonClick()
    {
        _currentPageIndex++;

        if (_currentPageIndex >= _tutorialPages.Count)
        {
            // 所有教程看完，关闭面板
            CloseTutorial();
        }
        else
        {
            // 显示下一页
            ShowCurrentPage();
        }
    }

    /// <summary>
    /// 关闭教程面板。
    /// </summary>
    private void CloseTutorial()
    {
        SetVisible(false);
        OnTutorialComplete?.Invoke();
        Debug.Log("[TutorialPanel] 教程已完成");
    }

    /// <summary>
    /// 设置面板可见性。
    /// </summary>
    private void SetVisible(bool visible)
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.interactable = visible;
            _canvasGroup.blocksRaycasts = visible;
        }
    }
}
