using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;

/// <summary>
/// Timeline自定义轨道，用于在特定时间点添加回溯控制标记
/// 允许在Timeline中标记可回溯的片段
/// </summary>
[TrackColor(0.8f, 0.2f, 0.2f)]
[TrackClipType(typeof(RewindControlClip))]
[TrackBindingType(typeof(TimelineRewindManager))]  // 改为 TimelineRewindManager
public class RewindControlTrack : TrackAsset
{
    // 轨道基类，无需额外实现
}
