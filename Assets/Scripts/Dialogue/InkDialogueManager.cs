using System;
using System.Collections;
using System.Collections.Generic;
using Ink.Runtime;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Playables;
using TMPro;

/// <summary>
/// Ink 对话管理器。
/// 驱动 Ink 故事文件，处理打字机效果、分支选择、Tag解析和线索触发。
///
/// DialoguePanel 子物体名称约定：
///   SpeakerName   — TMP_Text，说话人名称
///   DialogueText  — TMP_Text，对话内容
///   Portrait      — Image，角色头像
///   ContinueButton — Button，继续/加速按钮
///   ChoiceContainer — Transform，选项按钮的父节点（可选）
///
/// Ink Tag 约定：
///   # speaker: 杰斯          → 设置说话人，并自动匹配角色肖像
///   # clue: CLUE_ID|线索名称  → 触发线索收集（名称部分可省略）
///   # hide_continue          → 隐藏继续按钮（等待选项）
/// </summary>
public class InkDialogueManager : MonoBehaviour
{
    public static InkDialogueManager Instance { get; private set; }

    /// <summary>对话开始时触发。</summary>
    public event Action OnDialogueStart;
    /// <summary>对话结束时触发。</summary>
    public event Action OnDialogueEnd;

    [System.Serializable]
    public class CharacterPortrait
    {
        [Tooltip("角色名称，与 CSV 中 '角色' 列一致（如：杰斯、凯）")]
        public string characterName;
        public Sprite portrait;
    }

    [Header("UI 引用")]
    [Tooltip("对话面板根节点（需挂载 CanvasGroup，始终保持激活）")]
    [SerializeField] private GameObject _dialoguePanel;
    [Tooltip("说话人名称 TMP_Text（子物体名 SpeakerName）")]
    [SerializeField] private TMP_Text _speakerText;
    [Tooltip("对话内容 TMP_Text（子物体名 DialogueText）")]
    [SerializeField] private TMP_Text _contentText;
    [Tooltip("角色头像 Image（子物体名 Portrait）")]
    [SerializeField] private Image _portraitImage;
    [Tooltip("继续/加速按钮（子物体名 ContinueButton）")]
    [SerializeField] private Button _continueButton;
    [Tooltip("选项按钮的父节点（子物体名 ChoiceContainer，可选）")]
    [SerializeField] private Transform _choiceContainer;
    [Tooltip("选项按钮预制体，需含 Button + TMP_Text 子组件")]
    [SerializeField] private GameObject _choiceButtonPrefab;
    [Tooltip("自动播放按钮（子物体名 AutoPlayButton）")]
    [SerializeField] private Button _autoPlayButton;

    [Header("角色头像映射")]
    [SerializeField] private List<CharacterPortrait> _portraits = new List<CharacterPortrait>();

    [Header("打字机配置")]
    [Tooltip("Timeline 同步模式下，每句话的总时长（秒），包含打字时间和持续时间")]
    [SerializeField] private float _sentenceDuration = 2f;

    [Header("自动播放")]
    [Tooltip("Timeline 同步模式下，每句话完全显示后的持续时间（秒）。例如：0.5 表示文本打印完毕后再等待 0.5 秒，然后开始下一句")]
    [SerializeField] private float _autoPlayDelay = 0.5f;

    [Header("Timeline 同步自动播放")]
    [Tooltip("暂停键（Timeline 同步模式下可暂停 Timeline 和对话）")]
    [SerializeField] private KeyCode _pauseKey = KeyCode.T;
    [Tooltip("暂停提示 UI（可选）")]
    [SerializeField] private GameObject _pauseIndicator;

    private Story _story;
    private Coroutine _typewriterCoroutine;
    private bool _isTyping;
    private string _currentText = string.Empty;
    private bool _isPlaying;
    private bool _isAutoPlaying;
    private CanvasGroup _dialogueCanvasGroup;

    // Timeline 同步自动播放相关
    private bool _isTimelineSyncMode; // 是否为 Timeline 同步模式（对话自动播放，Timeline 继续运行）
    private int _sentenceCount;       // 对话句子数量（> 0 时启用 Timeline 同步模式）
    private bool _isAutoPaused;       // 是否处于手动暂停状态
    private Coroutine _autoPlayCoroutine;
    private float _remainingTime;     // 剩余等待时间

    public bool IsPlaying => _isPlaying;

    // ─── 生命周期 ────────────────────────────────────────────

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        // 启动时自动查找 DialoguePanel（若未手动赋值）
        if (_dialoguePanel == null)
            _dialoguePanel = GameObject.Find("DialoguePanel");

        if (_dialoguePanel != null)
        {
            AutoBindUIComponents();
            // 获取或添加 CanvasGroup
            _dialogueCanvasGroup = _dialoguePanel.GetComponent<CanvasGroup>();
            if (_dialogueCanvasGroup == null)
                _dialogueCanvasGroup = _dialoguePanel.AddComponent<CanvasGroup>();
            SetDialogueVisible(false);
        }
    }

    private void Start()
    {
        if (_continueButton != null)
            _continueButton.onClick.AddListener(OnContinueClicked);
        if (_autoPlayButton != null)
            _autoPlayButton.onClick.AddListener(ToggleAutoPlay);
    }

    private void Update()
    {
        // Timeline 同步自动播放模式下，按暂停键可暂停/恢复
        if (_isTimelineSyncMode && Input.GetKeyDown(_pauseKey))
        {
            if (_isAutoPaused)
                ResumeAutoPlay();
            else
                PauseAutoPlay();
        }
    }

    // ─── 公开接口 ────────────────────────────────────────────

    /// <summary>
    /// 开始播放一个 Ink 故事。
    /// </summary>
    /// <param name="inkJSON">编译后的 .json TextAsset（Ink 插件自动生成）</param>
    /// <param name="knotName">从指定 knot 开始，留空则从头播放</param>
    /// <summary>
    /// 启动 Ink 对话。
    /// </summary>
    /// <param name="inkJSON">Ink 故事文件</param>
    /// <param name="knotName">起始 knot 名称（留空则从头播放）</param>
    /// <param name="sentenceCount">对话句子数量（> 0 时启用 Timeline 同步模式：对话自动播放，Timeline 继续运行；= 0 时为传统模式：暂停 Timeline，手动点击继续）</param>
    public void StartDialogue(TextAsset inkJSON, string knotName = "", int sentenceCount = 0)
    {
        if (_isPlaying)
        {
            Debug.LogWarning("[InkDialogue] 已有对话在播放，忽略新请求。");
            return;
        }

        if (inkJSON == null)
        {
            Debug.LogWarning("[InkDialogue] inkJSON 为空，无法开始对话。");
            return;
        }

        _story = new Story(inkJSON.text);
        BindExternalFunctions();

        if (!string.IsNullOrEmpty(knotName))
        {
            if (_story.KnotContainerWithName(knotName) != null)
                _story.ChoosePathString(knotName);
            else
                Debug.LogWarning($"[InkDialogue] 找不到 knot：{knotName}");
        }

        _isPlaying = true;
        _isTimelineSyncMode = sentenceCount > 0; // sentenceCount > 0 时启用 Timeline 同步模式
        _sentenceCount = sentenceCount;
        _isAutoPaused = false;

        // 触发对话开始事件
        OnDialogueStart?.Invoke();

        // Timeline 同步模式：自动播放，不暂停 Timeline
        if (_isTimelineSyncMode)
        {
            _isAutoPlaying = true;
            // 隐藏自动播放按钮（Timeline 同步模式下由系统控制）
            if (_autoPlayButton != null)
                _autoPlayButton.gameObject.SetActive(false);
            Debug.Log($"[InkDialogue] Timeline 同步模式：对话自动播放（{sentenceCount} 句），每句持续 {_autoPlayDelay} 秒（打印完毕后）");
        }
        else
        {
            // 传统模式：若处于回溯模式，暂停 Timeline
            if (RetrospectManager.Instance != null && RetrospectManager.Instance.IsInRetrospect)
                RetrospectManager.Instance.PauseForDialogue();
            // 显示自动播放按钮（玩家可以手动切换）
            if (_autoPlayButton != null)
                _autoPlayButton.gameObject.SetActive(true);
            Debug.Log("[InkDialogue] 传统模式：手动点击继续");
        }

        if (_dialoguePanel != null) SetDialogueVisible(true);
        if (_continueButton != null) _continueButton.gameObject.SetActive(true);
        ClearChoices();
        ContinueStory();
    }

    // ─── 内部逻辑 ────────────────────────────────────────────

    private void ContinueStory()
    {
        if (_story == null) return;

        if (_story.canContinue)
        {
            _currentText = _story.Continue().Trim();
            HandleTags(_story.currentTags);
            ClearChoices();

            if (_typewriterCoroutine != null) StopCoroutine(_typewriterCoroutine);
            _typewriterCoroutine = StartCoroutine(TypewriterRoutine(_currentText));
        }
        else if (_story.currentChoices.Count > 0)
        {
            // 打字完成后显示选项
            if (!_isTyping) ShowChoices();
        }
        else
        {
            EndDialogue();
        }
    }

    private void HandleTags(List<string> tags)
    {
        foreach (string tag in tags)
        {
            int colonIndex = tag.IndexOf(':');
            if (colonIndex < 0) continue;

            string key = tag.Substring(0, colonIndex).Trim().ToLower();
            string value = tag.Substring(colonIndex + 1).Trim();

            switch (key)
            {
                case "speaker":
                    if (_speakerText != null) _speakerText.text = value;
                    SetPortrait(value); // 根据角色名自动匹配肖像
                    break;

                case "clue":
                    // 格式：# clue: CLUE_ID
                    ClueManager.Instance?.CollectClue(value.Trim());
                    break;

                case "hide_continue":
                    if (_continueButton != null) _continueButton.gameObject.SetActive(false);
                    break;
            }
        }
    }

    private void SetPortrait(string speakerName)
    {
        if (_portraitImage == null) return;

        CharacterPortrait entry = _portraits.Find(p =>
            string.Equals(p.characterName, speakerName, StringComparison.OrdinalIgnoreCase));

        if (entry != null && entry.portrait != null)
        {
            _portraitImage.sprite = entry.portrait;
            _portraitImage.gameObject.SetActive(true);
        }
        else
        {
            _portraitImage.gameObject.SetActive(false);
        }
    }

    private IEnumerator TypewriterRoutine(string text)
    {
        _isTyping = true;
        if (_contentText != null) _contentText.text = string.Empty;

        // Timeline 同步模式：根据总时长和持续时间，动态计算打字机速度
        float typewriterSpeed;
        if (_isTimelineSyncMode)
        {
            // 打字时间 = 总时长 - 持续时间
            float typingDuration = _sentenceDuration - _autoPlayDelay;
            if (typingDuration <= 0f)
            {
                // 如果持续时间 >= 总时长，直接瞬间显示
                typewriterSpeed = 0f;
            }
            else
            {
                // 每字符间隔 = 打字时间 ÷ 字符数
                int charCount = text.Length;
                typewriterSpeed = charCount > 0 ? typingDuration / charCount : 0f;
            }
        }
        else
        {
            // 传统模式：使用固定速度（0.05秒/字符）
            typewriterSpeed = 0.05f;
        }

        // 打字效果
        foreach (char c in text)
        {
            if (!_isTyping) break;
            if (_contentText != null) _contentText.text += c;
            if (typewriterSpeed > 0f)
                yield return new WaitForSecondsRealtime(typewriterSpeed);
        }

        // 确保完整显示
        if (_contentText != null) _contentText.text = text;
        _isTyping = false;
        _typewriterCoroutine = null;

        // 打字完成后，若有选项则显示
        if (_story != null && _story.currentChoices.Count > 0 && !_story.canContinue)
        {
            ShowChoices();
        }
        else if (_isAutoPlaying && _story != null && _story.canContinue)
        {
            // Timeline 同步模式：使用固定的 _autoPlayDelay（文本显示完毕后的持续时间）
            if (_autoPlayCoroutine != null) StopCoroutine(_autoPlayCoroutine);
            _autoPlayCoroutine = StartCoroutine(AutoPlayWaitRoutine(_autoPlayDelay));
        }
        else if (_isAutoPlaying && _story != null && !_story.canContinue && _story.currentChoices.Count == 0)
        {
            // Timeline 同步模式：最后一句话显示完毕后自动结束对话
            if (_autoPlayCoroutine != null) StopCoroutine(_autoPlayCoroutine);
            _autoPlayCoroutine = StartCoroutine(AutoPlayWaitRoutine(_autoPlayDelay, true)); // 传入 isLastSentence = true
        }
    }

    private void OnContinueClicked()
    {
        if (_isTyping)
        {
            // 加速：立即显示完整文字
            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
                _typewriterCoroutine = null;
            }
            if (_contentText != null) _contentText.text = _currentText;
            _isTyping = false;

            if (_story != null && _story.currentChoices.Count > 0 && !_story.canContinue)
                ShowChoices();
            return;
        }

        // 没有选项时继续推进故事
        if (_story != null && _story.currentChoices.Count == 0)
            ContinueStory();
    }

    private void ToggleAutoPlay()
    {
        // Timeline 同步模式下，自动播放由系统控制，不允许手动切换
        if (_isTimelineSyncMode)
        {
            Debug.LogWarning("[InkDialogue] Timeline 同步模式下，自动播放由系统控制，无法手动切换。");
            return;
        }

        _isAutoPlaying = !_isAutoPlaying;
        Debug.Log($"[InkDialogue] 自动播放：{(_isAutoPlaying ? "开启" : "关闭")}");

        // 如果开启自动播放时文本已显示完毕且可继续，立即启动等待
        if (_isAutoPlaying && !_isTyping && _story != null && _story.canContinue)
        {
            if (_typewriterCoroutine != null) StopCoroutine(_typewriterCoroutine);
            _typewriterCoroutine = StartCoroutine(AutoPlayWaitRoutine());
        }
    }

    private IEnumerator AutoPlayWaitRoutine()
    {
        yield return new WaitForSecondsRealtime(_autoPlayDelay);
        _typewriterCoroutine = null;
        if (_isAutoPlaying && _story != null && _story.canContinue)
            ContinueStory();
    }

    private IEnumerator AutoPlayWaitRoutine(float duration, bool isLastSentence = false)
    {
        float elapsed = 0f;
        _remainingTime = duration;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _remainingTime = duration - elapsed;
            yield return null;
        }

        _autoPlayCoroutine = null;

        // 如果是最后一句话，直接结束对话
        if (isLastSentence)
        {
            EndDialogue();
        }
        else if (_isAutoPlaying && _story != null && _story.canContinue)
        {
            ContinueStory();
        }
    }

    private void ShowChoices()
    {
        if (_choiceContainer == null || _choiceButtonPrefab == null)
        {
            Debug.LogWarning("[InkDialogue] 未配置 ChoiceContainer 或 ChoiceButtonPrefab，选项无法显示。");
            return;
        }

        if (_continueButton != null) _continueButton.gameObject.SetActive(false);

        for (int i = 0; i < _story.currentChoices.Count; i++)
        {
            Choice choice = _story.currentChoices[i];
            GameObject btn = Instantiate(_choiceButtonPrefab, _choiceContainer);

            TMP_Text btnText = btn.GetComponentInChildren<TMP_Text>();
            if (btnText != null) btnText.text = choice.text.Trim();

            int choiceIndex = i;
            btn.GetComponent<Button>()?.onClick.AddListener(() =>
            {
                _story.ChooseChoiceIndex(choiceIndex);
                ClearChoices();
                if (_continueButton != null) _continueButton.gameObject.SetActive(true);
                ContinueStory();
            });
        }
    }

    private void ClearChoices()
    {
        if (_choiceContainer == null) return;
        foreach (Transform child in _choiceContainer)
            Destroy(child.gameObject);
    }

    private void EndDialogue()
    {
        _isPlaying = false;
        _isAutoPlaying = false;
        _isTimelineSyncMode = false;
        _isAutoPaused = false;
        _story = null;

        if (_dialoguePanel != null) SetDialogueVisible(false);

        OnDialogueEnd?.Invoke();
        Debug.Log("[InkDialogue] 对话结束。");
    }

    /// <summary>
    /// 强制停止当前对话（用于外部中断，如退出回溯）。
    /// </summary>
    public void StopDialogue()
    {
        if (!_isPlaying) return;

        // 停止所有协程
        if (_typewriterCoroutine != null)
        {
            StopCoroutine(_typewriterCoroutine);
            _typewriterCoroutine = null;
        }
        if (_autoPlayCoroutine != null)
        {
            StopCoroutine(_autoPlayCoroutine);
            _autoPlayCoroutine = null;
        }

        // 清理状态
        _isTyping = false;
        ClearChoices();

        // 调用正常的结束流程
        EndDialogue();

        Debug.Log("[InkDialogue] 对话被强制停止。");
    }

    /// <summary>
    /// 绑定 Ink 外部函数，可在 .ink 文件中直接调用。
    /// 用法：~ collect_clue("CLUE_ID", "线索名称")
    /// </summary>
    private void BindExternalFunctions()
    {
        _story.BindExternalFunction("collect_clue", (string clueId, string clueName) =>
        {
            ClueManager.Instance?.CollectClue(clueId);
        });
    }

    private void SetDialogueVisible(bool visible)
    {
        if (_dialogueCanvasGroup == null) return;
        _dialogueCanvasGroup.alpha = visible ? 1f : 0f;
        _dialogueCanvasGroup.interactable = visible;
        _dialogueCanvasGroup.blocksRaycasts = visible;
    }

    /// <summary>
    /// 若 UI 字段未手动赋值，尝试按约定名称自动查找。
    /// </summary>
    private void AutoBindUIComponents()
    {
        Transform root = _dialoguePanel.transform;

        if (_speakerText == null)
            _speakerText = root.Find("SpeakerName")?.GetComponent<TMP_Text>();
        if (_contentText == null)
            _contentText = root.Find("DialogueText")?.GetComponent<TMP_Text>();
        if (_portraitImage == null)
            _portraitImage = root.Find("Portrait")?.GetComponent<Image>();
        if (_continueButton == null)
            _continueButton = root.Find("ContinueButton")?.GetComponent<Button>();
        if (_choiceContainer == null)
            _choiceContainer = root.Find("ChoiceContainer");
        if (_autoPlayButton == null)
            _autoPlayButton = root.Find("AutoPlayButton")?.GetComponent<Button>();
        if (_pauseIndicator == null)
            _pauseIndicator = root.Find("PauseIndicator")?.gameObject;
    }

    // ─── Timeline 同步自动播放功能 ────────────────────────────

    /// <summary>
    /// 暂停自动播放（同时暂停 Timeline）。
    /// </summary>
    private void PauseAutoPlay()
    {
        if (!_isTimelineSyncMode) return;

        _isAutoPaused = true;

        // 停止自动播放协程
        if (_autoPlayCoroutine != null)
        {
            StopCoroutine(_autoPlayCoroutine);
            _autoPlayCoroutine = null;
        }

        // 暂停 Timeline（通过 RetrospectManager）
        if (RetrospectManager.Instance != null && RetrospectManager.Instance.CurrentDirector != null)
        {
            var director = RetrospectManager.Instance.CurrentDirector;
            if (director.state == PlayState.Playing)
            {
                director.Pause();
                Debug.Log("[InkDialogue] Timeline 已暂停");
            }
        }

        // 显示暂停提示
        if (_pauseIndicator != null)
            _pauseIndicator.SetActive(true);

        Debug.Log("[InkDialogue] 自动播放已暂停（按 ESC 继续）");
    }

    /// <summary>
    /// 恢复自动播放（同时恢复 Timeline）。
    /// </summary>
    private void ResumeAutoPlay()
    {
        if (!_isTimelineSyncMode) return;

        _isAutoPaused = false;

        // 恢复 Timeline（通过 RetrospectManager）
        if (RetrospectManager.Instance != null && RetrospectManager.Instance.CurrentDirector != null)
        {
            var director = RetrospectManager.Instance.CurrentDirector;
            if (director.state == PlayState.Paused)
            {
                director.Resume();
                Debug.Log("[InkDialogue] Timeline 已恢复");
            }
        }

        // 隐藏暂停提示
        if (_pauseIndicator != null)
            _pauseIndicator.SetActive(false);

        // 继续自动播放（使用剩余时间）
        if (!_isTyping && _story != null && _story.canContinue && _remainingTime > 0f)
        {
            if (_autoPlayCoroutine != null) StopCoroutine(_autoPlayCoroutine);
            _autoPlayCoroutine = StartCoroutine(AutoPlayWaitRoutine(_remainingTime));
        }

        Debug.Log("[InkDialogue] 自动播放已恢复");
    }
}
