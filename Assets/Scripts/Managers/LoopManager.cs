using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 周目管理器。
/// 负责维护当前激活的角色周目，并提供周目切换入口。
/// </summary>
public class LoopManager : MonoBehaviour
{
    /// <summary>
    /// 周目切换事件，参数为切换后的周目ID。
    /// </summary>
    public event Action<string> OnLoopChanged;

    [Header("周目配置")]
    [Tooltip("可切换的周目ID列表，按顺序循环切换")]
    [SerializeField] private List<string> _loopIds = new List<string> { "LOOP_SUSPECT_A", "LOOP_SUSPECT_B" };
    [Tooltip("启动时默认激活的周目索引")]
    [SerializeField] private int _defaultLoopIndex;

    [Header("快捷切换按键")]
    [Tooltip("是否启用按键切换周目")]
    [SerializeField] private bool _enableHotkeySwitch = true;
    [Tooltip("切换到下一个周目的按键")]
    [SerializeField] private KeyCode _nextLoopKey = KeyCode.Tab;

    [Header("调试选项")]
    [Tooltip("是否输出周目切换日志")]
    [SerializeField] private bool _enableLog = true;

    private int _currentLoopIndex = -1;

    /// <summary>
    /// 当前激活的周目ID。
    /// </summary>
    public string CurrentLoopId
    {
        get
        {
            if (_currentLoopIndex < 0 || _currentLoopIndex >= _loopIds.Count)
            {
                return string.Empty;
            }

            return _loopIds[_currentLoopIndex];
        }
    }

    private void Start()
    {
        InitializeDefaultLoop();
    }

    private void Update()
    {
        if (!_enableHotkeySwitch)
        {
            return;
        }

        if (Input.GetKeyDown(_nextLoopKey))
        {
            SwitchToNextLoop();
        }
    }

    /// <summary>
    /// 切换到下一个周目。
    /// </summary>
    public void SwitchToNextLoop()
    {
        if (_loopIds.Count == 0)
        {
            Debug.LogWarning("[Loop] 切换失败：未配置任何周目ID。", this);
            return;
        }

        int nextIndex = (_currentLoopIndex + 1) % _loopIds.Count;
        SwitchToLoopByIndex(nextIndex);
    }

    /// <summary>
    /// 按周目ID切换。
    /// </summary>
    /// <param name="loopId">目标周目ID。</param>
    /// <returns>切换成功返回 true，否则返回 false。</returns>
    public bool SwitchToLoopById(string loopId)
    {
        if (string.IsNullOrWhiteSpace(loopId))
        {
            Debug.LogWarning("[Loop] 切换失败：loopId 为空。", this);
            return false;
        }

        int targetIndex = _loopIds.IndexOf(loopId);
        if (targetIndex < 0)
        {
            Debug.LogWarning($"[Loop] 切换失败：未找到周目 {loopId}。", this);
            return false;
        }

        SwitchToLoopByIndex(targetIndex);
        return true;
    }

    /// <summary>
    /// 按索引切换周目。
    /// </summary>
    /// <param name="index">目标周目索引。</param>
    public void SwitchToLoopByIndex(int index)
    {
        if (index < 0 || index >= _loopIds.Count)
        {
            Debug.LogWarning($"[Loop] 切换失败：索引越界 {index}。", this);
            return;
        }

        if (_currentLoopIndex == index)
        {
            return;
        }

        _currentLoopIndex = index;
        string loopId = _loopIds[_currentLoopIndex];

        if (_enableLog)
        {
            Debug.Log($"[Loop] 当前周目：{loopId}", this);
        }

        OnLoopChanged?.Invoke(loopId);
    }

    /// <summary>
    /// 初始化默认周目。
    /// </summary>
    private void InitializeDefaultLoop()
    {
        if (_loopIds.Count == 0)
        {
            Debug.LogWarning("[Loop] 初始化失败：未配置周目ID。", this);
            return;
        }

        int safeIndex = Mathf.Clamp(_defaultLoopIndex, 0, _loopIds.Count - 1);
        SwitchToLoopByIndex(safeIndex);
    }
}
