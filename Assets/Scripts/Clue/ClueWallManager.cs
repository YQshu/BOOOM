using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ClueWallManager : MonoBehaviour
{
    [Header("界面引用")]
    public GameObject mainScreen;
    public GameObject clueWallRoot;
    public GameObject characterDetailPanel;

    [Header("角色卡片")]
    public Button[] characterCardButtons;

    [Header("角色详情界面元素")]
    public Image characterDetailAvatar;
    public TMP_Text characterDetailName;
    public TMP_Text characterDetailDescription;
    public Button backButton;

    [Header("线索列表")]
    public GameObject clueItemPrefab;
    public Transform clueContentParent;
    public ScrollRect clueScrollView;

    [Header("角色数据")]
    public CharacterData[] characters;
    public ClueData[] clueDatabase;

    [Header("退出按钮")]
    public Button exitClueWallButton;

    private int currentCharacterIndex = -1;
    private Dictionary<string, float> clueUnlockTimes = new Dictionary<string, float>();
    private const float NEW_CLUE_DURATION = 5f;

    void Start()
    {
        if (mainScreen != null) mainScreen.SetActive(true);
        if (clueWallRoot != null) clueWallRoot.SetActive(false);
        if (characterDetailPanel != null) characterDetailPanel.SetActive(false);

        for (int i = 0; i < characterCardButtons.Length; i++)
        {
            int index = i;
            characterCardButtons[i].onClick.AddListener(() => OnSelectCharacter(index));
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackToCharacterList);
        }

        LoadClueUnlockTimes();
        StartCoroutine(CheckNewCluesPeriodically());

        if (ClueManager.Instance != null)
        {
            ClueManager.Instance.OnClueCollected += HandleClueCollected;
        }
    }

    /// <summary>
    /// 从主游戏界面进入线索墙
    /// </summary>
    public void OnEnterClueWall()
    {
        if (mainScreen != null) mainScreen.SetActive(false);
        if (clueWallRoot != null) clueWallRoot.SetActive(true);
        if (characterDetailPanel != null) characterDetailPanel.SetActive(false);

        Debug.Log("[ClueWall] 进入线索墙界面");
    }

    /// <summary>
    /// 从线索墙返回主游戏界面
    /// </summary>
    public void OnExitClueWall()
    {
        if (clueWallRoot != null) clueWallRoot.SetActive(false);

        Debug.Log("[ClueWall] 退出线索墙界面");
    }

    /// <summary>
    /// 选择角色
    /// </summary>
    public void OnSelectCharacter(int characterIndex)
    {
        if (characterIndex < 0 || characterIndex >= characters.Length)
        {
            Debug.LogError("[ClueWall] 无效的角色索引: " + characterIndex);
            return;
        }

        currentCharacterIndex = characterIndex;

        if (clueWallRoot != null)
        {
            clueWallRoot.SetActive(false);
        }

        if (exitClueWallButton != null)
        {
            exitClueWallButton.onClick.RemoveAllListeners();
            exitClueWallButton.onClick.AddListener(OnExitClueWall);
        }

        if (characterDetailPanel != null)
        {
            characterDetailPanel.SetActive(true);
        }

        LoadCharacterDetail(characterIndex);

        Debug.Log($"[ClueWall] 选择角色: {characters[characterIndex].characterName}");
    }

    /// <summary>
    /// 从角色详情返回角色选择列表
    /// </summary>
    public void OnBackToCharacterList()
    {
        if (characterDetailPanel != null)
        {
            characterDetailPanel.SetActive(false);
        }

        if (clueWallRoot != null)
        {
            clueWallRoot.SetActive(true);
        }

        Debug.Log("[ClueWall] 返回角色选择列表");
    }

    /// <summary>
    /// 根据线索ID获取所属角色索引。
    /// </summary>
    /// <param name="clueId">线索ID。</param>
    /// <returns>返回角色索引，未找到返回 -1。</returns>
    public int GetCharacterIndexByClueId(string clueId)
    {
        if (string.IsNullOrWhiteSpace(clueId) || clueDatabase == null)
        {
            return -1;
        }

        for (int i = 0; i < clueDatabase.Length; i++)
        {
            ClueData clue = clueDatabase[i];
            string resolvedClueId = ResolveClueId(clue, clue.characterIndex);
            if (resolvedClueId == clueId)
            {
                return clue.characterIndex;
            }
        }

        return -1;
    }

    /// <summary>
    /// 处理线索收集事件，记录解锁时间并按需刷新当前详情界面。
    /// </summary>
    private void HandleClueCollected(string clueId, string clueName)
    {
        if (string.IsNullOrWhiteSpace(clueId))
        {
            return;
        }

        clueUnlockTimes[clueId] = Time.time;
        SaveClueUnlockTimes();

        int characterIndex = GetCharacterIndexByClueId(clueId);
        if (characterIndex >= 0 && currentCharacterIndex == characterIndex && characterDetailPanel != null && characterDetailPanel.activeSelf)
        {
            LoadClueListForCharacter(characterIndex);
        }

        Debug.Log($"[ClueWall] 收到新线索：{clueId} - {clueName}");
    }

    /// <summary>
    /// 加载角色详情
    /// </summary>
    private void LoadCharacterDetail(int characterIndex)
    {
        CharacterData character = characters[characterIndex];

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

        LoadClueListForCharacter(characterIndex);
    }

    /// <summary>
    /// 加载指定角色的线索列表，仅展示当前已收集线索。
    /// </summary>
    private void LoadClueListForCharacter(int characterIndex)
    {
        if (clueItemPrefab == null || clueContentParent == null)
        {
            Debug.LogError("[ClueWall] 线索预制体或父对象未设置");
            return;
        }

        foreach (Transform child in clueContentParent)
        {
            Destroy(child.gameObject);
        }

        List<ClueData> characterClues = GetCluesForCharacter(characterIndex);
        characterClues.Sort((a, b) =>
        {
            if (a.time == null || b.time == null) return 0;
            return a.time.CompareTo(b.time);
        });

        int visibleCount = 0;
        foreach (ClueData clue in characterClues)
        {
            string clueId = ResolveClueId(clue, characterIndex);
            if (!IsClueCollected(clueId))
            {
                continue;
            }

            GameObject clueItemObj = Instantiate(clueItemPrefab, clueContentParent);
            ClueItemUI clueItem = clueItemObj.GetComponent<ClueItemUI>();

            if (clueItem != null)
            {
                float unlockTime = GetClueUnlockTime(clueId);
                bool isNew = (Time.time - unlockTime) <= NEW_CLUE_DURATION;
                clueItem.Setup(clue.time, clue.clueText, clue.icon, isNew);

                Button clueButton = clueItemObj.GetComponent<Button>();
                if (clueButton != null)
                {
                    clueButton.onClick.RemoveAllListeners();
                    clueButton.onClick.AddListener(() => OnClueClicked(clueId, clue));
                }
            }

            visibleCount++;
        }

        if (clueScrollView != null)
        {
            clueScrollView.verticalNormalizedPosition = 1f;
        }

        Debug.Log($"[ClueWall] 为角色 {characters[characterIndex].characterName} 显示了 {visibleCount} 条已收集线索");
    }

    /// <summary>
    /// 获取指定角色的所有线索
    /// </summary>
    private List<ClueData> GetCluesForCharacter(int characterIndex)
    {
        List<ClueData> result = new List<ClueData>();

        if (clueDatabase == null || clueDatabase.Length == 0)
        {
            return result;
        }

        foreach (ClueData clue in clueDatabase)
        {
            if (clue == null)
            {
                continue;
            }

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
        Debug.Log($"[ClueWall] 点击线索: {clue.clueText}");
        MarkClueAsViewed(clueId);
    }

    /// <summary>
    /// 标记线索为已查看
    /// </summary>
    private void MarkClueAsViewed(string clueId)
    {
        if (clueUnlockTimes.ContainsKey(clueId))
        {
            clueUnlockTimes[clueId] = Time.time - (NEW_CLUE_DURATION + 1f);
            SaveClueUnlockTimes();
        }
    }

    /// <summary>
    /// 添加新线索（测试功能）。
    /// </summary>
    public void AddNewClue(int characterIndex, string time, string clueText, Sprite icon = null)
    {
        ClueData newClue = new ClueData
        {
            clueId = $"TEST_{characterIndex}_{time}",
            characterIndex = characterIndex,
            time = time,
            clueText = clueText,
            icon = icon
        };

        Array.Resize(ref clueDatabase, clueDatabase.Length + 1);
        clueDatabase[clueDatabase.Length - 1] = newClue;

        if (ClueManager.Instance != null)
        {
            ClueManager.Instance.CollectClue(newClue.clueId, clueText);
        }

        Debug.Log($"[ClueWall] 添加新线索: {clueText}");
    }

    /// <summary>
    /// 获取线索的解锁时间，不存在时默认返回很早时间。
    /// </summary>
    private float GetClueUnlockTime(string clueId)
    {
        if (clueUnlockTimes.ContainsKey(clueId))
        {
            return clueUnlockTimes[clueId];
        }

        return -9999f;
    }

    /// <summary>
    /// 判断线索是否已收集。
    /// </summary>
    private bool IsClueCollected(string clueId)
    {
        if (ClueManager.Instance == null)
        {
            return false;
        }

        return ClueManager.Instance.HasClue(clueId);
    }

    /// <summary>
    /// 解析线索ID。优先使用配置ID，缺失时使用回退规则。
    /// </summary>
    private string ResolveClueId(ClueData clue, int characterIndex)
    {
        if (!string.IsNullOrWhiteSpace(clue.clueId))
        {
            return clue.clueId;
        }

        return $"Character{characterIndex}_Clue{clue.time}";
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

        if (characters == null || characters.Length == 0)
        {
            return;
        }

        for (int i = 0; i < characters.Length; i++)
        {
            List<ClueData> clues = GetCluesForCharacter(i);
            foreach (ClueData clue in clues)
            {
                string clueId = ResolveClueId(clue, i);
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
        AddNewClue(0, "00:03", "杰斯听到走廊有奇怪的脚步声", null);
        AddNewClue(0, "00:10", "杰斯在书房发现一本奇怪的笔记", null);
        AddNewClue(0, "00:15", "杰斯注意到墙上的画像位置移动了", null);

        AddNewClue(1, "00:05", "凯在花园看到可疑的脚印", null);
        AddNewClue(1, "00:12", "凯发现后门的锁被撬开了", null);
    }

    /// <summary>
    /// 清空所有线索
    /// </summary>
    public void ClearAllClues()
    {
        clueDatabase = new ClueData[0];
        clueUnlockTimes.Clear();

        if (ClueManager.Instance != null)
        {
            ClueManager.Instance.ClearAllClues();
        }

        foreach (Transform child in clueContentParent)
        {
            Destroy(child.gameObject);
        }

        Debug.Log("[ClueWall] 已清空所有线索");
    }

    void OnDestroy()
    {
        if (ClueManager.Instance != null)
        {
            ClueManager.Instance.OnClueCollected -= HandleClueCollected;
        }

        SaveClueUnlockTimes();
    }
}
