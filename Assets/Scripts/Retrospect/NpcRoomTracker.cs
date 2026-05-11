using System;
using UnityEngine;

/// <summary>
/// NPC房间追踪器。
/// 追踪由Timeline驱动的NPC当前所在RoomZone，供PlayerMovementConstraint使用。
/// </summary>
public class NpcRoomTracker : MonoBehaviour
{
    /// <summary>NPC切换房间时触发，传递新房间的Collider2D。</summary>
    public event Action<Collider2D> OnNpcRoomChanged;

    [Header("配置")]
    [Tooltip("检测间隔（秒），无需每帧检测")]
    [SerializeField] private float _checkInterval = 0.2f;
    [Tooltip("RoomZone所在的Layer（用于OverlapPoint过滤）")]
    [SerializeField] private LayerMask _roomLayer = ~0;

    /// <summary>当前NPC所在房间的Collider2D。</summary>
    public Collider2D CurrentRoomBounds { get; private set; }
    /// <summary>当前房间ID。</summary>
    public string CurrentRoomId { get; private set; }

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
        CurrentRoomBounds = null;
        CurrentRoomId = "";

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
        CurrentRoomBounds = null;
        CurrentRoomId = "";
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

        // 检测NPC位置落在哪个RoomZone内
        Collider2D hit = Physics2D.OverlapPoint(npcPos, _roomLayer);

        if (hit == null) return;

        // 检查是否为RoomZone
        RoomZone roomZone = hit.GetComponent<RoomZone>();
        if (roomZone == null) return;

        // 房间未变化则跳过
        if (hit == CurrentRoomBounds) return;

        // 房间变化
        CurrentRoomBounds = hit;
        CurrentRoomId = roomZone.RoomId;
        OnNpcRoomChanged?.Invoke(hit);
        Debug.Log($"[Rewind] NPC进入房间：{CurrentRoomId}");
    }
}
