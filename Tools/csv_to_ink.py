#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
csv_to_ink.py — Excel对话表 → Ink故事文件 转换工具

用法：
  python csv_to_ink.py 输入.csv 输出.ink

示例：
  python csv_to_ink.py dialogue.csv ../Assets/Ink/Stories/Dialogue.ink

CSV列名约定（第一行为表头）：
  对话序号  — 对话段落ID，同一序号的行合并为一个knot
  角色      — 说话人名称（对应 # speaker tag）
  对话内容  — 对话文字
  文本状态  — begin/normal/end（可选，转换时忽略）

生成的knot名称规则：
  "对话0"     → dialogue_0
  "对话12"    → dialogue_12
  无数字序号  → dialogue_0, dialogue_1, ...（按出现顺序）
"""

import csv
import re
import sys
import os
from collections import OrderedDict


def sanitize_knot_name(name: str, index: int) -> str:
    """将对话序号转换为合法的Ink knot名称（只允许字母/数字/下划线）"""
    numbers = re.findall(r'\d+', name)
    if numbers:
        return f'dialogue_{numbers[0]}'
    return f'dialogue_{index}'


def csv_to_ink(csv_path: str, output_path: str):
    if not os.path.exists(csv_path):
        print(f'错误：找不到文件 {csv_path}')
        sys.exit(1)

    dialogues = OrderedDict()  # {序号原文: [{'speaker': str, 'content': str}]}

    with open(csv_path, encoding='utf-8-sig') as f:
        reader = csv.DictReader(f)
        for row in reader:
            seq     = row.get('对话序号', '').strip()
            speaker = row.get('角色',     '').strip()
            content = row.get('对话内容', '').strip()

            # 跳过空行
            if not seq or not content:
                continue

            if seq not in dialogues:
                dialogues[seq] = []
            dialogues[seq].append({'speaker': speaker, 'content': content})

    if not dialogues:
        print('警告：未读取到任何对话数据，请检查CSV列名是否正确。')
        print('  期望列名：对话序号 / 角色 / 对话内容')
        sys.exit(1)

    # 生成 .ink 内容
    lines = [
        '// 由 csv_to_ink.py 自动生成，请勿手动修改',
        f'// 源文件：{os.path.basename(csv_path)}',
        '',
    ]

    for i, (seq, rows) in enumerate(dialogues.items()):
        knot_name = sanitize_knot_name(seq, i)
        lines.append(f'// {seq}')
        lines.append(f'=== {knot_name} ===')
        for row in rows:
            if row['speaker']:
                lines.append(f'# speaker: {row["speaker"]}')
            lines.append(row['content'])
        lines.append('-> END')
        lines.append('')

    # 确保输出目录存在
    out_dir = os.path.dirname(output_path)
    if out_dir and not os.path.exists(out_dir):
        os.makedirs(out_dir)

    with open(output_path, 'w', encoding='utf-8') as f:
        f.write('\n'.join(lines))

    total_lines = sum(len(v) for v in dialogues.values())
    print(f'OK: {output_path}')
    print(f'  {len(dialogues)} segments, {total_lines} lines')


if __name__ == '__main__':
    if len(sys.argv) < 3:
        print('用法：python csv_to_ink.py 输入.csv 输出.ink')
        print('示例：python csv_to_ink.py dialogue.csv ../Assets/Ink/Stories/Dialogue.ink')
        sys.exit(1)
    csv_to_ink(sys.argv[1], sys.argv[2])
