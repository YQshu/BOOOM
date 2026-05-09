using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.U2D.Animation;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ClueWallManager : MonoBehaviour
{
    [Header("界面引用")]
    public GameObject mainScreen;           // 主游戏界面
    public GameObject clueWallRoot;         // 线索墙根对象
    public GameObject characterDetailPanel; // 角色详情面板

    [Header("角色卡片")]
    public Button[] characterCardButtons;   // 4个角色卡片按钮

    [Header("角色详情界面元素")]
    public Image characterDetailAvatar;     // 详情界面角色头像
    public TMP_Text characterDetailName;        // 详情界面角色名
    public TMP_Text characterDetailDescription; // 详情界面描述
    public Button backButton;               // 返回按钮

    [Header("线索列表")]
    public GameObject clueItemPrefab;       // 线索条目预制体
    public Transform clueContentParent;     // 线索条目的父对象（ScrollView的Content）
    public ScrollRect clueScrollView;       // 线索滚动视图

    [Header("角色数据")]
    public CharacterData[] characters;      // 角色数据数组
    public ClueData[] clueDatabase;        // 线索数据库

    [Header("退出按钮")]
    public Button exitClueWallButton;

    // 当前选中的角色索引
    private int currentCharacterIndex = -1;

    // 存储每个线索的解锁时间
    private Dictionary<string, float> clueUnlockTimes = new Dictionary<string, float>();

    // 新线索感叹号持续时间（秒）
    private const float NEW_CLUE_DURATION = 5f;

    void Start()
    {
        // 初始化界面状态
        if (mainScreen != null) mainScreen.SetActive(true);
        if (clueWallRoot != null) clueWallRoot.SetActive(false);
        if (characterDetailPanel != null) characterDetailPanel.SetActive(false);

        // 绑定角色卡片点击事件
        for (int i = 0; i < characterCardButtons.Length; i++)
        {
            int index = i; // 闭包问题，需要局部变量
            characterCardButtons[i].onClick.AddListener(() => OnSelectCharacter(index));
        }

        // 绑定返回按钮事件
        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackToCharacterList);
        }

        // 加载已保存的线索解锁时间
        LoadClueUnlockTimes();

        // 检查是否有新线索
        StartCoroutine(CheckNewCluesPeriodically());
    }

    /// <summary>
    /// 从主游戏界面进入线索墙
    /// </summary>
    public void OnEnterClueWall()
    {
        if (mainScreen != null) mainScreen.SetActive(false);
        if (clueWallRoot != null) clueWallRoot.SetActive(true);
        if (characterDetailPanel != null) characterDetailPanel.SetActive(false);

        Debug.Log("进入线索墙界面");
    }

    /// <summary>
    /// 从线索墙返回主游戏界面
    /// </summary>
    public void OnExitClueWall()
    {
        if (clueWallRoot != null) clueWallRoot.SetActive(false);

        Debug.Log("退出线索墙界面");
    }

    /// <summary>
    /// 选择角色
    /// </summary>
    public void OnSelectCharacter(int characterIndex)
    {
        if (characterIndex < 0 || characterIndex >= characters.Length)
        {
            Debug.LogError("无效的角色索引: " + characterIndex);
            return;
        }

        currentCharacterIndex = characterIndex;

        // 切换面板：隐藏列表，显示详情
        if (clueWallRoot != null)
        {
            clueWallRoot.SetActive(false);  // ← 关键：隐藏角色列表
        }

        if (exitClueWallButton != null)
        {
            exitClueWallButton.onClick.AddListener(OnExitClueWall);
        }

        if (characterDetailPanel != null)
        {
            characterDetailPanel.SetActive(true);  // 显示详情
        }

        // 加载角色详情
        LoadCharacterDetail(characterIndex);

        Debug.Log($"选择角色: {characters[characterIndex].characterName}");
    }

    /// <summary>
    /// 从角色详情返回角色选择列表
    /// </summary>
    public void OnBackToCharacterList()
    {
        // 切换面板：隐藏详情，显示列表
        if (characterDetailPanel != null)
        {
            characterDetailPanel.SetActive(false);
        }

        if (clueWallRoot != null)
        {
            clueWallRoot.SetActive(true);  // ← 关键：重新显示角色列表
        }

        Debug.Log("返回角色选择列表");
    }

    /// <summary>
    /// 加载角色详情
    /// </summary>
    private void LoadCharacterDetail(int characterIndex)
    {
        CharacterData character = characters[characterIndex];

        // 更新UI元素
        if (characterDetailAvatar != null && character.avatar != null)
        {
            characterDetailAvatar.sprite = character.avatar;
        }

        if (characterDetailName != null)
        {
            characterDetailName.text = character.characterName;
        }

        if (characterDetailDescription != null)
        {
            characterDetailDescription.text = character.description;
        }

        // 加载该角色的线索列表
        LoadClueListForCharacter(characterIndex);
    }

    /// <summary>
    /// 加载指定角色的线索列表
    /// </summary>
    private void LoadClueListForCharacter(int characterIndex)
    {
        if (clueItemPrefab == null || clueContentParent == null)
        {
            Debug.LogError("线索预制体或父对象未设置");
            return;
        }

        // 清空旧的线索条目
        foreach (Transform child in clueContentParent)
        {
            Destroy(child.gameObject);
        }

        // 获取该角色的所有线索
        List<ClueData> characterClues = GetCluesForCharacter(characterIndex);

        // 按时间排序（假设时间格式为"00:05"这样的字符串）
        characterClues.Sort((a, b) =>
        {
            if (a.time == null || b.time == null) return 0;
            return a.time.CompareTo(b.time);
        });

        // 生成线索条目
        foreach (ClueData clue in characterClues)
        {
            GameObject clueItemObj = Instantiate(clueItemPrefab, clueContentParent);
            ClueItemUI clueItem = clueItemObj.GetComponent<ClueItemUI>();

            if (clueItem != null)
            {
                // 生成唯一的线索ID
                string clueId = $"Character{characterIndex}_Clue{clue.time}";

                // 检查是否是新线索
                float unlockTime = GetClueUnlockTime(clueId);
                bool isNew = (Time.time - unlockTime) <= NEW_CLUE_DURATION;

                // 设置线索UI
                clueItem.Setup(clue.time, clue.clueText, clue.icon, isNew);

                // 可选：添加点击事件
                Button clueButton = clueItemObj.GetComponent<Button>();
                if (clueButton != null)
                {
                    clueButton.onClick.AddListener(() => OnClueClicked(clueId, clue));
                }
            }
        }

        // 确保滚动到顶部
        if (clueScrollView != null)
        {
            clueScrollView.verticalNormalizedPosition = 1f;
        }

        Debug.Log($"为角色 {characters[characterIndex].characterName} 加载了 {characterClues.Count} 条线索");
    }

    /// <summary>
    /// 获取指定角色的所有线索
    /// </summary>
    private List<ClueData> GetCluesForCharacter(int characterIndex)
    {
        List<ClueData> result = new List<ClueData>();

        foreach (ClueData clue in clueDatabase)
        {
            if (clue.characterIndex == characterIndex)
            {
                result.Add(clue);
            }
        }

        return result;
    }

    /// <summary>
    /// 线索被点击
    /// </summary>
    private void OnClueClicked(string clueId, ClueData clue)
    {
        Debug.Log($"点击线索: {clue.clueText}");

        // 可以在这里添加更多逻辑，比如显示线索详情、播放音效等

        // 标记线索为已查看（清除新线索状态）
        MarkClueAsViewed(clueId);
    }

    /// <summary>
    /// 标记线索为已查看
    /// </summary>
    private void MarkClueAsViewed(string clueId)
    {
        if (clueUnlockTimes.ContainsKey(clueId))
        {
            // 设置为很早的时间，使其不再是新线索
            clueUnlockTimes[clueId] = Time.time - (NEW_CLUE_DURATION + 1f);
            SaveClueUnlockTimes();
        }
    }

    /// <summary>
    /// 添加新线索
    /// </summary>
    public void AddNewClue(int characterIndex, string time, string clueText, Sprite icon = null)
    {
        // 创建新线索数据
        ClueData newClue = new ClueData
        {
            characterIndex = characterIndex,
            time = time,
            clueText = clueText,
            icon = icon
        };

        // 添加到数据库
        Array.Resize(ref clueDatabase, clueDatabase.Length + 1);
        clueDatabase[clueDatabase.Length - 1] = newClue;

        // 生成线索ID
        string clueId = $"Character{characterIndex}_Clue{time}";

        // 记录解锁时间
        clueUnlockTimes[clueId] = Time.time;
        SaveClueUnlockTimes();

        Debug.Log($"添加新线索: {clueText}");

        // 如果当前正在查看这个角色的详情，刷新列表
        if (currentCharacterIndex == characterIndex && characterDetailPanel.activeSelf)
        {
            LoadClueListForCharacter(characterIndex);
        }
    }

    /// <summary>
    /// 获取线索的解锁时间
    /// </summary>
    private float GetClueUnlockTime(string clueId)
    {
        if (clueUnlockTimes.ContainsKey(clueId))
        {
            return clueUnlockTimes[clueId];
        }
        else
        {
            // 如果不存在，设置为当前时间（新线索）
            clueUnlockTimes[clueId] = Time.time;
            SaveClueUnlockTimes();
            return Time.time;
        }
    }

    /// <summary>
    /// 保存线索解锁时间
    /// </summary>
    private void SaveClueUnlockTimes()
    {
        foreach (var pair in clueUnlockTimes)
        {
            PlayerPrefs.SetFloat("ClueTime_" + pair.Key, pair.Value);
        }
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 加载线索解锁时间
    /// </summary>
    private void LoadClueUnlockTimes()
    {
        clueUnlockTimes.Clear();

        // 遍历所有线索数据，加载已保存的时间
        for (int i = 0; i < characters.Length; i++)
        {
            List<ClueData> clues = GetCluesForCharacter(i);
            foreach (ClueData clue in clues)
            {
                string clueId = $"Character{i}_Clue{clue.time}";
                if (PlayerPrefs.HasKey("ClueTime_" + clueId))
                {
                    float time = PlayerPrefs.GetFloat("ClueTime_" + clueId);
                    clueUnlockTimes[clueId] = time;
                }
            }
        }
    }

    /// <summary>
    /// 定期检查新线索状态
    /// </summary>
    private IEnumerator CheckNewCluesPeriodically()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);

            // 如果正在查看角色详情，刷新感叹号状态
            if (characterDetailPanel != null && characterDetailPanel.activeSelf && currentCharacterIndex >= 0)
            {
                RefreshClueItemsExclamation();
            }
        }
    }

    /// <summary>
    /// 刷新线索条目的感叹号状态
    /// </summary>
    private void RefreshClueItemsExclamation()
    {
        foreach (Transform child in clueContentParent)
        {
            ClueItemUI clueItem = child.GetComponent<ClueItemUI>();
            if (clueItem != null)
            {
                clueItem.CheckExclamationState();
            }
        }
    }

    /// <summary>
    /// 测试用：添加示例线索
    /// </summary>
    public void AddTestClues()
    {
        // 为角色0添加测试线索
        AddNewClue(0, "00:03", "杰斯听到走廊有奇怪的脚步声", null);
        AddNewClue(0, "00:10", "杰斯在书房发现一本奇怪的笔记", null);
        AddNewClue(0, "00:15", "杰斯注意到墙上的画像位置移动了", null);

        // 为角色1添加测试线索
        AddNewClue(1, "00:05", "凯在花园看到可疑的脚印", null);
        AddNewClue(1, "00:12", "凯发现后门的锁被撬开了", null);
    }

    /// <summary>
    /// 清空所有线索
    /// </summary>
    public void ClearAllClues()
    {
        // 清空数据库
        clueDatabase = new ClueData[0];

        // 清空解锁时间
        clueUnlockTimes.Clear();
        PlayerPrefs.DeleteAll();

        // 清空UI
        foreach (Transform child in clueContentParent)
        {
            Destroy(child.gameObject);
        }

        Debug.Log("已清空所有线索");
    }

    void OnDestroy()
    {
        // 保存数据
        SaveClueUnlockTimes();
    }
}
