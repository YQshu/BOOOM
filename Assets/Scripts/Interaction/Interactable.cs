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
    [SerializeField] private KeyCode _interactKey = KeyCode.F;

    [Header("显示与行为")]
    [Tooltip("是否允许重复交互")]
    [SerializeField] private bool _allowRepeatInteraction;
    [Tooltip("交互后是否作为线索写入线索系统")]
    [SerializeField] private bool _collectAsClue = true;
    [Tooltip("交互成功后是否隐藏当前对象")]
    [SerializeField] private bool _hideAfterInteraction;

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
        if (!_isPlayerInRange)
        {
            return;
        }

        if (!_allowRepeatInteraction && _isInteracted)
        {
            return;
        }

        if (Input.GetKeyDown(_interactKey))
        {
            Interact();
        }
    }

    /// <summary>
    /// 玩家进入交互范围时设置可交互状态。
    /// </summary>
    /// <param name="other">进入触发器的碰撞体。</param>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        _isPlayerInRange = true;

        if (_enableLog)
        {
            Debug.Log($"[Interactable] 可交互：{_interactionName}（按 {_interactKey}）", this);
        }
    }

    /// <summary>
    /// 玩家离开交互范围时清理可交互状态。
    /// </summary>
    /// <param name="other">离开触发器的碰撞体。</param>
    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        _isPlayerInRange = false;
    }

    /// <summary>
    /// 执行一次交互逻辑。
    /// </summary>
    private void Interact()
    {
        _isInteracted = true;

        if (_collectAsClue)
        {
            if (ClueManager.Instance != null)
            {
                ClueManager.Instance.CollectClue(_interactionId, _interactionName);
            }
            else if (_enableLog)
            {
                Debug.LogWarning("[Interactable] 未找到 ClueManager，线索不会被记录。", this);
            }
        }

        if (_enableLog)
        {
            Debug.Log($"[Interactable] 获得线索：{_interactionId} - {_interactionName}", this);
        }

        if (_hideAfterInteraction)
        {
            gameObject.SetActive(false);
        }
    }
}
