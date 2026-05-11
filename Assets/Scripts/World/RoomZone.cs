using UnityEngine;

/// <summary>
/// 房间区域触发器。
/// 挂载在每个房间的触发器Collider2D上，玩家进入时通知RoomManager。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class RoomZone : MonoBehaviour
{
    [Header("房间信息")]
    [Tooltip("房间唯一ID（如 ROOM_DANCE_FLOOR、ROOM_BAR、ROOM_VIP）")]
    [SerializeField] private string _roomId = "ROOM_MAIN";
    [Tooltip("房间显示名称（进入时显示在UI上）")]
    [SerializeField] private string _roomName = "主厅";

    /// <summary>房间唯一ID。</summary>
    public string RoomId => _roomId;
    /// <summary>房间显示名称。</summary>
    public string RoomName => _roomName;
    /// <summary>房间区域Collider2D。</summary>
    public Collider2D RoomCollider => GetComponent<Collider2D>();

    private void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (RoomManager.Instance != null)
            RoomManager.Instance.OnPlayerEnterRoom(_roomId, _roomName);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (RoomManager.Instance != null)
            RoomManager.Instance.OnPlayerExitRoom(_roomId);
    }

#if UNITY_EDITOR
    // 编辑器下显示房间名称，方便场景搭建
    private void OnDrawGizmos()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null) return;

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.15f);
        Gizmos.DrawCube(transform.position, col.bounds.size);
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.6f);
        Gizmos.DrawWireCube(transform.position, col.bounds.size);

        UnityEditor.Handles.Label(
            transform.position,
            $"[{_roomName}]",
            new GUIStyle { normal = { textColor = new Color(0.2f, 0.8f, 1f) }, fontSize = 11 }
        );
    }
#endif
}
