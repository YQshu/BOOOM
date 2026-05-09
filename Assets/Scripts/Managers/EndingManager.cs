using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 结局管理器。
/// 负责指认凶手、判定结局类型并展示对应结局界面。
/// </summary>
public class EndingManager : MonoBehaviour
{
    [System.Serializable]
    public class SuspectEntry
    {
        [Tooltip("嫌疑人ID，需与_realKillerId对比")]
        public string suspectId;
        [Tooltip("嫌疑人显示名称")]
        public string suspectName;
        [Tooltip("指认按钮")]
        public Button button;
    }

    [Header("结局判定")]
    [Tooltip("真凶ID")]
    [SerializeField] private string _realKillerId = "SUSPECT_KAI";
    [Tooltip("触发真结局所需最少线索数")]
    [SerializeField] private int _requiredClueCount = 5;

    [Header("指认界面")]
    [SerializeField] private GameObject _accusationPanel;
    [SerializeField] private List<SuspectEntry> _suspects = new List<SuspectEntry>();

    [Header("结局界面")]
    [SerializeField] private GameObject _endingPanel;
    [SerializeField] private GameObject _trueEndingUI;       // 真相大白
    [SerializeField] private GameObject _incompleteEndingUI; // 线索不足
    [SerializeField] private GameObject _wrongEndingUI;      // 指认错误
    [Tooltip("结局描述文本（可选）")]
    [SerializeField] private TMP_Text _endingDescText;

    public static EndingManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // 为每个嫌疑人按钮绑定指认事件
        foreach (SuspectEntry suspect in _suspects)
        {
            string id = suspect.suspectId;
            suspect.button?.onClick.AddListener(() => TriggerAccusation(id));
        }

        // 初始隐藏所有结局界面
        if (_endingPanel != null) _endingPanel.SetActive(false);
        if (_accusationPanel != null) _accusationPanel.SetActive(false);
    }

    /// <summary>
    /// 显示指认凶手面板。
    /// </summary>
    public void ShowAccusationPanel()
    {
        if (_accusationPanel != null)
            _accusationPanel.SetActive(true);
    }

    /// <summary>
    /// 执行指认逻辑，根据线索数量和凶手ID判定结局。
    /// </summary>
    /// <param name="suspectId">玩家指认的嫌疑人ID。</param>
    public void TriggerAccusation(string suspectId)
    {
        if (_accusationPanel != null)
            _accusationPanel.SetActive(false);

        int clueCount = ClueManager.Instance != null ? ClueManager.Instance.GetCollectedCount() : 0;

        if (suspectId == _realKillerId && clueCount >= _requiredClueCount)
            ShowEnding("TrueEnding", "真相大白。你找到了所有证据，成功揭露了凶手。");
        else if (suspectId == _realKillerId)
            ShowEnding("IncompleteEnding", $"你猜对了凶手，但证据不足（{clueCount}/{_requiredClueCount}）。");
        else
            ShowEnding("WrongEnding", "你指认了错误的人。真相依然隐藏在黑暗中。");
    }

    private void ShowEnding(string endingType, string description)
    {
        if (_endingPanel != null) _endingPanel.SetActive(true);

        if (_trueEndingUI != null)       _trueEndingUI.SetActive(endingType == "TrueEnding");
        if (_incompleteEndingUI != null) _incompleteEndingUI.SetActive(endingType == "IncompleteEnding");
        if (_wrongEndingUI != null)      _wrongEndingUI.SetActive(endingType == "WrongEnding");

        if (_endingDescText != null)
            _endingDescText.text = description;

        Debug.Log($"[Ending] {endingType}：{description}");
    }

    /// <summary>
    /// 重新开始游戏（重载当前场景）。
    /// </summary>
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
