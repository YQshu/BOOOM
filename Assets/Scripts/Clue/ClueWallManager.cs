using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Playables;

/// <summary>
/// 线索墙管理器（含嫌疑人选择 + 回溯入口）。
/// 三层结构：
///   第一层 — 主面板：四个嫌疑人头像卡片，从屏幕侧边滑入
///   第二层 — 嫌疑人详情面板：左侧全身像+基础信息，右侧线索展示台+进度+开始回溯按钮
///   第三层 — 线索详细面板（预留）
/// 所有面板通过 CanvasGroup 控制显隐，物体始终保持激活。
/// </summary>
public class ClueWallManager : MonoBehaviour
{
    // ─── 数据结构 ─────────────────────────────────────────────

    [System.Serializable]
    public class SuspectEntry
    {
        [Header("角色信息")]
        public CharacterData characterData;

        [Header("主面板卡片UI")]
        [Tooltip("嫌疑人卡片按钮")]
        public Button cardButton;
        [Tooltip("卡片头像 Image")]
        public Image cardAvatar;
        [Tooltip("卡片名称文本（可选）")]
        public TMP_Text cardName;
        [Tooltip("未读新线索红点（可选）")]
        public GameObject cardBadge;

        [Header("回溯配置")]
        [Tooltip("嫌疑人唯一ID（需与 ClueDataSO.suspectId 一致，如 SUSPECT_JESS）")]
        public string suspectId;
        [Tooltip("该嫌疑人的 PlayableDirector（Timeline）")]
        public PlayableDirector director;
        [Tooltip("嫌疑人 NPC 的 Transform（用于房间追踪和视野遮罩）")]
        public Transform npcTransform;
        [Tooltip("嫌疑人世界根节点（进入回溯时激活，退出时隐藏）")]
        public GameObject worldRoot;
    }

    // ─── UI 引用 ─────────────────────────────────────────────

    [Header("主面板（第一层）")]
    [Tooltip("线索墙主面板根节点，需挂载 CanvasGroup")]
    [SerializeField] private RectTransform _mainPanel;
    [Tooltip("面板从屏幕外滑入的起始偏移（正值=右侧，负值=左侧）")]
    [SerializeField] private float _slideOffscreenX = 800f;
    [Tooltip("滑入/滑出动画时长")]
    [SerializeField] private float _slideDuration = 0.3f;
    [Tooltip("关闭线索墙按钮")]
    [SerializeField] private Button _closeButton;

    [Header("详情面板（第二层）")]
    [Tooltip("嫌疑人详情面板根节点，需挂载 CanvasGroup")]
    [SerializeField] private GameObject _detailPanel;
    [Tooltip("返回按钮（详情 → 主面板）")]
    [SerializeField] private Button _backButton;

    [Header("详情面板 — 左侧角色信息")]
    [SerializeField] private Image _detailFullBody;
    [SerializeField] private TMP_Text _detailName;
    [SerializeField] private TMP_Text _detailAge;
    [SerializeField] private TMP_Text _detailBackground;

    [Header("详情面板 — 右侧线索展示台")]
    [Tooltip("线索条目的父节点（ScrollRect Content）")]
    [SerializeField] private Transform _clueContentParent;
    [Tooltip("线索条目预制体（需含 ClueItemUI 组件）")]
    [SerializeField] private GameObject _clueItemPrefab;
    [Tooltip("进度文本，格式：x/y")]
    [SerializeField] private TMP_Text _clueProgressText;

    [Header("详情面板 — 回溯入口")]
    [Tooltip("开始回溯按钮（点击后关闭线索墙并进入该嫌疑人的回溯）")]
    [SerializeField] private Button _startRetrospectButton;

    [Header("指认凶手入口")]
    [Tooltip("指认凶手按钮（主面板上）")]
    [SerializeField] private Button _accuseButton;
    [Tooltip("收集多少条线索后才显示指认按钮（0 = 始终显示）")]
    [SerializeField] private int _accuseUnlockClueCount = 3;

    [Header("测试功能")]
    [Tooltip("【测试用】一键解锁所有线索按钮")]
    [SerializeField] private Button _debugUnlockAllButton;

    [Header("数据")]
    [SerializeField] private SuspectEntry[] _suspects;

    // ─── 内部状态 ─────────────────────────────────────────────

    private CanvasGroup _mainCanvasGroup;
    private CanvasGroup _detailCanvasGroup;
    private int _currentSuspectIndex = -1;
    private Coroutine _slideCoroutine;

    // ─── 生命周期 ─────────────────────────────────────────────

    private void Awake()
    {
        _mainCanvasGroup = GetOrAddCanvasGroup(_mainPanel.gameObject);
        _detailCanvasGroup = GetOrAddCanvasGroup(_detailPanel);

        SetVisible(_mainCanvasGroup, false);
        SetVisible(_detailCanvasGroup, false);
    }

    private void Start()
    {
        for (int i = 0; i < _suspects.Length; i++)
        {
            int index = i;
            _suspects[i].cardButton?.onClick.AddListener(() => OpenDetailPanel(index));
        }

        if (_closeButton != null)
            _closeButton.onClick.AddListener(CloseClueWall);
        if (_backButton != null)
            _backButton.onClick.AddListener(BackToMainPanel);
        if (_startRetrospectButton != null)
            _startRetrospectButton.onClick.AddListener(OnStartRetrospectClicked);
        if (_accuseButton != null)
            _accuseButton.onClick.AddListener(OnAccuseButtonClick);
        if (_debugUnlockAllButton != null)
            _debugUnlockAllButton.onClick.AddListener(OnDebugUnlockAll);

        if (ClueManager.Instance != null)
            ClueManager.Instance.OnClueCollected += OnClueCollected;

        RefreshCharacterCards();
        RefreshAccuseButton();
    }

    private void OnDestroy()
    {
        if (ClueManager.Instance != null)
            ClueManager.Instance.OnClueCollected -= OnClueCollected;
    }

    // ─── 公开接口 ─────────────────────────────────────────────

    /// <summary>打开线索墙主面板。</summary>
    public void OpenClueWall()
    {
        RefreshCharacterCards();
        RefreshAccuseButton();
        SetVisible(_mainCanvasGroup, true);
        SetVisible(_detailCanvasGroup, false);

        if (_slideCoroutine != null) StopCoroutine(_slideCoroutine);
        _mainPanel.anchoredPosition = new Vector2(_slideOffscreenX, _mainPanel.anchoredPosition.y);
        _slideCoroutine = StartCoroutine(SlidePanel(_mainPanel, Vector2.zero, _slideDuration));

        Debug.Log("[ClueWall] 打开线索墙");
    }

    /// <summary>关闭线索墙。</summary>
    public void CloseClueWall()
    {
        if (_slideCoroutine != null) StopCoroutine(_slideCoroutine);
        _slideCoroutine = StartCoroutine(
            SlideAndHide(_mainPanel, new Vector2(_slideOffscreenX, 0), _slideDuration, _mainCanvasGroup));
        SetVisible(_detailCanvasGroup, false);

        Debug.Log("[ClueWall] 关闭线索墙");
    }

    // ─── 第一层：主面板 ───────────────────────────────────────

    private void RefreshCharacterCards()
    {
        for (int i = 0; i < _suspects.Length; i++)
        {
            SuspectEntry entry = _suspects[i];
            CharacterData data = entry.characterData;
            if (data == null) continue;

            if (entry.cardAvatar != null) entry.cardAvatar.sprite = data.avatar;
            if (entry.cardName != null) entry.cardName.text = data.characterName;
            if (entry.cardBadge != null) entry.cardBadge.SetActive(HasUnreadClue(i));
        }
    }

    // ─── 第二层：详情面板 ─────────────────────────────────────

    private void OpenDetailPanel(int index)
    {
        if (index < 0 || index >= _suspects.Length) return;

        _currentSuspectIndex = index;
        SuspectEntry entry = _suspects[index];
        CharacterData data = entry.characterData;

        if (_detailFullBody != null) _detailFullBody.sprite = data.fullBodySprite;
        if (_detailName != null) _detailName.text = data.characterName;
        if (_detailAge != null) _detailAge.text = $"年龄：{data.age}";
        if (_detailBackground != null) _detailBackground.text = data.background;

        // 回溯按钮：director 未配置时禁用
        if (_startRetrospectButton != null)
            _startRetrospectButton.interactable = entry.director != null;

        RefreshClueList(index);

        SetVisible(_mainCanvasGroup, false);
        SetVisible(_detailCanvasGroup, true);

        if (entry.cardBadge != null) entry.cardBadge.SetActive(false);

        Debug.Log($"[ClueWall] 打开详情：{data.characterName}");
    }

    private void BackToMainPanel()
    {
        SetVisible(_detailCanvasGroup, false);
        SetVisible(_mainCanvasGroup, true);
        RefreshCharacterCards();
    }

    // ─── 回溯入口 ─────────────────────────────────────────────

    private void OnStartRetrospectClicked()
    {
        if (_currentSuspectIndex < 0 || _currentSuspectIndex >= _suspects.Length) return;

        SuspectEntry entry = _suspects[_currentSuspectIndex];
        if (entry.director == null)
        {
            Debug.LogWarning("[ClueWall] 该嫌疑人尚未配置 PlayableDirector，无法进入回溯。");
            return;
        }

        // 先关闭线索墙（立即隐藏，不播放滑出动画）
        SetVisible(_mainCanvasGroup, false);
        SetVisible(_detailCanvasGroup, false);

        // 进入回溯
        if (RetrospectManager.Instance != null)
        {
            RetrospectManager.Instance.EnterRetrospect(
                entry.suspectId,
                entry.director,
                entry.npcTransform,
                entry.characterData != null ? entry.characterData.characterName : "",
                entry.worldRoot);
        }

        Debug.Log($"[ClueWall] 进入回溯：{entry.characterData?.characterName}");
    }

    // ─── 右侧线索展示台 ───────────────────────────────────────

    private void RefreshClueList(int suspectIndex)
    {
        if (_clueContentParent == null || _clueItemPrefab == null) return;

        foreach (Transform child in _clueContentParent)
            Destroy(child.gameObject);

        List<ClueDataSO> clues = GetCluesForSuspect(suspectIndex);
        int total = clues.Count;
        int collected = 0;

        foreach (ClueDataSO clue in clues)
        {
            bool isCollected = ClueManager.Instance != null
                               && ClueManager.Instance.HasClue(clue.clueId);

            GameObject item = Instantiate(_clueItemPrefab, _clueContentParent);
            ClueItemUI ui = item.GetComponent<ClueItemUI>();

            if (ui != null)
            {
                if (isCollected)
                {
                    ui.Setup(clue.time, clue.clueName, clue.icon, isNewClue: IsNewClue(clue.clueId));
                    Button btn = item.GetComponent<Button>();
                    if (btn != null)
                    {
                        string clueId = clue.clueId;
                        btn.onClick.AddListener(() => OnClueItemClicked(clueId));
                    }
                }
                else
                {
                    ui.Setup(clue.time, "???", null, isNewClue: false);
                    Button btn = item.GetComponent<Button>();
                    if (btn != null) btn.interactable = false;
                }
            }

            if (isCollected) collected++;
        }

        if (_clueProgressText != null)
            _clueProgressText.text = $"{collected}/{total}";
    }

    private void OnClueItemClicked(string clueId)
    {
        // 预留：打开第三层线索详细面板
        Debug.Log($"[ClueWall] 点击线索：{clueId}（详细面板待实装）");
    }

    // ─── 事件回调 ─────────────────────────────────────────────

    private void OnClueCollected(ClueDataSO clue)
    {
        if (_detailCanvasGroup.alpha > 0.01f && _currentSuspectIndex >= 0)
        {
            string suspectId = _suspects[_currentSuspectIndex].suspectId;
            if (clue.suspectId == suspectId)
                RefreshClueList(_currentSuspectIndex);
        }
        RefreshCharacterCards();
        RefreshAccuseButton();
    }

    // ─── 工具方法 ─────────────────────────────────────────────

    private List<ClueDataSO> GetCluesForSuspect(int suspectIndex)
    {
        List<ClueDataSO> result = new List<ClueDataSO>();
        if (ClueManager.Instance == null || suspectIndex < 0 || suspectIndex >= _suspects.Length) return result;

        List<ClueDataSO> allClues = ClueManager.Instance.GetAllClues();
        if (allClues == null) return result;

        string suspectId = _suspects[suspectIndex].suspectId;
        foreach (ClueDataSO clue in allClues)
        {
            if (clue != null && clue.suspectId == suspectId)
                result.Add(clue);
        }
        result.Sort((a, b) => string.Compare(a.time, b.time, System.StringComparison.Ordinal));
        return result;
    }

    private bool HasUnreadClue(int suspectIndex)
    {
        foreach (ClueDataSO clue in GetCluesForSuspect(suspectIndex))
        {
            if (ClueManager.Instance != null
                && ClueManager.Instance.HasClue(clue.clueId)
                && IsNewClue(clue.clueId))
                return true;
        }
        return false;
    }

    private bool IsNewClue(string clueId)
    {
        float unlockTime = PlayerPrefs.GetFloat("ClueTime_" + clueId, -9999f);
        return (Time.time - unlockTime) <= 30f;
    }

    private static CanvasGroup GetOrAddCanvasGroup(GameObject go)
    {
        CanvasGroup cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();
        return cg;
    }

    private static void SetVisible(CanvasGroup cg, bool visible)
    {
        if (cg == null) return;
        cg.alpha = visible ? 1f : 0f;
        cg.interactable = visible;
        cg.blocksRaycasts = visible;
    }

    // ─── 滑动动画 ─────────────────────────────────────────────

    private IEnumerator SlidePanel(RectTransform panel, Vector2 targetPos, float duration)
    {
        Vector2 startPos = panel.anchoredPosition;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            panel.anchoredPosition = Vector2.Lerp(startPos, targetPos, elapsed / duration);
            yield return null;
        }
        panel.anchoredPosition = targetPos;
        _slideCoroutine = null;
    }

    private IEnumerator SlideAndHide(RectTransform panel, Vector2 targetPos, float duration, CanvasGroup cg)
    {
        yield return StartCoroutine(SlidePanel(panel, targetPos, duration));
        SetVisible(cg, false);
        _slideCoroutine = null;
    }

    // ─── 指认入口 ─────────────────────────────────────────────

    private void RefreshAccuseButton()
    {
        if (_accuseButton == null) return;
        int count = ClueManager.Instance != null ? ClueManager.Instance.GetCollectedCount() : 0;
        bool unlocked = _accuseUnlockClueCount <= 0 || count >= _accuseUnlockClueCount;
        _accuseButton.gameObject.SetActive(unlocked);
    }

    private void OnAccuseButtonClick()
    {
        CloseClueWall();
        EndingManager.Instance?.ShowAccusationPanel();
    }

    // ─── 测试功能 ─────────────────────────────────────────────

    private void OnDebugUnlockAll()
    {
        ClueManager.Instance?.UnlockAllClues();
        RefreshCharacterCards();
        RefreshAccuseButton();
        Debug.Log("[ClueWall] 测试：已解锁所有线索");
    }
}
