using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// 回溯控制行为，在Timeline播放时执行
/// 处理回溯逻辑的具体实现
/// </summary>
public class RewindControlBehaviour : PlayableBehaviour
{
    [Header("控制参数")]
    [Tooltip("是否允许回溯")]
    public bool allowRewind = true;
    [Tooltip("是否在此处暂停")]
    public bool pauseAtClip = true;

    private TimelineRewindManager _timelineManager;  // 改为 TimelineRewindManager
    private bool _hasBeenTriggered = false;

    /// <summary>
    /// 当Playable被创建时调用
    /// </summary>
    public override void OnPlayableCreate(Playable playable)
    {
        _hasBeenTriggered = false;
    }

    /// <summary>
    /// 当Playable进入激活状态时调用
    /// </summary>
    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        if (_hasBeenTriggered || !allowRewind) return;

        // 获取TimelineRewindManager
        if (_timelineManager == null)
        {
            var director = playable.GetGraph().GetResolver() as PlayableDirector;
            if (director != null)
            {
                _timelineManager = director.GetComponent<TimelineRewindManager>();
            }
        }

        if (_timelineManager != null && pauseAtClip)
        {
            _timelineManager.Pause();  // 改为 Pause() 方法
        }

        _hasBeenTriggered = true;
    }

    /// <summary>
    /// 当Playable离开激活状态时调用
    /// </summary>
    public override void OnBehaviourPause(Playable playable, FrameData info)
    {
        _hasBeenTriggered = false;
    }
}
