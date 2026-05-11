using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

/// <summary>
/// Timeline线索信号接收器。
/// 挂载在NPC Timeline所在的GameObject上，配合Signal Track使用。
/// 收到ClueSignal时根据配置触发线索收集或Ink对话。
/// </summary>
public class ClueSignalReceiver : MonoBehaviour, INotificationReceiver
{
    [System.Serializable]
    public class SignalClueEntry
    {
        [Tooltip("线索ID（如 CLUE_BAR_GLASS_01）")]
        public string clueId;
        [Tooltip("线索显示名称")]
        public string clueName;
        [Tooltip("触发时启动的Ink对话（留空则直接收集线索）")]
        public TextAsset inkStory;
        [Tooltip("Ink对话起始knot（留空则从头播放）")]
        public string inkKnotName;
    }

    [Header("线索配置")]
    [Tooltip("按Signal触发顺序配置，第N个Signal对应第N条配置")]
    [SerializeField] private List<SignalClueEntry> _clueEntries = new List<SignalClueEntry>();

    /// <summary>已触发的Signal计数，用于匹配配置列表索引。</summary>
    private int _signalIndex;

    /// <summary>
    /// 回溯开始时重置计数（由RetrospectManager调用或Timeline从头播放时自动重置）。
    /// </summary>
    public void ResetSignalIndex()
    {
        _signalIndex = 0;
    }

    // ─── INotificationReceiver 实现 ────────────────────────────

    /// <summary>
    /// 接收Timeline Signal通知。
    /// </summary>
    public void OnNotify(Playable origin, INotification notification, object context)
    {
        // 仅处理SignalEmitter类型的通知
        if (!(notification is SignalEmitter emitter)) return;
        if (emitter.asset == null) return;

        if (_signalIndex >= _clueEntries.Count)
        {
            Debug.LogWarning($"[Rewind] Signal索引 {_signalIndex} 超出配置数量，忽略。");
            _signalIndex++;
            return;
        }

        SignalClueEntry entry = _clueEntries[_signalIndex];
        _signalIndex++;

        if (string.IsNullOrEmpty(entry.clueId))
        {
            Debug.LogWarning("[Rewind] 线索配置缺少clueId，跳过。");
            return;
        }

        // 有Ink对话：暂停Timeline并启动对话（对话中通过tag自动收集线索）
        if (entry.inkStory != null)
        {
            TriggerDialogueClue(entry);
        }
        else
        {
            // 无对话：直接收集线索
            TriggerDirectClue(entry);
        }
    }

    // ─── 内部逻辑 ────────────────────────────────────────────

    /// <summary>
    /// 直接收集线索（无对话）。
    /// </summary>
    private void TriggerDirectClue(SignalClueEntry entry)
    {
        if (ClueManager.Instance != null)
            ClueManager.Instance.CollectClue(entry.clueId, entry.clueName);

        Debug.Log($"[Rewind] 自动线索触发：{entry.clueId}（{entry.clueName}）");
    }

    /// <summary>
    /// 通过对话触发线索（暂停Timeline → 播放Ink对话 → 对话中收集线索）。
    /// </summary>
    private void TriggerDialogueClue(SignalClueEntry entry)
    {
        // 暂停Timeline
        if (RetrospectManager.Instance != null)
            RetrospectManager.Instance.PauseForDialogue();

        // 启动Ink对话
        if (InkDialogueManager.Instance != null)
            InkDialogueManager.Instance.StartDialogue(entry.inkStory, entry.inkKnotName);

        Debug.Log($"[Rewind] 对话线索触发：{entry.clueId}，启动对话 knot={entry.inkKnotName}");
    }
}
