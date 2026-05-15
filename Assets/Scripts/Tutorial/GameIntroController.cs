using System.Collections;
using UnityEngine;

/// <summary>
/// 游戏开场流程控制器。
/// 统筹整个开场流程：黑屏 → ink对话 → 屏幕亮起 → 教程面板 → 游戏开始。
/// </summary>
public class GameIntroController : MonoBehaviour
{
    [Header("依赖引用")]
    [Tooltip("Ink 对话文件")]
    [SerializeField] private TextAsset _introDialogueInk;

    [Tooltip("屏幕淡入淡出工具")]
    [SerializeField] private ScreenFader _screenFader;

    [Tooltip("教程面板")]
    [SerializeField] private TutorialPanel _tutorialPanel;

    [Tooltip("玩家控制器")]
    [SerializeField] private PlayerController _playerController;

    [Header("开场配置")]
    [Tooltip("是否强制启用开场流程（测试用，勾选后即使有存档也会播放开场）")]
    [SerializeField] private bool _forceIntro = false;

    [Tooltip("对话结束后延迟多久淡入（秒）")]
    [SerializeField] private float _fadeInDelay = 0.5f;

    private bool _introCompleted = false;

    private void Start()
    {
        // 检查是否需要播放开场流程
        bool shouldPlayIntro = _forceIntro || !HasSaveFile();

        if (shouldPlayIntro)
        {
            // 没有存档或强制播放 → 播放开场流程
            StartCoroutine(IntroSequence());
        }
        else
        {
            // 有存档 → 跳过开场，直接开始游戏
            Debug.Log("[GameIntro] 检测到存档，跳过开场流程");
            SkipIntro();
        }
    }

    /// <summary>
    /// 检查是否有存档文件。
    /// </summary>
    private bool HasSaveFile()
    {
        if (SaveManager.Instance != null)
        {
            return SaveManager.Instance.HasSaveFile();
        }
        return false;
    }

    /// <summary>
    /// 开场流程协程。
    /// </summary>
    private IEnumerator IntroSequence()
    {
        Debug.Log("[GameIntro] 开场流程开始");

        // 1. 禁用玩家输入
        if (_playerController != null)
            _playerController.SetInputEnabled(false);

        // 2. 设置黑屏
        if (_screenFader != null)
            _screenFader.SetBlack();

        // 等待一帧确保所有初始化完成
        yield return null;

        // 3. 播放 ink 对话
        if (_introDialogueInk != null && InkDialogueManager.Instance != null)
        {
            // 订阅对话结束事件
            InkDialogueManager.Instance.OnDialogueEnd += OnDialogueEnd;

            // 开始对话（传统模式，sentenceCount = 0）
            InkDialogueManager.Instance.StartDialogue(_introDialogueInk, "intro", 0);

            // 等待对话结束
            yield return new WaitUntil(() => _introCompleted);
        }
        else
        {
            Debug.LogWarning("[GameIntro] 未配置 Ink 对话文件或 InkDialogueManager 不存在");
        }

        // 4. 延迟后淡入
        yield return new WaitForSeconds(_fadeInDelay);

        if (_screenFader != null)
        {
            bool fadeInComplete = false;
            _screenFader.FadeIn(() => fadeInComplete = true);
            yield return new WaitUntil(() => fadeInComplete);
        }

        // 5. 显示教程面板
        if (_tutorialPanel != null)
        {
            bool tutorialComplete = false;
            _tutorialPanel.OnTutorialComplete += () => tutorialComplete = true;
            _tutorialPanel.StartTutorial();

            // 等待教程完成
            yield return new WaitUntil(() => tutorialComplete);
        }
        else
        {
            Debug.LogWarning("[GameIntro] 未配置教程面板");
        }

        // 6. 游戏正式开始
        StartGame();

        Debug.Log("[GameIntro] 开场流程结束");
    }

    /// <summary>
    /// 对话结束回调。
    /// </summary>
    private void OnDialogueEnd()
    {
        _introCompleted = true;

        // 取消订阅
        if (InkDialogueManager.Instance != null)
            InkDialogueManager.Instance.OnDialogueEnd -= OnDialogueEnd;

        Debug.Log("[GameIntro] 对话结束");
    }

    /// <summary>
    /// 跳过开场流程，直接开始游戏。
    /// </summary>
    private void SkipIntro()
    {
        // 确保屏幕是透明的
        if (_screenFader != null)
            _screenFader.SetClear();

        // 启用玩家输入
        StartGame();
    }

    /// <summary>
    /// 游戏正式开始。
    /// </summary>
    private void StartGame()
    {
        // 启用玩家输入
        if (_playerController != null)
            _playerController.SetInputEnabled(true);

        Debug.Log("[GameIntro] 游戏开始");
    }

    private void OnDestroy()
    {
        // 清理事件订阅
        if (InkDialogueManager.Instance != null)
            InkDialogueManager.Instance.OnDialogueEnd -= OnDialogueEnd;
    }
}
