// 凯的记忆视角（存在记忆盲区与主观扭曲）
// 时间：案发当晚 22:00 - 23:00
// 注意：凯是真凶，他的叙述存在矛盾和空白

=== intro ===
# speaker: 凯
# portrait: kai
今晚的酒吧很吵。我只是想安静地喝一杯。
# speaker: 凯
林也在。我们……有些事情需要谈清楚。
# speaker: 凯
（记忆在这里变得模糊。）
-> END

// ══════════════════════════════════════════
//  凯的自述（存在矛盾）
// ══════════════════════════════════════════

=== kai_alibi ===
# speaker: 凯
# portrait: kai
我一直在吧台。整晚。
* [真的整晚？]
    # speaker: 凯
    # portrait: kai
    # clue: CLUE_KAI_ALIBI_GAP_01,凯的记忆空白
    ……大部分时间。中间我去了一下……我去哪了来着？
    # speaker: 凯
    记忆有点模糊。可能是喝多了。
    -> END
* [你和林说过话吗？]
    # speaker: 凯
    # portrait: kai
    # clue: CLUE_KAI_LIN_DENY_01,凯否认与受害者交谈
    没有。我们不熟。
    # speaker: 凯
    （但他的眼神飘向了别处。）
    -> END

=== kai_jacket ===
# speaker: 凯
# portrait: kai
我今晚穿的是白色衬衫。
# speaker: 凯
# clue: CLUE_KAI_JACKET_01,凯的服装矛盾
（但你在别处看到的目击者描述是——黑色夹克。）
# speaker: 凯
黑色夹克？那不是我。
-> END

=== kai_timeline_gap ===
# speaker: 凯
# portrait: kai
# clue: CLUE_KAI_TIME_GAP_01,22:40的十分钟空白
22:40……我在哪？
# speaker: 凯
我去……洗手间。对，洗手间。
* [洗手间在哪个方向？]
    # speaker: 凯
    # portrait: kai
    # clue: CLUE_KAI_DIRECTION_LIE_01,凯指错了方向
    就在……那边。
    # speaker: 凯
    （他指向的方向，正是VIP室所在的走廊。）
    -> END
* [你去了多久？]
    # speaker: 凯
    # portrait: kai
    就几分钟。
    # speaker: 凯
    （调酒师说是十分钟。）
    -> END

// ══════════════════════════════════════════
//  凯视角的线索（反向证据）
// ══════════════════════════════════════════

=== clue_kai_phone ===
# speaker: 凯
# portrait: kai
# clue: CLUE_KAI_PHONE_01,凯的手机记录
我的手机……22:47有一条发出的消息。
# speaker: 凯
只是普通的消息。
# speaker: 凯
（消息内容：「完成了。」）
-> END

=== clue_kai_hands ===
# speaker: 凯
# portrait: kai
我的手……有点划伤。
* [怎么划伤的？]
    # speaker: 凯
    # portrait: kai
    # clue: CLUE_KAI_HANDS_01,凯手上的伤口
    不记得了。可能是……玻璃？
    # speaker: 凯
    （吧台那只破碎的酒杯。）
    -> END
* [不追问]
    -> END

=== clue_kai_data ===
# speaker: 凯
# portrait: kai
# clue: CLUE_KAI_MOTIVE_01,凯的作案动机
林手里有一份数据。关于我的数据。
# speaker: 凯
如果那份数据流出去……我的一切都会毁掉。
# speaker: 凯
（他停顿了很久。）
# speaker: 凯
但我没有杀他。
-> END

// ══════════════════════════════════════════
//  凯视角结尾
// ══════════════════════════════════════════

=== kai_ending_fragment ===
# speaker: 凯
# portrait: kai
那晚之后，我一直在想——
# speaker: 凯
如果我当时做了不同的选择……
# speaker: 凯
（记忆在这里彻底断裂。）
-> END
