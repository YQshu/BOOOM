using UnityEngine;

/// <summary>
/// 对话系统初始化器，用于手动挂载 DialoguePanel
/// 把这个脚本挂载到场景中的任意 GameObject 上，然后拖入你的对话面板
/// </summary>
public class DialogueInitializer : MonoBehaviour
{
    [Header("手动挂载对话面板")]
    [Tooltip("请将你创建的 DialoguePanel 拖到这里")]
    [SerializeField] private GameObject dialoguePanel;

    [Header("可选：自动查找")]
    [Tooltip("如果没有手动拖拽，是否自动查找")]
    [SerializeField] private bool autoFindIfNull = true;

    private void Awake()
    {
        if (dialoguePanel != null)
        {
            // 手动挂载的优先
            DialogueBehaviour.SetDialoguePanel(dialoguePanel);
            Debug.Log($"DialogueInitializer: 已设置对话面板 - {dialoguePanel.name}");
        }
        else if (autoFindIfNull)
        {
            // 自动查找作为备用
            GameObject foundPanel = GameObject.Find("DialoguePanel");
            if (foundPanel != null)
            {
                DialogueBehaviour.SetDialoguePanel(foundPanel);
                Debug.Log($"DialogueInitializer: 自动找到对话面板 - {foundPanel.name}");
            }
            else
            {
                Debug.LogWarning("DialogueInitializer: 找不到 DialoguePanel，请确保场景中有名为 DialoguePanel 的 GameObject");
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 编辑器模式下，如果拖拽了面板，立即设置
        if (dialoguePanel != null && Application.isPlaying == false)
        {
            Debug.Log($"DialogueInitializer: 已接收面板引用 - {dialoguePanel.name}");
        }
    }
#endif
}
