using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 房间管理器。
/// 追踪玩家当前所在房间，进入新房间时显示房间名称提示（自动淡出）。
/// </summary>
public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance { get; private set; }

    /// <summary>
    /// 房间切换事件，参数：新房间ID、新房间名称。
    /// </summary>
    public event Action<string, string> OnRoomChanged;

    [Header("UI引用")]
    [Tooltip("显示房间名称的TMP_Text组件")]
    [SerializeField] private TMP_Text _roomNameText;
    [Tooltip("控制淡出的CanvasGroup（挂在房间名Text的父物体上）")]
    [SerializeField] private CanvasGroup _roomNameGroup;
    [Tooltip("房间名称显示后多少秒开始淡出")]
    [SerializeField] private float _displayDuration = 2f;
    [Tooltip("淡出持续时间")]
    [SerializeField] private float _fadeDuration = 0.5f;

    private string _currentRoomId = string.Empty;
    private float _displayTimer;
    private bool _isFading;

    public string CurrentRoomId => _currentRoomId;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (_roomNameGroup != null)
            _roomNameGroup.alpha = 0f;
    }

    private void Update()
    {
        if (_roomNameGroup == null || !_isFading) return;

        _displayTimer -= Time.deltaTime;

        if (_displayTimer > 0)
        {
            // 显示阶段：保持不透明
            _roomNameGroup.alpha = 1f;
        }
        else
        {
            // 淡出阶段
            float fadeProgress = Mathf.Clamp01(-_displayTimer / _fadeDuration);
            _roomNameGroup.alpha = 1f - fadeProgress;

            if (fadeProgress >= 1f)
                _isFading = false;
        }
    }

    /// <summary>
    /// 玩家进入房间时由RoomZone调用。
    /// </summary>
    public void OnPlayerEnterRoom(string roomId, string roomName)
    {
        if (_currentRoomId == roomId) return;

        _currentRoomId = roomId;
        OnRoomChanged?.Invoke(roomId, roomName);
        ShowRoomName(roomName);

        Debug.Log($"[Room] 进入：{roomName}（{roomId}）");
    }

    /// <summary>
    /// 玩家离开房间时由RoomZone调用。
    /// </summary>
    public void OnPlayerExitRoom(string roomId)
    {
        // 仅在离开当前房间时清空（防止多区域重叠时误清）
        if (_currentRoomId == roomId)
            _currentRoomId = string.Empty;
    }

    private void ShowRoomName(string roomName)
    {
        if (_roomNameText != null)
            _roomNameText.text = roomName;

        if (_roomNameGroup != null)
        {
            _roomNameGroup.alpha = 1f;
            _displayTimer = _displayDuration;
            _isFading = true;
        }
    }

    /// <summary>
    /// 获取当前房间ID（用于保存）。
    /// </summary>
    public string GetCurrentRoomId()
    {
        return _currentRoomId;
    }

    /// <summary>
    /// 加载房间ID（存档恢复时调用，不触发UI提示）。
    /// </summary>
    public void LoadRoomId(string roomId)
    {
        _currentRoomId = roomId;
        Debug.Log($"[Room] 房间ID已加载：{roomId}");
    }
}
