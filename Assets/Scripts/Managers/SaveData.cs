using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 游戏存档数据结构。
/// 包含所有需要持久化的游戏进度信息。
/// </summary>
[Serializable]
public class SaveData
{
    /// <summary>存档版本号，用于兼容性检查。</summary>
    public int saveVersion = 1;

    /// <summary>保存时间戳（ISO 8601格式）。</summary>
    public string saveTimestamp;

    /// <summary>已收集的线索ID列表。</summary>
    public List<string> collectedClueIds = new List<string>();

    /// <summary>玩家位置。</summary>
    public Vector2Data playerPosition;

    /// <summary>当前房间ID。</summary>
    public string currentRoomId = string.Empty;
}

/// <summary>
/// Vector2 的可序列化包装类。
/// Unity 的 JsonUtility 不支持直接序列化 Vector2。
/// </summary>
[Serializable]
public class Vector2Data
{
    public float x;
    public float y;

    public Vector2Data() { }

    public Vector2Data(Vector2 vector)
    {
        x = vector.x;
        y = vector.y;
    }

    public Vector2 ToVector2()
    {
        return new Vector2(x, y);
    }
}
