using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// NPC房间追踪器。
/// 追踪由Timeline驱动的NPC当前所在RoomZone，供PlayerMovementConstraint使用。
/// 支持NPC同时处于多个房间交界处。
/// </summary>
public class NpcRoomTracker : MonoBehaviour
{
    /// <summary>NPC切换房间时触发，传递新房间的Collider2D列表。</summary>
    public event Action<List<Collider2D>> OnNpcRoomChanged;

    [Header("配置")]
    [Tooltip("检测间隔（秒），无需每帧检测")]
    [SerializeField] private float _checkInterval = 0.2f;
    [Tooltip("RoomZone所在的Layer（用于OverlapPoint过滤）")]
    [SerializeField] private LayerMask _roomLayer = ~0;

    /// <summary>当前NPC所在的所有房间的Collider2D列表（支持交界处多房间）。</summary>
    public List<Collider2D> CurrentRoomBounds { get; private set; } = new List<Collider2D>();
    /// <summary>当前房间ID列表（用逗号分隔）。</summary>
    public string CurrentRoomIds { get; private set; }

    private Transform _npcTransform;
    private float _nextCheckTime;
    private bool _isBound;

    // ─── 公开接口 ────────────────────────────────────────────

    /// <summary>
    /// 绑定要追踪的NPC Transform（回溯开始时调用）。
    /// </summary>
    public void BindNpc(Transform npcTransform)
    {
        _npcTransform = npcTransform;
        _isBound = npcTransform != null;
        _nextCheckTime = 0f;
        CurrentRoomBounds.Clear();
        CurrentRoomIds = "";

        if (_isBound)
            CheckNpcRoom(); // 立即检测一次
    }

    /// <summary>
    /// 解除绑定（回溯结束时调用）。
    /// </summary>
    public void Unbind()
    {
        _npcTransform = null;
        _isBound = false;
        CurrentRoomBounds.Clear();
        CurrentRoomIds = "";
    }

    // ─── 内部逻辑 ────────────────────────────────────────────

    private void Update()
    {
        if (!_isBound || _npcTransform == null) return;

        if (Time.time >= _nextCheckTime)
        {
            _nextCheckTime = Time.time + _checkInterval;
            CheckNpcRoom();
        }
    }

    private void CheckNpcRoom()
    {
        Vector2 npcPos = _npcTransform.position;

        // 检测NPC位置落在哪些RoomZone内（支持多个重叠房间）
        Collider2D[] hits = Physics2D.OverlapPointAll(npcPos, _roomLayer);

        // 过滤出有效的RoomZone
        List<Collider2D> validRooms = new List<Collider2D>();
        List<string> roomIds = new List<string>();

        foreach (var hit in hits)
        {
            RoomZone roomZone = hit.GetComponent<RoomZone>();
            if (roomZone != null)
            {
                validRooms.Add(hit);
                roomIds.Add(roomZone.RoomId);
            }
        }

        // 检查房间列表是否变化
        if (RoomListEquals(validRooms, CurrentRoomBounds))
            return;

        // 房间变化
        CurrentRoomBounds = validRooms;
        CurrentRoomIds = string.Join(", ", roomIds);
        OnNpcRoomChanged?.Invoke(validRooms);

        if (validRooms.Count > 1)
            Debug.Log($"[Rewind] NPC处于多个房间交界处：{CurrentRoomIds}");
        else if (validRooms.Count == 1)
            Debug.Log($"[Rewind] NPC进入房间：{CurrentRoomIds}");
        else
            Debug.LogWarning("[Rewind] NPC不在任何房间内");
    }

    /// <summary>
    /// 比较两个房间列表是否相同（顺序无关）。
    /// </summary>
    private bool RoomListEquals(List<Collider2D> list1, List<Collider2D> list2)
    {
        if (list1.Count != list2.Count) return false;
        return list1.All(c => list2.Contains(c)) && list2.All(c => list1.Contains(c));
    }
}
