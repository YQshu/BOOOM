using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[System.Serializable]
public class DialogueClip : PlayableAsset, ITimelineClipAsset
{
    [Header("对话列表（按顺序播放）")]
    public DialogueLine[] dialogueLines;

    [Header("全局设置")]
    [Tooltip("是否启用打字机效果")]
    public bool enableTypewriter = true;

    [Tooltip("打字机速度")]
    [Range(0.01f, 0.2f)]
    public float typewriterSpeed = 0.05f;

    [Tooltip("是否允许点击加速/跳过")]
    public bool allowClickToSkip = true;

    public ClipCaps clipCaps
    {
        get { return ClipCaps.None; }
    }

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<DialogueBehaviour>.Create(graph);
        var behaviour = playable.GetBehaviour();

        behaviour.dialogueLines = dialogueLines;
        behaviour.enableTypewriter = enableTypewriter;
        behaviour.typewriterSpeed = typewriterSpeed;
        behaviour.allowClickToSkip = allowClickToSkip;

        return playable;
    }
}

[System.Serializable]
public class DialogueLine
{
    [Header("单条对话")]
    public string speakerName = "角色名";
    [TextArea(2, 4)]
    public string dialogueText = "对话内容";
    public Sprite speakerPortrait;

    [Tooltip("本条对话的打字机速度（0表示使用全局速度）")]
    public float customTypewriterSpeed = 0;
}

