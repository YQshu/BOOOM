using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 结局管理器（基于线索完成度）。
///
/// 指认规则：
/// - 单个嫌疑人：已解锁该角色线索的 75%（向上取整）以上
/// - "自己"：所有角色线索都解锁 80%（向上取整）以上
/// - "不指认"：所有角色线索都解锁 100%
///
/// 流程：ShowAccusationPanel → 点击嫌疑人 → 检查条件 → ShowConfirmation → TriggerAccusation → ShowEnding
/// </summary>
public class EndingManager : MonoBehaviour
{
    [System.Serializable]
    public class SuspectEntry
    {
        [Tooltip("嫌疑人ID（如 SUSPECT_KAI）")]
        public string suspectId;
        [Tooltip("嫌疑人显示名称")]
        public string suspectName;
        [Tooltip("指认按钮")]
        public Button button;
        [Tooltip("版本一：草率指认（有角色未调查）")]
        [TextArea(3, 6)]
        public string endingTextHasty;
        [Tooltip("版本二：充分调查（所有角色都调查过）")]
        [TextArea(3, 6)]
        public string endingTextThorough;
    }

    [System.Serializable]
    public class SpecialEndingConfig
    {
        [Tooltip("按钮")]
        public Button button;
        [Tooltip("结局文本（用 · 分隔每句）")]
        [TextArea(3, 6)]
        public string endingText;
    }

    [Header("指认面板")]
    [SerializeField] private GameObject _accusationPanel;
    [Tooltip("显示已收集线索数量")]
    [SerializeField] private TMP_Text _clueCountText;
    [Tooltip("普通嫌疑人列表（不包括特殊选项）")]
    [SerializeField] private List<SuspectEntry> _suspects = new List<SuspectEntry>();

    [Header("特殊指认选项")]
    [Tooltip("指认自己（需所有角色线索 ≥ 80%）")]
    [SerializeField] private SpecialEndingConfig _accuseSelfConfig;
    [Tooltip("指认外域黑客/不指认任何人（需所有角色线索 = 100%，有彩蛋）")]
    [SerializeField] private SpecialEndingConfig _accuseHackerConfig;

    [Header("警告提示")]
    [SerializeField] private GameObject _warningPanel;
    [SerializeField] private TMP_Text _warningText;
    [SerializeField] private Button _warningCloseButton;

    [Header("确认面板")]
    [SerializeField] private GameObject _confirmPanel;
    [SerializeField] private TMP_Text _confirmNameText;
    [SerializeField] private Button _confirmButton;
    [SerializeField] private Button _cancelButton;

    [Header("结局面板")]
    [SerializeField] private GameObject _endingPanel;
    [SerializeField] private ScrollingCredits _scrollingCredits;
    [SerializeField] private Button _restartButton;
    [SerializeField] private Button _returnToMenuButton;

    [Header("场景名称")]
    [SerializeField] private string _mainMenuSceneName = "MainMenu";
    [SerializeField] private string _gameSceneName = "SampleScene";

    public static EndingManager Instance { get; private set; }

    private CanvasGroup _accusationCG;
    private CanvasGroup _warningCG;
    private CanvasGroup _confirmCG;
    private CanvasGroup _endingCG;
    private string _pendingSuspectId;
    private string _pendingSuspectName;
    private bool _isSpecialEnding; // 标记是否为特殊结局（指认自己/外域黑客）
    private bool _hasEasterEgg;    // 标记是否有彩蛋（仅外域黑客）

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        _accusationCG = GetOrAddCG(_accusationPanel);
        _warningCG    = GetOrAddCG(_warningPanel);
        _confirmCG    = GetOrAddCG(_confirmPanel);
        _endingCG     = GetOrAddCG(_endingPanel);

        SetVisible(_accusationCG, false);
        SetVisible(_warningCG,    false);
        SetVisible(_confirmCG,    false);
        SetVisible(_endingCG,     false);
    }

    private void Start()
    {
        // 绑定普通嫌疑人按钮
        foreach (SuspectEntry s in _suspects)
        {
            string id   = s.suspectId;
            string name = s.suspectName;
            s.button?.onClick.AddListener(() => OnSuspectButtonClick(id, name));
        }

        // 绑定特殊选项按钮
        _accuseSelfConfig?.button?.onClick.AddListener(() => OnSpecialButtonClick("SELF", "自己", false));
        _accuseHackerConfig?.button?.onClick.AddListener(() => OnSpecialButtonClick("HACKER", "外域黑客", true));

        _warningCloseButton?.onClick.AddListener(() => SetVisible(_warningCG, false));
        _confirmButton?.onClick.AddListener(OnConfirmAccuse);
        _cancelButton?.onClick.AddListener(OnCancelAccuse);
        _restartButton?.onClick.AddListener(RestartGame);
        _returnToMenuButton?.onClick.AddListener(ReturnToMenu);
    }

    // ─── 公开接口 ─────────────────────────────────────────────

    /// <summary>显示指认面板（由线索墙"指认凶手"按钮调用）。</summary>
    public void ShowAccusationPanel()
    {
        int count = ClueManager.Instance != null ? ClueManager.Instance.GetCollectedCount() : 0;
        if (_clueCountText != null)
            _clueCountText.text = $"已收集线索：{count} 条";

        SetVisible(_accusationCG, true);
        SetVisible(_warningCG,    false);
        SetVisible(_confirmCG,    false);
        SetVisible(_endingCG,     false);
    }

    public void RestartGame()  => SceneManager.LoadScene(_gameSceneName);
    public void ReturnToMenu() => SceneManager.LoadScene(_mainMenuSceneName);

    // ─── 内部流程 ─────────────────────────────────────────────

    private void OnSuspectButtonClick(string suspectId, string suspectName)
    {
        if (!CanAccuseSuspect(suspectId))
        {
            ShowWarning("当前线索还不足以指认该嫌疑人");
            return;
        }

        _isSpecialEnding = false;
        _hasEasterEgg = false;
        ShowConfirmation(suspectId, suspectName);
    }

    private void OnSpecialButtonClick(string specialId, string displayName, bool hasEasterEgg)
    {
        if (specialId == "SELF" && !CanAccuseSelf())
        {
            ShowWarning("指认自己需要所有角色的线索都达到 80% 以上");
            return;
        }

        if (specialId == "HACKER" && !CanAccuseHacker())
        {
            ShowWarning("指认外域黑客需要收集所有角色的全部线索");
            return;
        }

        _isSpecialEnding = true;
        _hasEasterEgg = hasEasterEgg;
        ShowConfirmation(specialId, displayName);
    }

    private void ShowWarning(string message)
    {
        if (_warningText != null)
            _warningText.text = message;
        SetVisible(_warningCG, true);
    }

    private void ShowConfirmation(string suspectId, string suspectName)
    {
        _pendingSuspectId   = suspectId;
        _pendingSuspectName = suspectName;

        if (_confirmNameText != null)
        {
            if (suspectId == "HACKER")
                _confirmNameText.text = $"你确定不指认任何人，将真凶指向外域黑客吗？\n确定后，游戏将结束，进入结算状态。";
            else if (suspectId == "SELF")
                _confirmNameText.text = $"你确定要指认自己为真凶吗？\n确定指认后，游戏将结束，进入结算状态。";
            else
                _confirmNameText.text = $"你确定要指认 {suspectName} 为真凶吗？\n确定指认后，游戏将结束，进入结算状态。";
        }

        // 确认面板是指认面板的子面板，只需显示确认面板即可
        SetVisible(_confirmCG, true);
    }

    private void OnConfirmAccuse()
    {
        SetVisible(_confirmCG, false);
        SetVisible(_accusationCG, false);
        TriggerAccusation(_pendingSuspectId);
    }

    private void OnCancelAccuse()
    {
        SetVisible(_confirmCG, false);
    }

    private void TriggerAccusation(string suspectId)
    {
        string endingText;

        // 处理特殊结局
        if (suspectId == "SELF")
        {
            endingText = _accuseSelfConfig?.endingText ?? "游戏结束。";
            ShowEnding(endingText, false);
            return;
        }

        if (suspectId == "HACKER")
        {
            endingText = _accuseHackerConfig?.endingText ?? "游戏结束。";
            ShowEnding(endingText, true); // 有彩蛋
            return;
        }

        // 处理普通嫌疑人
        SuspectEntry suspect = _suspects.Find(s => s.suspectId == suspectId);
        if (suspect == null)
        {
            ShowEnding("游戏结束。", false);
            return;
        }

        // 判断是草率指认还是充分调查
        bool hasUninvestigated = HasUninvestigatedSuspects();
        endingText = hasUninvestigated ? suspect.endingTextHasty : suspect.endingTextThorough;

        ShowEnding(endingText, false);
    }

    private void ShowEnding(string endingText, bool hasEasterEgg)
    {
        if (_scrollingCredits != null)
            _scrollingCredits.PlayCredits(endingText, hasEasterEgg);

        SetVisible(_endingCG, true);
        Debug.Log($"[Ending] 播放结局字幕，彩蛋={hasEasterEgg}");
    }

    // ─── 线索完成度判定 ───────────────────────────────────────

    /// <summary>判断是否可以指认该嫌疑人（普通嫌疑人需 ≥ 75%）。</summary>
    private bool CanAccuseSuspect(string suspectId)
    {
        return GetSuspectClueProgress(suspectId, 0.75f);
    }

    /// <summary>判断是否可以指认"自己"（所有角色线索 ≥ 80%）。</summary>
    private bool CanAccuseSelf()
    {
        foreach (SuspectEntry s in _suspects)
        {
            if (!GetSuspectClueProgress(s.suspectId, 0.8f))
                return false;
        }
        return true;
    }

    /// <summary>判断是否可以指认"外域黑客"（所有角色线索 = 100%）。</summary>
    private bool CanAccuseHacker()
    {
        foreach (SuspectEntry s in _suspects)
        {
            if (!GetSuspectClueProgress(s.suspectId, 1.0f))
                return false;
        }
        return true;
    }

    /// <summary>
    /// 检查某嫌疑人的线索完成度是否达到阈值（向上取整）。
    /// </summary>
    /// <param name="suspectId">嫌疑人ID</param>
    /// <param name="threshold">阈值（0.75 = 75%）</param>
    private bool GetSuspectClueProgress(string suspectId, float threshold)
    {
        if (ClueManager.Instance == null) return false;

        List<ClueDataSO> allClues = ClueManager.Instance.GetAllClues();
        if (allClues == null) return false;

        int total = 0;
        int collected = 0;

        foreach (ClueDataSO clue in allClues)
        {
            if (clue == null || clue.suspectId != suspectId) continue;
            total++;
            if (ClueManager.Instance.HasClue(clue.clueId))
                collected++;
        }

        if (total == 0) return false;

        int required = Mathf.CeilToInt(total * threshold);
        return collected >= required;
    }

    /// <summary>
    /// 判断是否有嫌疑人完全未调查（线索收集率 = 0%）。
    /// </summary>
    private bool HasUninvestigatedSuspects()
    {
        if (ClueManager.Instance == null) return false;

        List<ClueDataSO> allClues = ClueManager.Instance.GetAllClues();
        if (allClues == null) return false;

        foreach (SuspectEntry s in _suspects)
        {
            int total = 0;
            int collected = 0;

            foreach (ClueDataSO clue in allClues)
            {
                if (clue == null || clue.suspectId != s.suspectId) continue;
                total++;
                if (ClueManager.Instance.HasClue(clue.clueId))
                    collected++;
            }

            // 如果该嫌疑人有线索但一条都没收集，说明完全未调查
            if (total > 0 && collected == 0)
                return true;
        }

        return false;
    }

    // ─── 工具方法 ─────────────────────────────────────────────

    private static CanvasGroup GetOrAddCG(GameObject go)
    {
        if (go == null) return null;
        CanvasGroup cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();
        return cg;
    }

    private static void SetVisible(CanvasGroup cg, bool visible)
    {
        if (cg == null) return;
        cg.alpha          = visible ? 1f : 0f;
        cg.interactable   = visible;
        cg.blocksRaycasts = visible;
    }
}
