using UnityEngine;

/// <summary>
/// 基础可交互物体脚本。
/// 当玩家进入触发范围后可按键交互，并输出调试信息用于验证流程。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Interactable : MonoBehaviour
{
    [Header("交互配置")]
    [Tooltip("交互唯一ID，用于日志或线索系统定位对象")]
    [SerializeField] private string _interactionId = "CLUE_DEMO_01";
    [Tooltip("交互名称，用于提示和日志展示")]
    [SerializeField] private string _interactionName = "演示线索";
    [Tooltip("触发交互的按键")]
    [SerializeField] private KeyCode _interactKey = KeyCode.E;

    [Header("显示与行为")]
    [Tooltip("头顶调查提示UI（World Space Canvas），进入范围时自动显示")]
    [SerializeField] private GameObject _promptUI;
    [Tooltip("是否允许重复交互")]
    [SerializeField] private bool _allowRepeatInteraction;
    [Tooltip("交互后是否作为线索写入线索系统（无Ink故事时生效）")]
    [SerializeField] private bool _collectAsClue = true;
    [Tooltip("交互成功后是否隐藏当前对象")]
    [SerializeField] private bool _hideAfterInteraction;

    [Header("Ink 对话（可选）")]
    [Tooltip("交互时触发的 Ink 故事 JSON（留空则只收集线索）")]
    [SerializeField] private TextAsset _inkStory;
    [Tooltip("从指定 knot 开始播放，留空则从头播放")]
    [SerializeField] private string _inkKnotName = "";

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

        if (_enableLog)
            Debug.Log($"[Interactable] 可交互：{_interactionName}（按 {_interactKey}）", this);
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

        // 优先触发 Ink 对话
        if (_inkStory != null)
        {
            if (InkDialogueManager.Instance != null)
            {
                InkDialogueManager.Instance.StartDialogue(_inkStory, _inkKnotName);
            }
            else if (_enableLog)
            {
                Debug.LogWarning("[Interactable] 未找到 InkDialogueManager。", this);
            }
        }
        else if (_collectAsClue)
        {
            // 无 Ink 故事时直接收集线索
            if (ClueManager.Instance != null)
                ClueManager.Instance.CollectClue(_interactionId, _interactionName);
            else if (_enableLog)
                Debug.LogWarning("[Interactable] 未找到 ClueManager。", this);
        }

        if (_enableLog)
            Debug.Log($"[Interactable] 交互：{_interactionId} - {_interactionName}", this);

        if (_hideAfterInteraction)
        {
            if (_promptUI != null) _promptUI.SetActive(false);
            gameObject.SetActive(false);
        }
    }
}

