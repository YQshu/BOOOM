// 杰斯的记忆视角
// 时间：案发当晚 22:00 - 23:00
// 地点：《离线》酒吧
//
// Tag 约定：
//   # speaker: 名字        → 说话人
//   # portrait: id         → 头像（jess / kai / kenji / aira / bartender / unknown）
//   # clue: ID,名称        → 触发线索收集

// ══════════════════════════════════════════
//  开场
// ══════════════════════════════════════════

=== intro ===
# speaker: 杰斯
# portrait: jess
霓虹灯在雨水中折射出七彩的光，《离线》酒吧，这座城市最后的秘密角落。
# speaker: 杰斯
今晚，有人死在了这里。而我，是第一个发现的人。
# speaker: 杰斯
我需要回想起那晚发生的一切。
-> END

// ══════════════════════════════════════════
//  吧台区域线索
// ══════════════════════════════════════════

=== clue_broken_glass ===
# speaker: 杰斯
# portrait: jess
# clue: CLUE_JESS_GLASS_01,破碎的酒杯
吧台角落有一只破碎的酒杯。碎片的分布方式很奇怪——不像是意外打碎的。
* [仔细检查碎片]
    # clue: CLUE_JESS_GLASS_02,酒杯上的指纹
    # speaker: 杰斯
    杯沿上有两组不同的指纹。其中一组……和凯的手型很像。
    -> END
* [先记下来，继续调查]
    # speaker: 杰斯
    记在脑子里。也许之后会有用。
    -> END

=== clue_note ===
# speaker: 杰斯
# portrait: jess
# clue: CLUE_JESS_NOTE_01,神秘纸条
吧台下面有一张揉皱的纸条。上面写着一串数字：22:47。
# speaker: 杰斯
案发时间是22:50。这三分钟……意味着什么？
-> END

=== clue_cctv_blind_spot ===
# speaker: 杰斯
# portrait: jess
# clue: CLUE_JESS_CCTV_01,监控盲区
我注意到吧台右侧有一个角落，正好在监控摄像头的死角里。
# speaker: 杰斯
如果有人不想被看到，会选择站在那里。
-> END

// ══════════════════════════════════════════
//  NPC 对话
// ══════════════════════════════════════════

=== talk_bartender ===
# speaker: 杰斯
# portrait: jess
我走向调酒师。
# speaker: 调酒师
# portrait: bartender
你好，今晚不太平啊。
* [问关于凯的行踪]
    # speaker: 杰斯
    凯今晚一直在这里吗？
    # speaker: 调酒师
    # portrait: bartender
    # clue: CLUE_JESS_KAI_ALIBI_01,凯的可疑行踪
    在啊。不过……他22:40左右离开了一会儿，说去洗手间。但洗手间在另一个方向。
    # speaker: 调酒师
    大概十分钟后才回来。我当时觉得奇怪，但没多想。
    -> END
* [问关于受害者]
    # speaker: 杰斯
    受害者今晚有没有和谁起冲突？
    # speaker: 调酒师
    # portrait: bartender
    # clue: CLUE_JESS_VICTIM_FIGHT_01,受害者的争吵
    有。大概22:30，他和一个穿黑色夹克的人吵了起来。声音很大，但我没听清说什么。
    # speaker: 调酒师
    那个人……背对着我，我没看清脸。
    -> END
* [什么都不问，离开]
    # speaker: 杰斯
    没事，谢谢。
    -> END

=== talk_witness ===
# speaker: 杰斯
# portrait: jess
舞池边站着一个看起来很紧张的人。
# speaker: 目击者
# portrait: unknown
你……你也在调查吗？
* [是的，你看到了什么？]
    # speaker: 目击者
    # portrait: unknown
    # clue: CLUE_JESS_WITNESS_01,目击者证词
    我看到……22:45左右，有人从VIP室出来，走得很快。戴着帽子，看不清脸。
    # speaker: 目击者
    但我记得他穿着黑色夹克。
    -> END
* [你认识受害者吗？]
    # speaker: 目击者
    # portrait: unknown
    # clue: CLUE_JESS_VICTIM_ID_01,受害者身份线索
    认识。他叫林，是个数据掮客。最近好像得罪了什么人。
    -> END

// ══════════════════════════════════════════
//  VIP室（需要线索解锁）
// ══════════════════════════════════════════

=== clue_vip_room ===
# speaker: 杰斯
# portrait: jess
# clue: CLUE_JESS_VIP_01,VIP室的痕迹
VIP室的地毯上有一块深色污渍。不是酒——颜色太深了。
# speaker: 杰斯
# clue: CLUE_JESS_VIP_02,凶器线索
角落里有一根细长的金属棒，被擦拭过，但还是留下了微量的……
# speaker: 杰斯
这就是凶器。
-> END
