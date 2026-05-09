using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 角色数据
/// </summary>
[Serializable]
public class CharacterData
{
    public string characterName;     // 角色名称
    public Sprite avatar;           // 角色头像
    public string description;      // 角色描述
    public Color themeColor = Color.white; // 主题色（用于UI）
}

/// <summary>
/// 线索数据
/// </summary>
public class ClueData
{
    public string clueId;            // 线索唯一ID（为空时回退到自动生成ID）
    public int characterIndex;       // 属于哪个角色
    public string time;              // 时间（如"00:05"）
    public string clueText;          // 线索文本
    public Sprite icon;              // 线索图标（可选）
    public bool isImportant = false; // 是否重要线索
}

/// <summary>
/// 游戏数据管理器（可选，用于数据持久化）
/// </summary>
public class GameDataManager : MonoBehaviour
{
    private static GameDataManager instance;

    public static GameDataManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject obj = new GameObject("GameDataManager");
                instance = obj.AddComponent<GameDataManager>();
                DontDestroyOnLoad(obj);
            }
            return instance;
        }
    }

    // 保存的游戏数据
    [Serializable]
    public class SaveData
    {
        public List<string> unlockedClues = new List<string>();
        public List<string> viewedClues = new List<string>();
        public Dictionary<string, float> clueUnlockTimes = new Dictionary<string, float>();
    }

    private SaveData currentSaveData = new SaveData();

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        LoadGameData();
    }

    /// <summary>
    /// 保存游戏数据
    /// </summary>
    public void SaveGameData()
    {
        string json = JsonUtility.ToJson(currentSaveData);
        PlayerPrefs.SetString("GameData", json);
        PlayerPrefs.Save();

        Debug.Log("游戏数据已保存");
    }

    /// <summary>
    /// 加载游戏数据
    /// </summary>
    public void LoadGameData()
    {
        if (PlayerPrefs.HasKey("GameData"))
        {
            string json = PlayerPrefs.GetString("GameData");
            currentSaveData = JsonUtility.FromJson<SaveData>(json);
            Debug.Log("游戏数据已加载");
        }
        else
        {
            Debug.Log("无保存数据，创建新数据");
            currentSaveData = new SaveData();
        }
    }

    /// <summary>
    /// 添加新线索
    /// </summary>
    public void AddNewClue(string clueId)
    {
        if (!currentSaveData.unlockedClues.Contains(clueId))
        {
            currentSaveData.unlockedClues.Add(clueId);
            currentSaveData.clueUnlockTimes[clueId] = Time.time;
            SaveGameData();
        }
    }

    /// <summary>
    /// 标记线索为已查看
    /// </summary>
    public void MarkClueAsViewed(string clueId)
    {
        if (!currentSaveData.viewedClues.Contains(clueId))
        {
            currentSaveData.viewedClues.Add(clueId);
            SaveGameData();
        }
    }

    /// <summary>
    /// 检查线索是否已解锁
    /// </summary>
    public bool IsClueUnlocked(string clueId)
    {
        return currentSaveData.unlockedClues.Contains(clueId);
    }

    /// <summary>
    /// 检查线索是否是新线索
    /// </summary>
    public bool IsClueNew(string clueId)
    {
        if (currentSaveData.clueUnlockTimes.ContainsKey(clueId))
        {
            float unlockTime = currentSaveData.clueUnlockTimes[clueId];
            return (Time.time - unlockTime) <= 5f; // 5秒内为新线索
        }
        return false;
    }
}
