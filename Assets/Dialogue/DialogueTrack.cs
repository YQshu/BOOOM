using UnityEngine;
using UnityEngine.Timeline;

[TrackColor(0.3f, 0.6f, 0.8f)]
[TrackClipType(typeof(DialogueClip))]
[TrackBindingType(typeof(GameObject))]
public class DialogueTrack : TrackAsset
{
    // 轨道基类，无需额外实现
}
