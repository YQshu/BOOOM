using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[System.Serializable]
public class RewindControlClip : PlayableAsset, ITimelineClipAsset
{
    [Header("回溯配置")]
    [SerializeField] private bool _allowRewind = true;
    [SerializeField] private bool _pauseAtClip = true;

    public ClipCaps clipCaps
    {
        get { return ClipCaps.Blending; }
    }

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<RewindControlBehaviour>.Create(graph);
        var behaviour = playable.GetBehaviour();
        behaviour.allowRewind = _allowRewind;
        behaviour.pauseAtClip = _pauseAtClip;
        return playable;
    }
}
