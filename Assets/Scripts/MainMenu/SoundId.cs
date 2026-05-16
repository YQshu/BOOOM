
/// <summary>
/// 游戏内所有音效的唯一标识枚举。
/// 新增音效时：① 在此处添加枚举值；② 在 AudioManager Inspector 中配对 AudioClip。
/// </summary>
public enum SoundId
{
    /// <summary>通用按钮点击音效。</summary>
    ButtonClick,

    /// <summary>时间线控制按钮音效（播放/暂停/快进等）。</summary>
    TimelineButton,

    /// <summary>可交互线索高亮/发现音效。</summary>
    ClueInteractable,

    /// <summary>线索墙点击线索音效。</summary>
    ClueWallClick,
}
