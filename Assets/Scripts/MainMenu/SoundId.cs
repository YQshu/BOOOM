namespace InnsmouthCafe.Audio
{
    /// <summary>
    /// 游戏内所有音效的唯一标识枚举。
    /// 新增音效时：① 在此处添加枚举值；② 在 AudioManager Inspector 中配对 AudioClip。
    /// </summary>
    public enum SoundId
    {
        /// <summary>通用按钮点击音效。</summary>
        ButtonClick,

        /// <summary>咖啡豆落入容器的碰撞音效。</summary>
        BeanDrop,

        /// <summary>研磨手柄点击 / 研磨进行中的音效。</summary>
        GrindClick,

        /// <summary>液体倾倒入杯的音效。</summary>
        PourLiquid,

        /// <summary>配料（糖浆、奶泡等）落入杯中的音效。</summary>
        ToppingDrop,

        /// <summary>萃取开始时的蒸汽 / 机器启动音效。</summary>
        ExtractionStart,

        /// <summary>整杯咖啡制作完成的提示音效。</summary>
        BrewingComplete,

        /// <summary>空槽位，可按需改名复用。</summary>
        Reserved01,

        /// <summary>空槽位，可按需改名复用。</summary>
        Reserved02,
    }
}
