using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 对话行为 - 自动播放，点击加速/跳过
/// </summary>
public class DialogueBehaviour : PlayableBehaviour
{
    [Header("对话列表")]
    public DialogueLine[] dialogueLines;

    [Header("全局设置")]
    public bool enableTypewriter = true;
    public float typewriterSpeed = 0.05f;
    [Tooltip("是否允许点击加速（跳过打字效果）")]
    public bool allowClickToSkip = true;

    // UI 组件引用
    private GameObject dialoguePanel;
    private TextMeshProUGUI speakerText;
    private TextMeshProUGUI contentText;
    private Image portraitImage;
    private Button continueButton;

    // 控制变量
    private PlayableDirector currentDirector;
    private int currentLineIndex = 0;
    private Coroutine typewriterCoroutine;
    private string currentFullText = "";
    private bool isTyping = false;
    private bool isAutoAdvancing = false;  // 是否正在自动等待下一句

    // 缓存
    private static DialogueBehaviour activeDialogue;
    private static GameObject _manualDialoguePanel;

    public static void SetDialoguePanel(GameObject panel)
    {
        _manualDialoguePanel = panel;
    }

    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        if (dialogueLines == null || dialogueLines.Length == 0) return;
        if (activeDialogue != null && activeDialogue != this) return;

        currentDirector = playable.GetGraph().GetResolver() as PlayableDirector;

        if (!InitializeDialogueUI()) return;

        activeDialogue = this;
        currentLineIndex = 0;

        // 显示第一条对话
        ShowCurrentLine();

        // 显示对话面板
        dialoguePanel.SetActive(true);
    }

    public override void OnBehaviourPause(Playable playable, FrameData info)
    {
        CleanupDialogue();
    }

    private bool InitializeDialogueUI()
    {
        if (_manualDialoguePanel != null)
        {
            dialoguePanel = _manualDialoguePanel;
        }

        if (dialoguePanel == null)
        {
            dialoguePanel = GameObject.Find("DialoguePanel");
        }

        if (dialoguePanel == null)
        {
            Debug.LogError("找不到 DialoguePanel！");
            return false;
        }

        speakerText = dialoguePanel.transform.Find("SpeakerName")?.GetComponent<TextMeshProUGUI>();
        contentText = dialoguePanel.transform.Find("DialogueText")?.GetComponent<TextMeshProUGUI>();
        portraitImage = dialoguePanel.transform.Find("Portrait")?.GetComponent<Image>();
        continueButton = dialoguePanel.transform.Find("ContinueButton")?.GetComponent<Button>();

        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnContinueClicked);
            continueButton.gameObject.SetActive(allowClickToSkip);  // 只有允许加速时才显示按钮
            UpdateButtonText();
        }

        return true;
    }

    private void ShowCurrentLine()
    {
        if (currentLineIndex >= dialogueLines.Length)
        {
            FinishDialogue();
            return;
        }

        DialogueLine line = dialogueLines[currentLineIndex];
        currentFullText = line.dialogueText;

        // 更新UI
        if (speakerText != null) speakerText.text = line.speakerName;
        if (portraitImage != null) portraitImage.sprite = line.speakerPortrait;

        // 清空文字
        if (contentText != null) contentText.text = "";

        // 开始打字机效果
        if (enableTypewriter && contentText != null && !string.IsNullOrEmpty(currentFullText))
        {
            if (typewriterCoroutine != null)
            {
                GameTimer.StopCoroutineStatic(typewriterCoroutine);
            }
            isTyping = true;
            typewriterCoroutine = GameTimer.StartCoroutineStatic(TypewriterRoutine());
        }
        else if (contentText != null)
        {
            contentText.text = currentFullText;
            isTyping = false;
            StartAutoAdvance();  // 没有打字机效果，直接开始自动计时
        }

        // 更新按钮显示
        if (continueButton != null && allowClickToSkip)
        {
            continueButton.interactable = true;
            UpdateButtonText();
        }
    }

    private IEnumerator TypewriterRoutine()
    {
        if (contentText == null) yield break;

        contentText.text = "";

        // 获取本条对话的打字速度
        float speed = dialogueLines[currentLineIndex].customTypewriterSpeed > 0
            ? dialogueLines[currentLineIndex].customTypewriterSpeed
            : typewriterSpeed;

        foreach (char c in currentFullText)
        {
            // 如果被打断，跳出循环
            if (!isTyping) break;

            contentText.text += c;
            yield return new WaitForSecondsRealtime(speed);
        }

        // 打字完成
        isTyping = false;
        typewriterCoroutine = null;
        UpdateButtonText();

        // 开始自动进入下一句的计时
        StartAutoAdvance();
    }

    private void StartAutoAdvance()
    {
        if (isAutoAdvancing) return;

        // 计算本句对话的显示时长（基于文字长度）
        float displayDuration = Mathf.Max(1f, currentFullText.Length * 0.1f);

        isAutoAdvancing = true;
        GameTimer.DelayCall(displayDuration, () => {
            if (!isTyping && !isAutoAdvancing) return;
            isAutoAdvancing = false;
            NextLine();
        });
    }

    private void UpdateButtonText()
    {
        if (continueButton == null) return;

        TextMeshProUGUI btnText = continueButton.GetComponentInChildren<TextMeshProUGUI>();
        if (btnText == null) return;

        if (isTyping)
        {
            btnText.text = "Accel";
        }
        else
        {
            btnText.text = "Skip";
        }
    }

    private void OnContinueClicked()
    {
        if (!allowClickToSkip) return;

        // 如果正在打字，加速（立即完成当前打字）
        if (isTyping)
        {
            SkipToFullText();
            return;
        }

        // 打字已完成，直接进入下一句（跳过等待）
        SkipToNextLine();
    }

    private void SkipToFullText()
    {
        // 停止打字机协程
        if (typewriterCoroutine != null)
        {
            GameTimer.StopCoroutineStatic(typewriterCoroutine);
            typewriterCoroutine = null;
        }

        // 直接显示完整文字
        if (contentText != null)
        {
            contentText.text = currentFullText;
        }

        isTyping = false;
        UpdateButtonText();

        // 重新开始自动计时（缩短等待时间）
        if (isAutoAdvancing)
        {
            // 取消当前的自动计时，重新开始
            isAutoAdvancing = false;
        }
        // 等待0.5秒后自动进入下一句（或者立即进入？这里设为等待0.5秒）
        GameTimer.DelayCall(0.5f, () => {
            if (!isTyping && !isAutoAdvancing)
            {
                NextLine();
            }
        });
    }

    private void SkipToNextLine()
    {
        // 取消自动计时
        isAutoAdvancing = false;

        // 停止打字机（如果有）
        if (typewriterCoroutine != null)
        {
            GameTimer.StopCoroutineStatic(typewriterCoroutine);
            typewriterCoroutine = null;
        }

        isTyping = false;

        // 直接进入下一句
        NextLine();
    }

    private void NextLine()
    {
        currentLineIndex++;
        isAutoAdvancing = false;

        if (currentLineIndex < dialogueLines.Length)
        {
            ShowCurrentLine();
        }
        else
        {
            FinishDialogue();
        }
    }

    private void FinishDialogue()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        // 恢复 Timeline 播放
        if (currentDirector != null)
        {
            currentDirector.Resume();
        }

        CleanupDialogue();
    }

    private void CleanupDialogue()
    {
        if (typewriterCoroutine != null)
        {
            GameTimer.StopCoroutineStatic(typewriterCoroutine);
            typewriterCoroutine = null;
        }

        isTyping = false;
        isAutoAdvancing = false;

        if (activeDialogue == this)
        {
            activeDialogue = null;
        }
    }
}
