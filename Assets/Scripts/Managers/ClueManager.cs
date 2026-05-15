using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 线索管理器。
/// 统一记录、去重、查询已收集线索。
/// 支持两种收集方式：
///   1. CollectClue(ClueDataSO) — 直接引用 SO（推荐，可交互物体 / Signal）
///   2. CollectClue(string id)  — 仅传 ID（Ink tag 触发，运行时查表）
/// </summary>
public class ClueManager : Singleton<ClueManager>
{
    /// <summary>线索首次收集时触发，参数为线索SO。</summary>
    public event Action<ClueDataSO> OnClueCollected;

    [Header("线索数据库")]
    [Tooltip("项目中所有 ClueDataSO 资产，用于 Ink tag 的 ID 查表")]
    [SerializeField] private List<ClueDataSO> _clueDatabase = new List<ClueDataSO>();

    [Header("调试")]
    [SerializeField] private bool _enableLog = true;

    private readonly HashSet<string> _collectedIds = new HashSet<string>();
    private readonly Dictionary<string, ClueDataSO> _idToSO = new Dictionary<string, ClueDataSO>();

    [Header("运行时只读（调试用）")]
    [SerializeField] private List<string> _collectedIdsList = new List<string>();

    protected override void Awake()
    {
        base.Awake();
        BuildLookup();
    }

    // ─── 公开接口 ────────────────────────────────────────────

    /// <summary>
    /// 通过 SO 直接收集（推荐方式）。
    /// </summary>
    public bool CollectClue(ClueDataSO clue)
    {
        if (clue == null) return false;
        return Collect(clue);
    }

    /// <summary>
    /// 通过 ID 收集（Ink tag 使用）。运行时查表找到对应 SO。
    /// </summary>
    public bool CollectClue(string clueId)
    {
        if (string.IsNullOrWhiteSpace(clueId)) return false;

        if (!_idToSO.TryGetValue(clueId.Trim(), out ClueDataSO so))
        {
            Debug.LogWarning($"[Clue] 找不到 ID 对应的 ClueDataSO：'{clueId}'，请确认已加入 ClueManager._clueDatabase。");
            return false;
        }

        return Collect(so);
    }

    /// <summary>判断是否已收集。</summary>
    public bool HasClue(string clueId) => _collectedIds.Contains(clueId);

    /// <summary>判断是否已收集。</summary>
    public bool HasClue(ClueDataSO clue) => clue != null && _collectedIds.Contains(clue.clueId);

    /// <summary>获取已收集数量。</summary>
    public int GetCollectedCount() => _collectedIds.Count;

    /// <summary>获取已收集 ID 列表副本。</summary>
    public List<string> GetCollectedClueIds() => new List<string>(_collectedIds);

    /// <summary>获取完整线索数据库（只读）。</summary>
    public List<ClueDataSO> GetAllClues() => _clueDatabase;

    /// <summary>获取 ID 对应的 SO（未找到返回 null）。</summary>
    public ClueDataSO GetClueSOById(string clueId)
    {
        _idToSO.TryGetValue(clueId, out ClueDataSO so);
        return so;
    }

    /// <summary>清空所有已收集线索。</summary>
    public void ClearAllClues()
    {
        _collectedIds.Clear();
        _collectedIdsList.Clear();
        if (_enableLog) Debug.Log("[Clue] 已清空全部线索。");
    }

    /// <summary>
    /// 批量加载线索（存档恢复时调用）。
    /// 清空现有线索，批量添加存档中的线索ID。
    /// </summary>
    public void LoadClues(List<string> clueIds)
    {
        if (clueIds == null)
        {
            Debug.LogWarning("[Clue] LoadClues 参数为 null");
            return;
        }

        // 清空现有线索
        _collectedIds.Clear();
        _collectedIdsList.Clear();

        // 批量添加（不触发事件，避免重复保存）
        int loadedCount = 0;
        foreach (string clueId in clueIds)
        {
            if (string.IsNullOrWhiteSpace(clueId)) continue;

            // 验证线索ID是否存在于数据库
            if (!_idToSO.ContainsKey(clueId))
            {
                Debug.LogWarning($"[Clue] 存档中的线索ID不存在于数据库：{clueId}");
                continue;
            }

            if (!_collectedIds.Contains(clueId))
            {
                _collectedIds.Add(clueId);
                _collectedIdsList.Add(clueId);
                loadedCount++;
            }
        }

        if (_enableLog)
            Debug.Log($"[Clue] 已加载 {loadedCount} 条线索（总计 {_collectedIds.Count} 条）");
    }

    /// <summary>
    /// 【测试用】一键解锁所有线索。
    /// </summary>
    public void UnlockAllClues()
    {
        if (_clueDatabase == null || _clueDatabase.Count == 0)
        {
            Debug.LogWarning("[Clue] 线索数据库为空，无法解锁。");
            return;
        }

        int unlocked = 0;
        foreach (ClueDataSO clue in _clueDatabase)
        {
            if (clue == null) continue;
            if (CollectClue(clue))
                unlocked++;
        }

        Debug.Log($"[Clue] 测试模式：已解锁 {unlocked} 条线索（总计 {_collectedIds.Count} 条）");
    }

    // ─── 内部 ────────────────────────────────────────────────

    private bool Collect(ClueDataSO clue)
    {
        if (_collectedIds.Contains(clue.clueId))
        {
            if (_enableLog) Debug.Log($"[Clue] 重复，已忽略：{clue.clueId}");
            return false;
        }

        _collectedIds.Add(clue.clueId);
        _collectedIdsList.Add(clue.clueId);

        if (_enableLog) Debug.Log($"[Clue] 已收集：{clue.clueId}");

        // 触发事件
        if (_enableLog)
        {
            int subscriberCount = OnClueCollected?.GetInvocationList().Length ?? 0;
            Debug.Log($"[Clue] ★ 触发 OnClueCollected 事件，订阅者数量: {subscriberCount}");
        }

        OnClueCollected?.Invoke(clue);
        return true;
    }

    private void BuildLookup()
    {
        _idToSO.Clear();
        foreach (ClueDataSO so in _clueDatabase)
        {
            if (so == null || string.IsNullOrWhiteSpace(so.clueId)) continue;
            if (_idToSO.ContainsKey(so.clueId))
            {
                Debug.LogWarning($"[Clue] 重复 clueId：{so.clueId}，请检查 ClueDatabase。");
                continue;
            }
            _idToSO[so.clueId] = so;
        }
    }
}
