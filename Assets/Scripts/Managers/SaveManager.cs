using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 存档管理器。
/// 负责游戏进度的保存、加载、自动保存触发。
/// </summary>
public class SaveManager : Singleton<SaveManager>
{
    /// <summary>保存开始事件。</summary>
    public event Action OnSaveStart;

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
    private bool _isSubscribedToClueEvent = false;
    private bool _isSubscribedToDialogueEvent = false;
    private bool _hasPendingSave = false; // 是否有待保存的内容

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);

        // 监听场景加载事件
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        if (_enableLog)
            Debug.Log("[Save] ========== SaveManager.Start() ==========");

        // 延迟订阅，确保 ClueManager 已初始化
        StartCoroutine(DelayedSubscribe());

        // 启动自动保存
        if (_enableAutoSave)
        {
            StartAutoSave();
            if (_enableLog)
                Debug.Log("[Save] 自动保存已启动");
        }

        if (_enableLog)
            Debug.Log($"[Save] 存档路径：{SaveFilePath}");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 每次场景加载后，尝试重新订阅事件
        if (_enableLog)
            Debug.Log($"[Save] 场景加载：{scene.name}，尝试订阅事件");

        StartCoroutine(DelayedSubscribe());
    }

    private IEnumerator DelayedSubscribe()
    {
        // 等待一帧，确保所有单例初始化完成
        yield return null;

        // 订阅线索收集事件
        if (_saveOnClueCollected)
        {
            TrySubscribeToClueManager();
        }

        // 订阅对话结束事件
        TrySubscribeToDialogueManager();
    }

    private void TrySubscribeToClueManager()
    {
        if (_enableLog)
            Debug.Log($"[Save] TrySubscribeToClueManager 被调用 - 已订阅状态: {_isSubscribedToClueEvent}");

        if (_isSubscribedToClueEvent)
        {
            if (_enableLog)
                Debug.Log("[Save] 已经订阅过了，跳过");
            return;
        }

        if (ClueManager.Instance != null)
        {
            ClueManager.Instance.OnClueCollected += OnClueCollected;
            _isSubscribedToClueEvent = true;

            if (_enableLog)
                Debug.Log("[Save] ✓ 成功订阅 ClueManager.OnClueCollected 事件");
        }
        else
        {
            if (_enableLog)
                Debug.LogWarning("[Save] ✗ ClueManager.Instance 为 null，无法订阅事件");
        }
    }

    private void TrySubscribeToDialogueManager()
    {
        if (_isSubscribedToDialogueEvent)
            return;

        if (InkDialogueManager.Instance != null)
        {
            InkDialogueManager.Instance.OnDialogueEnd += OnDialogueEnd;
            _isSubscribedToDialogueEvent = true;

            if (_enableLog)
                Debug.Log("[Save] ✓ 成功订阅 InkDialogueManager.OnDialogueEnd 事件");
        }
        else
        {
            if (_enableLog)
                Debug.LogWarning("[Save] ✗ InkDialogueManager.Instance 为 null，无法订阅对话结束事件");
        }
    }

    private void OnDestroy()
    {
        // 取消监听场景加载事件
        SceneManager.sceneLoaded -= OnSceneLoaded;

        // 取消订阅线索事件
        if (_isSubscribedToClueEvent && ClueManager.Instance != null)
        {
            ClueManager.Instance.OnClueCollected -= OnClueCollected;
            _isSubscribedToClueEvent = false;
        }

        // 取消订阅对话事件
        if (_isSubscribedToDialogueEvent && InkDialogueManager.Instance != null)
        {
            InkDialogueManager.Instance.OnDialogueEnd -= OnDialogueEnd;
            _isSubscribedToDialogueEvent = false;
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
            // 触发保存开始事件
            OnSaveStart?.Invoke();

            SaveData data = GetCurrentSaveData();
            WriteSaveFile(data);

            if (_enableLog)
                Debug.Log($"[Save] 保存成功：{data.collectedClueIds.Count} 条线索");

            // 触发保存完成事件
            OnSaveComplete?.Invoke();

            // 清除待保存标记
            _hasPendingSave = false;
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
        if (_enableLog)
            Debug.Log($"[Save] ★ OnClueCollected 事件触发！线索: {clue?.clueId ?? "null"}");

        if (!_saveOnClueCollected)
        {
            if (_enableLog)
                Debug.Log("[Save] _saveOnClueCollected 为 false，跳过自动保存");
            return;
        }

        if (ShouldAutoSave())
        {
            SaveGame();
            if (_enableLog)
                Debug.Log($"[Save] 自动保存触发（线索：{clue.clueId}）");
        }
        else
        {
            // 标记有待保存的内容，等待对话结束后保存
            _hasPendingSave = true;
            if (_enableLog)
                Debug.Log("[Save] 当前无法保存（对话中/回溯中），标记为待保存");
        }
    }

    /// <summary>
    /// 对话结束事件处理。
    /// 如果有待保存的内容，则触发保存。
    /// </summary>
    private void OnDialogueEnd()
    {
        if (_enableLog)
            Debug.Log($"[Save] 对话结束，待保存标记: {_hasPendingSave}");

        if (_hasPendingSave)
        {
            // 延迟一帧保存，确保对话完全结束
            StartCoroutine(DelayedSave());
        }
    }

    private IEnumerator DelayedSave()
    {
        yield return null;

        if (_hasPendingSave && ShouldAutoSave())
        {
            SaveGame();
            if (_enableLog)
                Debug.Log("[Save] 对话结束后自动保存");
        }
    }

    private bool ShouldAutoSave()
    {
        // 回溯模式中不保存
        if (RetrospectManager.Instance != null && RetrospectManager.Instance.IsInRetrospect)
        {
            if (_enableLog)
                Debug.Log("[Save] 跳过保存：正在回溯模式中");
            return false;
        }

        // 对话中不保存
        if (InkDialogueManager.Instance != null && InkDialogueManager.Instance.IsPlaying)
        {
            if (_enableLog)
                Debug.Log("[Save] 跳过保存：正在播放对话");
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
