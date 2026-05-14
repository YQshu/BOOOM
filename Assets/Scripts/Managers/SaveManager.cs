using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 存档管理器。
/// 负责游戏进度的保存、加载、自动保存触发。
/// </summary>
public class SaveManager : Singleton<SaveManager>
{
    /// <summary>保存完成事件。</summary>
    public event Action OnSaveComplete;

    /// <summary>加载完成事件。</summary>
    public event Action OnLoadComplete;

    /// <summary>保存/加载错误事件，参数为错误信息。</summary>
    public event Action<string> OnSaveError;

    [Header("自动保存配置")]
    [Tooltip("是否启用自动保存")]
    [SerializeField] private bool _enableAutoSave = true;
    [Tooltip("自动保存间隔（分钟）")]
    [SerializeField] private float _autoSaveIntervalMinutes = 5f;
    [Tooltip("收集新线索时自动保存")]
    [SerializeField] private bool _saveOnClueCollected = true;

    [Header("调试")]
    [SerializeField] private bool _enableLog = true;

    private string SaveFilePath => Path.Combine(Application.persistentDataPath, "gamesave.json");

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // 订阅线索收集事件
        if (_saveOnClueCollected && ClueManager.Instance != null)
        {
            ClueManager.Instance.OnClueCollected += OnClueCollected;
        }

        // 启动自动保存
        if (_enableAutoSave)
        {
            StartAutoSave();
        }

        if (_enableLog)
            Debug.Log($"[Save] 存档路径：{SaveFilePath}");
    }

    private void OnDestroy()
    {
        // 取消订阅
        if (ClueManager.Instance != null)
        {
            ClueManager.Instance.OnClueCollected -= OnClueCollected;
        }
    }

    // ─── 公开接口 ────────────────────────────────────────────

    /// <summary>
    /// 保存游戏进度。
    /// </summary>
    public void SaveGame()
    {
        try
        {
            SaveData data = GetCurrentSaveData();
            WriteSaveFile(data);

            if (_enableLog)
                Debug.Log($"[Save] 保存成功：{data.collectedClueIds.Count} 条线索");

            OnSaveComplete?.Invoke();
        }
        catch (Exception ex)
        {
            string errorMsg = $"保存失败：{ex.Message}";
            Debug.LogError($"[Save] {errorMsg}");
            OnSaveError?.Invoke(errorMsg);
        }
    }

    /// <summary>
    /// 加载游戏进度。
    /// </summary>
    public void LoadGame()
    {
        try
        {
            SaveData data = ReadSaveFile();
            if (data == null)
            {
                Debug.LogWarning("[Save] 存档文件不存在或损坏");
                return;
            }

            // 版本检查
            if (data.saveVersion != 1)
            {
                Debug.LogWarning($"[Save] 存档版本不匹配（存档：{data.saveVersion}，当前：1），尝试兼容加载");
            }

            // 恢复线索
            if (ClueManager.Instance != null)
            {
                ClueManager.Instance.LoadClues(data.collectedClueIds);
            }

            // 恢复玩家位置
            if (data.playerPosition != null)
            {
                PlayerController player = FindObjectOfType<PlayerController>();
                if (player != null)
                {
                    player.LoadPosition(data.playerPosition.ToVector2());
                }
            }

            // 恢复房间ID
            if (!string.IsNullOrEmpty(data.currentRoomId) && RoomManager.Instance != null)
            {
                RoomManager.Instance.LoadRoomId(data.currentRoomId);
            }

            if (_enableLog)
                Debug.Log($"[Save] 加载成功：{data.collectedClueIds.Count} 条线索，时间戳：{data.saveTimestamp}");

            OnLoadComplete?.Invoke();
        }
        catch (Exception ex)
        {
            string errorMsg = $"加载失败：{ex.Message}";
            Debug.LogError($"[Save] {errorMsg}");
            OnSaveError?.Invoke(errorMsg);
        }
    }

    /// <summary>
    /// 检查存档文件是否存在。
    /// </summary>
    public bool HasSaveFile()
    {
        return File.Exists(SaveFilePath);
    }

    /// <summary>
    /// 删除存档文件。
    /// </summary>
    public void DeleteSave()
    {
        try
        {
            if (File.Exists(SaveFilePath))
            {
                File.Delete(SaveFilePath);
                if (_enableLog)
                    Debug.Log("[Save] 存档已删除");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Save] 删除存档失败：{ex.Message}");
        }
    }

    // ─── 自动保存 ────────────────────────────────────────────

    private void StartAutoSave()
    {
        StartCoroutine(AutoSaveRoutine());
    }

    private IEnumerator AutoSaveRoutine()
    {
        float intervalSeconds = _autoSaveIntervalMinutes * 60f;

        while (true)
        {
            yield return new WaitForSeconds(intervalSeconds);

            if (ShouldAutoSave())
            {
                SaveGame();
                if (_enableLog)
                    Debug.Log("[Save] 自动保存触发（定时）");
            }
        }
    }

    private void OnClueCollected(ClueDataSO clue)
    {
        if (!_saveOnClueCollected) return;

        if (ShouldAutoSave())
        {
            SaveGame();
            if (_enableLog)
                Debug.Log($"[Save] 自动保存触发（线索：{clue.clueId}）");
        }
    }

    private bool ShouldAutoSave()
    {
        // 回溯模式中不保存
        if (RetrospectManager.Instance != null && RetrospectManager.Instance.IsInRetrospect)
        {
            return false;
        }

        // 对话中不保存
        if (InkDialogueManager.Instance != null && InkDialogueManager.Instance.IsPlaying)
        {
            return false;
        }

        return true;
    }

    // ─── 内部方法 ────────────────────────────────────────────

    private SaveData GetCurrentSaveData()
    {
        SaveData data = new SaveData
        {
            saveVersion = 1,
            saveTimestamp = DateTime.Now.ToString("o") // ISO 8601 格式
        };

        // 收集线索
        if (ClueManager.Instance != null)
        {
            data.collectedClueIds = ClueManager.Instance.GetCollectedClueIds();
        }

        // 收集玩家位置
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            data.playerPosition = new Vector2Data(player.GetPosition());
        }

        // 收集房间ID
        if (RoomManager.Instance != null)
        {
            data.currentRoomId = RoomManager.Instance.GetCurrentRoomId();
        }

        return data;
    }

    private void WriteSaveFile(SaveData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SaveFilePath, json);
    }

    private SaveData ReadSaveFile()
    {
        if (!File.Exists(SaveFilePath))
            return null;

        try
        {
            string json = File.ReadAllText(SaveFilePath);
            return JsonUtility.FromJson<SaveData>(json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Save] 读取存档文件失败：{ex.Message}");
            return null;
        }
    }
}
