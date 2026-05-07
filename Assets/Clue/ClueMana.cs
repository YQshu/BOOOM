using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 线索管理器 - 管理所有线索的收集和状态
/// </summary>
public class ClueMana : MonoBehaviour
{
    public static ClueMana Instance { get; private set; }

    [Header("线索数据")]
    [SerializeField] private List<ClueData> allClues;           // 所有线索
    [SerializeField] private GameObject clueCardPrefab;         // 线索卡预制体
    [SerializeField] private Transform clueGrid;                // 线索网格容器

    [Header("详情面板")]
    [SerializeField] private GameObject detailPanel;            // 详情面板
    [SerializeField] private Image detailImage;                 // 大图
    [SerializeField] private TextMeshProUGUI detailTitle;       // 标题
    [SerializeField] private TextMeshProUGUI detailDescription; // 描述
    [SerializeField] private Button closeDetailBtn;             // 关闭按钮

    [Header("线索墙界面")]
    [SerializeField] private GameObject clueWallPanel;          // 整个线索墙界面

    // 已解锁的线索
    private HashSet<string> unlockedClues = new HashSet<string>();
    // 当前显示的线索卡
    private Dictionary<string, ClueCard> clueCards = new Dictionary<string, ClueCard>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // 初始化默认解锁的线索
        InitializeDefaultClues();

        // 绑定关闭按钮
        if (closeDetailBtn != null)
            closeDetailBtn.onClick.AddListener(CloseDetail);

        // 初始关闭界面
        if (clueWallPanel != null)
            clueWallPanel.SetActive(false);

        if (detailPanel != null)
            detailPanel.SetActive(false);
    }

    /// <summary>
    /// 初始化默认解锁的线索
    /// </summary>
    private void InitializeDefaultClues()
    {
        foreach (var clue in allClues)
        {
            if (clue.isUnlockedByDefault)
            {
                UnlockClue(clue.clueId);
            }
        }
    }

    /// <summary>
    /// 解锁线索
    /// </summary>
    public void UnlockClue(string clueId)
    {
        if (unlockedClues.Contains(clueId))
        {
            Debug.Log($"线索 {clueId} 已经解锁");
            return;
        }

        ClueData clue = GetClueById(clueId);
        if (clue == null)
        {
            Debug.LogWarning($"找不到线索: {clueId}");
            return;
        }

        unlockedClues.Add(clueId);
        Debug.Log($"解锁线索: {clue.clueName}");

        // 添加到UI
        AddClueToWall(clue);

        // 可以在这里播放获得线索的音效
        // AudioManager.Play("ClueObtained");

        // 可以在这里显示获得线索的提示
        ShowClueObtainedTip(clue);
    }

    /// <summary>
    /// 添加线索卡到墙上
    /// </summary>
    private void AddClueToWall(ClueData clue)
    {
        if (clueCardPrefab == null || clueGrid == null) return;

        GameObject cardObj = Instantiate(clueCardPrefab, clueGrid);
        ClueCard card = cardObj.GetComponent<ClueCard>();

        if (card != null)
        {
            card.Initialize(clue, OnClueCardClicked);
            clueCards[clue.clueId] = card;
        }
    }

    /// <summary>
    /// 点击线索卡时的回调
    /// </summary>
    private void OnClueCardClicked(ClueData clue)
    {
        ShowDetail(clue);
    }

    /// <summary>
    /// 显示详情面板
    /// </summary>
    private void ShowDetail(ClueData clue)
    {
        if (detailPanel == null) return;

        if (detailImage != null && clue.clueImage != null)
            detailImage.sprite = clue.clueImage;

        if (detailTitle != null)
            detailTitle.text = clue.clueName;

        if (detailDescription != null)
            detailDescription.text = clue.clueDescription;

        detailPanel.SetActive(true);
    }

    /// <summary>
    /// 关闭详情面板
    /// </summary>
    private void CloseDetail()
    {
        if (detailPanel != null)
            detailPanel.SetActive(false);
    }

    /// <summary>
    /// 显示获得线索的提示
    /// </summary>
    private void ShowClueObtainedTip(ClueData clue)
    {
        // 可以使用 Toast 消息或浮动提示
        Debug.Log($"获得线索: {clue.clueName}");
        // TODO: 实现 UI 提示效果
    }

    /// <summary>
    /// 打开线索墙界面
    /// </summary>
    public void OpenClueWall()
    {
        if (clueWallPanel != null)
        {
            clueWallPanel.SetActive(true);
            // 可以添加打开动画
        }
    }

    /// <summary>
    /// 关闭线索墙界面
    /// </summary>
    public void CloseClueWall()
    {
        if (clueWallPanel != null)
        {
            clueWallPanel.SetActive(false);
            CloseDetail(); // 同时关闭详情面板
        }
    }

    /// <summary>
    /// 切换线索墙界面
    /// </summary>
    public void ToggleClueWall()
    {
        if (clueWallPanel != null && clueWallPanel.activeSelf)
        {
            CloseClueWall();
        }
        else
        {
            OpenClueWall();
        }
    }

    /// <summary>
    /// 根据ID获取线索
    /// </summary>
    public ClueData GetClueById(string clueId)
    {
        return allClues.FirstOrDefault(c => c.clueId == clueId);
    }

    /// <summary>
    /// 检查线索是否已解锁
    /// </summary>
    public bool IsClueUnlocked(string clueId)
    {
        return unlockedClues.Contains(clueId);
    }

    /// <summary>
    /// 获取所有已解锁的线索
    /// </summary>
    public List<ClueData> GetUnlockedClues()
    {
        return allClues.Where(c => unlockedClues.Contains(c.clueId)).ToList();
    }
}
