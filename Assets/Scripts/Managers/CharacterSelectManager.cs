using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

/// <summary>
/// 角色选择管理器。
/// 负责展示角色卡片、切换周目并播放对应Timeline。
/// </summary>
public class CharacterSelectManager : MonoBehaviour
{
    [System.Serializable]
    public class CharacterEntry
    {
        [Tooltip("对应的周目ID，需与LoopManager中配置一致")]
        public string loopId;
        [Tooltip("角色显示名称")]
        public string characterName;
        [Tooltip("该角色对应的PlayableDirector（Timeline）")]
        public PlayableDirector director;
        [Tooltip("选择按钮")]
        public Button selectButton;
        [Tooltip("选中状态指示器（选中时激活）")]
        public GameObject selectedIndicator;
    }

    [Header("角色配置")]
    [SerializeField] private List<CharacterEntry> _characters = new List<CharacterEntry>();

    [Header("界面")]
    [SerializeField] private GameObject _selectPanel;

    [Header("引用")]
    [SerializeField] private LoopManager _loopManager;

    private int _currentIndex = -1;

    private void Start()
    {
        if (_loopManager == null)
            _loopManager = FindObjectOfType<LoopManager>();

        // 为每张卡片绑定点击事件
        for (int i = 0; i < _characters.Count; i++)
        {
            int index = i; // 闭包捕获
            _characters[i].selectButton?.onClick.AddListener(() => SelectCharacter(index));
        }

        // 默认选中第一个角色
        if (_characters.Count > 0)
            SelectCharacter(0);
    }

    /// <summary>
    /// 显示角色选择面板。
    /// </summary>
    public void ShowSelectPanel()
    {
        if (_selectPanel != null)
            _selectPanel.SetActive(true);
    }

    /// <summary>
    /// 隐藏角色选择面板。
    /// </summary>
    public void HideSelectPanel()
    {
        if (_selectPanel != null)
            _selectPanel.SetActive(false);
    }

    /// <summary>
    /// 选择指定索引的角色，切换周目并播放对应Timeline。
    /// </summary>
    public void SelectCharacter(int index)
    {
        if (index < 0 || index >= _characters.Count) return;
        if (index == _currentIndex) return;

        _currentIndex = index;
        CharacterEntry entry = _characters[index];

        // 切换周目
        if (_loopManager != null)
            _loopManager.SwitchToLoopById(entry.loopId);

        // 从头播放对应Timeline
        if (entry.director != null)
        {
            entry.director.time = 0;
            entry.director.Play();
        }

        // 更新所有卡片的选中状态
        for (int i = 0; i < _characters.Count; i++)
        {
            if (_characters[i].selectedIndicator != null)
                _characters[i].selectedIndicator.SetActive(i == index);
        }

        HideSelectPanel();
        Debug.Log($"[CharacterSelect] 选择角色：{entry.characterName}（周目：{entry.loopId}）");
    }

    /// <summary>
    /// 按周目ID选择角色（供外部调用）。
    /// </summary>
    public void SelectCharacterByLoopId(string loopId)
    {
        int index = _characters.FindIndex(c => c.loopId == loopId);
        if (index >= 0) SelectCharacter(index);
    }
}
