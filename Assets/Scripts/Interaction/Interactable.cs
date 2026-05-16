using UnityEngine;

/// <summary>
/// 基础可交互物体脚本。
/// 当玩家进入触发范围后可按键交互，并输出调试信息用于验证流程。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Interactable : MonoBehaviour
{
    [Header("交互配置")]
    [Tooltip("触发交互的按键")]
    [SerializeField] private KeyCode _interactKey = KeyCode.E;

    [Header("显示与行为")]
    [Tooltip("头顶调查提示UI")]
    [SerializeField] private GameObject _promptUI;
    [Tooltip("是否允许重复交互")]
    [SerializeField] private bool _allowRepeatInteraction;
    [Tooltip("交互后收集的线索 SO")]
    [SerializeField] private ClueDataSO _clueData;
    [Tooltip("交互成功后是否隐藏当前对象")]
    [SerializeField] private bool _hideAfterInteraction;

    [Header("Ink 对话（可选）")]
    [Tooltip("交互时触发的 Ink 故事 JSON（留空则只收集线索）")]
    [SerializeField] private TextAsset _inkStory;
    [Tooltip("从指定 knot 开始播放")]
    [SerializeField] private string _inkKnotName = "";

    [Header("旁白音频（可选）")]
    [Tooltip("交互时播放的旁白音频（有旁白则不触发Ink对话）")]
    [SerializeField] private AudioClip _narrationClip;

    [Header("调试输出")]
    [Tooltip("是否输出交互日志")]
    [SerializeField] private bool _enableLog = true;

    private bool _isPlayerInRange;
    private bool _isInteracted;

    private void Awake()
    {
        Collider2D collider2D = GetComponent<Collider2D>();
        collider2D.isTrigger = true;
    }

    private void Update()
    {
        if (!_isPlayerInRange) return;
        if (!_allowRepeatInteraction && _isInteracted) return;

        // 对话播放期间不响应交互
        if (InkDialogueManager.Instance != null && InkDialogueManager.Instance.IsPlaying) return;

        if (Input.GetKeyDown(_interactKey))
            Interact();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        _isPlayerInRange = true;

        if (_promptUI != null)
            _promptUI.SetActive(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        _isPlayerInRange = false;

        if (_promptUI != null)
            _promptUI.SetActive(false);
    }

    private void Interact()
    {
        _isInteracted = true;

        // 优先级：旁白 > Ink对话 > 直接收集线索
        if (_narrationClip != null)
        {
            // 有旁白：播放旁白并收集线索（不触发Ink）
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayNarration(_narrationClip);
            }
            else if (_enableLog)
            {
                Debug.LogWarning("[Interactable] AudioManager 未找到，无法播放旁白。", this);
            }

            if (_clueData != null)
            {
                ClueManager.Instance?.CollectClue(_clueData);
            }

            if (_enableLog)
                Debug.Log($"[Interactable] 旁白交互：{(_clueData != null ? _clueData.clueId : "无线索")}，旁白={_narrationClip.name}", this);
        }
        else if (_inkStory != null)
        {
            // 有Ink对话：触发对话
            if (InkDialogueManager.Instance != null)
                InkDialogueManager.Instance.StartDialogue(_inkStory, _inkKnotName);
            else if (_enableLog)
                Debug.LogWarning("[Interactable] 未找到 InkDialogueManager。", this);
        }
        else if (_clueData != null)
        {
            // 无旁白无对话：直接收集线索
            ClueManager.Instance?.CollectClue(_clueData);
        }

        if (_hideAfterInteraction)
        {
            if (_promptUI != null) _promptUI.SetActive(false);
            gameObject.SetActive(false);
        }
    }
}

