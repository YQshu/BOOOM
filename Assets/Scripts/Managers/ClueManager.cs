using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 线索管理器。
/// 负责统一记录、去重和查询已收集线索，为后续周目与结局系统提供数据基础。
/// </summary>
public class ClueManager : Singleton<ClueManager>
{
    /// <summary>
    /// 线索首次收集事件。
    /// 参数：线索ID、线索名称。
    /// </summary>
    public event Action<string, string> OnClueCollected;

    [Header("调试选项")]
    [Tooltip("是否在收集和查询时输出调试日志")]
    [SerializeField] private bool _enableLog = true;

    [Header("初始化线索")]
    [Tooltip("场景启动时预置为已收集状态的线索ID列表")]
    [SerializeField] private List<string> _preCollectedClueIds = new List<string>();

    private readonly HashSet<string> _collectedClueIds = new HashSet<string>();
    private readonly Dictionary<string, string> _clueNameMap = new Dictionary<string, string>();

    private new void Awake()
    {
        base.Awake();
        InitializePreCollectedClues();
    }

    /// <summary>
    /// 收集一条线索并执行去重。
    /// </summary>
    /// <param name="clueId">线索ID。</param>
    /// <param name="clueName">线索名称。</param>
    /// <returns>首次收集返回 true，重复收集返回 false。</returns>
    public bool CollectClue(string clueId, string clueName)
    {
        if (string.IsNullOrWhiteSpace(clueId))
        {
            Debug.LogWarning("[Clue] 收集失败：clueId 为空。", this);
            return false;
        }

        if (_collectedClueIds.Contains(clueId))
        {
            if (_enableLog)
            {
                Debug.Log($"[Clue] 重复线索已忽略：{clueId}", this);
            }

            return false;
        }

        _collectedClueIds.Add(clueId);
        _clueNameMap[clueId] = clueName;

        if (_enableLog)
        {
            Debug.Log($"[Clue] 已收集：{clueId} - {clueName}", this);
        }

        OnClueCollected?.Invoke(clueId, clueName);
        return true;
    }

    /// <summary>
    /// 判断指定线索是否已经收集。
    /// </summary>
    /// <param name="clueId">线索ID。</param>
    /// <returns>已收集返回 true，否则返回 false。</returns>
    public bool HasClue(string clueId)
    {
        if (string.IsNullOrWhiteSpace(clueId))
        {
            return false;
        }

        return _collectedClueIds.Contains(clueId);
    }

    /// <summary>
    /// 获取当前已收集线索数量。
    /// </summary>
    /// <returns>已收集线索总数。</returns>
    public int GetCollectedCount()
    {
        return _collectedClueIds.Count;
    }

    /// <summary>
    /// 获取所有已收集线索ID的只读副本。
    /// </summary>
    /// <returns>线索ID列表副本。</returns>
    public List<string> GetCollectedClueIds()
    {
        return new List<string>(_collectedClueIds);
    }

    /// <summary>
    /// 根据线索ID获取线索名称。
    /// </summary>
    /// <param name="clueId">线索ID。</param>
    /// <returns>若存在返回线索名称，否则返回空字符串。</returns>
    public string GetClueName(string clueId)
    {
        if (string.IsNullOrWhiteSpace(clueId))
        {
            return string.Empty;
        }

        return _clueNameMap.TryGetValue(clueId, out string clueName) ? clueName : string.Empty;
    }

    /// <summary>
    /// 清空当前已收集线索数据。
    /// </summary>
    public void ClearAllClues()
    {
        _collectedClueIds.Clear();
        _clueNameMap.Clear();

        if (_enableLog)
        {
            Debug.Log("[Clue] 已清空全部线索。", this);
        }
    }

    /// <summary>
    /// 根据预置列表初始化线索状态。
    /// </summary>
    private void InitializePreCollectedClues()
    {
        for (int i = 0; i < _preCollectedClueIds.Count; i++)
        {
            string clueId = _preCollectedClueIds[i];
            if (string.IsNullOrWhiteSpace(clueId))
            {
                continue;
            }

            _collectedClueIds.Add(clueId);
            if (!_clueNameMap.ContainsKey(clueId))
            {
                _clueNameMap[clueId] = clueId;
            }
        }

        if (_enableLog && _preCollectedClueIds.Count > 0)
        {
            Debug.Log($"[Clue] 预置线索初始化完成，数量：{_collectedClueIds.Count}", this);
        }
    }
}
