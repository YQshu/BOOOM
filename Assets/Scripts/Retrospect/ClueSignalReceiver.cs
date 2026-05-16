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
        [Tooltip("触发的线索 SO")]
        public ClueDataSO clueData;
        [Tooltip("触发时播放的旁白音频（可选，有旁白则不触发Ink对话）")]
        public AudioClip narrationClip;
        [Tooltip("触发时启动的Ink对话（留空则直接收集线索，有旁白时忽略）")]
        public TextAsset inkStory;
        [Tooltip("Ink对话起始knot（留空则从头播放）")]
        public string inkKnotName;

        [Header("Timeline 同步配置")]
        [Tooltip("对话句子数量（填写后启用 Timeline 同步模式：对话自动播放，Timeline 继续运行；留空或填 0 则为传统模式：暂停 Timeline，手动点击继续）")]
        public int sentenceCount = 0;
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

        if (entry.clueData == null)
        {
            Debug.LogWarning("[Rewind] 线索配置缺少 ClueDataSO，跳过。");
            return;
        }

        // 优先级：旁白 > Ink对话 > 直接收集
        if (entry.narrationClip != null)
        {
            // 有旁白：播放旁白并直接收集线索（不触发Ink）
            TriggerNarrationClue(entry);
        }
        else if (entry.inkStory != null)
        {
            // 有Ink对话：暂停Timeline并启动对话（对话中通过tag自动收集线索）
            TriggerDialogueClue(entry);
        }
        else
        {
            // 无旁白无对话：直接收集线索
            TriggerDirectClue(entry);
        }
    }

    // ─── 内部逻辑 ────────────────────────────────────────────

    /// <summary>
    /// 播放旁白并直接收集线索（不触发Ink对话）。
    /// </summary>
    private void TriggerNarrationClue(SignalClueEntry entry)
    {
        // 播放可交互线索音效
        AudioManager.Instance?.PlaySfx(SoundId.ClueInteractable);

        // 播放旁白音频
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayNarration(entry.narrationClip);
        }
        else
        {
            Debug.LogWarning("[Rewind] AudioManager 未找到，无法播放旁白。");
        }

        // 直接收集线索
        ClueManager.Instance?.CollectClue(entry.clueData);
        Debug.Log($"[Rewind] 旁白线索触发：{entry.clueData.clueId}，旁白={entry.narrationClip.name}");
    }

    /// <summary>
    /// 直接收集线索（无对话、无旁白）。
    /// </summary>
    private void TriggerDirectClue(SignalClueEntry entry)
    {
        // 播放可交互线索音效
        AudioManager.Instance?.PlaySfx(SoundId.ClueInteractable);

        ClueManager.Instance?.CollectClue(entry.clueData);
        Debug.Log($"[Rewind] 自动线索触发：{entry.clueData.clueId}");
    }

    private void TriggerDialogueClue(SignalClueEntry entry)
    {
        // 播放可交互线索音效
        AudioManager.Instance?.PlaySfx(SoundId.ClueInteractable);

        // Timeline 同步模式：sentenceCount > 0 时自动播放，Timeline 继续运行
        // 传统模式：sentenceCount = 0 时暂停 Timeline，手动点击继续
        bool isTimelineSyncMode = entry.sentenceCount > 0;

        if (!isTimelineSyncMode && RetrospectManager.Instance != null)
        {
            // 传统模式：暂停 Timeline
            RetrospectManager.Instance.PauseForDialogue();
        }

        // 启动对话，传入句子数量
        if (InkDialogueManager.Instance != null)
        {
            InkDialogueManager.Instance.StartDialogue(entry.inkStory, entry.inkKnotName, entry.sentenceCount);
        }

        string mode = isTimelineSyncMode ? $"Timeline 同步模式（{entry.sentenceCount} 句）" : "传统模式（暂停 Timeline）";
        Debug.Log($"[Rewind] 对话线索触发：{entry.clueData.clueId}，启动对话 knot={entry.inkKnotName}，{mode}");
    }
}
