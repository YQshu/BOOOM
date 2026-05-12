using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

/// <summary>
/// 条件Track控制器（崩坏遮挡轨模式）。
/// 挂载在 PlayableDirector 所在的 GameObject 上。
///
/// 工作原理：
///   主轨道全程播放完整剧情内容。
///   崩坏遮挡轨默认播放（遮挡主轨道内容）；
///   玩家获得对应线索后，遮挡轨 mute，主轨道内容完整显现。
///
/// 编辑器配置步骤：
///   1. Timeline 窗口中给崩坏效果 Track 起易识别的名称（如 "Glitch_KAI_01"）
///   2. 在本组件列表中填入 trackName 和 requiredClueId
///   3. 崩坏效果 Track 在编辑器中保持 unmuted（默认播放）
/// </summary>
public class ConditionalTrackController : MonoBehaviour
{
    [Serializable]
    public class ConditionalTrack
    {
        [Tooltip("Timeline 窗口中崩坏效果 Track 的名称（区分大小写）")]
        public string trackName;
        [Tooltip("解锁此片段所需的线索ID，收集后崩坏遮挡消失")]
        public string requiredClueId;
    }

    [Header("崩坏遮挡Track配置")]
    [SerializeField] private List<ConditionalTrack> _conditionalTracks = new List<ConditionalTrack>();

    // 保存改动过的 Track 原始 muted 状态，退出时恢复
    private readonly Dictionary<TrackAsset, bool> _originalMutedStates = new Dictionary<TrackAsset, bool>();

    // ─── 公开接口 ────────────────────────────────────────────

    /// <summary>
    /// 进入回溯前调用，根据线索状态决定崩坏遮挡轨是否 mute。
    /// </summary>
    public void ApplyConditions(PlayableDirector director)
    {
        if (director == null || director.playableAsset == null) return;

        TimelineAsset timeline = director.playableAsset as TimelineAsset;
        if (timeline == null) return;

        _originalMutedStates.Clear();

        foreach (TrackAsset track in timeline.GetOutputTracks())
        {
            ConditionalTrack condition = _conditionalTracks.Find(c => c.trackName == track.name);
            if (condition == null) continue;

            _originalMutedStates[track] = track.muted;

            bool hasClue = ClueManager.Instance != null
                           && ClueManager.Instance.HasClue(condition.requiredClueId);
            // 有线索 → 遮挡消失（mute）；没有线索 → 遮挡播放（unmute）
            track.muted = hasClue;

            Debug.Log($"[ConditionalTrack] '{track.name}' muted={track.muted}" +
                      $"（线索:{condition.requiredClueId} 已收集:{hasClue}）");
        }
    }

    /// <summary>
    /// 退出回溯时调用，将所有 Track 恢复为原始状态。
    /// </summary>
    public void RestoreConditions()
    {
        foreach (var kvp in _originalMutedStates)
        {
            if (kvp.Key != null)
                kvp.Key.muted = kvp.Value;
        }
        _originalMutedStates.Clear();
        Debug.Log("[ConditionalTrack] Track 条件已恢复。");
    }
}
