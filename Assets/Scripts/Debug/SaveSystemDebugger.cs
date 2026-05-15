using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 存档系统调试工具。
/// 用于诊断 SaveManager 和 ClueManager 的事件订阅问题。
/// </summary>
public class SaveSystemDebugger : MonoBehaviour
{
    [Header("UI 引用（可选）")]
    [SerializeField] private TMP_Text _debugText;

    [Header("调试选项")]
    [SerializeField] private bool _logOnStart = true;
    [SerializeField] private bool _logEverySecond = false;
    [SerializeField] private KeyCode _debugKey = KeyCode.F12;

    private float _timer = 0f;

    private void Start()
    {
        if (_logOnStart)
        {
            LogStatus();
        }
    }

    private void Update()
    {
        // 按键触发调试
        if (Input.GetKeyDown(_debugKey))
        {
            LogStatus();
        }

        // 每秒自动日志
        if (_logEverySecond)
        {
            _timer += Time.deltaTime;
            if (_timer >= 1f)
            {
                _timer = 0f;
                LogStatus();
            }
        }
    }

    private void LogStatus()
    {
        string status = GetDebugStatus();
        Debug.Log(status);

        if (_debugText != null)
        {
            _debugText.text = status;
        }
    }

    private string GetDebugStatus()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("=== 存档系统调试信息 ===");
        sb.AppendLine($"时间: {System.DateTime.Now:HH:mm:ss}");
        sb.AppendLine();

        // 检查 SaveManager
        sb.AppendLine("【SaveManager】");
        if (SaveManager.Instance == null)
        {
            sb.AppendLine("  ❌ Instance 为 null");
        }
        else
        {
            sb.AppendLine("  ✓ Instance 存在");
            sb.AppendLine($"  - GameObject: {SaveManager.Instance.gameObject.name}");
            sb.AppendLine($"  - 场景: {SaveManager.Instance.gameObject.scene.name}");

            // 检查事件订阅者数量
            var onClueCollectedField = typeof(SaveManager).GetField("OnClueCollected",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (onClueCollectedField != null)
            {
                var handler = onClueCollectedField.GetValue(SaveManager.Instance) as System.Delegate;
                int subscriberCount = handler?.GetInvocationList().Length ?? 0;
                sb.AppendLine($"  - OnClueCollected 订阅者: {subscriberCount}");
            }
        }
        sb.AppendLine();

        // 检查 ClueManager
        sb.AppendLine("【ClueManager】");
        if (ClueManager.Instance == null)
        {
            sb.AppendLine("  ❌ Instance 为 null");
        }
        else
        {
            sb.AppendLine("  ✓ Instance 存在");
            sb.AppendLine($"  - GameObject: {ClueManager.Instance.gameObject.name}");
            sb.AppendLine($"  - 场景: {ClueManager.Instance.gameObject.scene.name}");
            sb.AppendLine($"  - 已收集线索数: {ClueManager.Instance.GetCollectedCount()}");

            // 检查 OnClueCollected 事件订阅者数量
            var eventField = typeof(ClueManager).GetField("OnClueCollected",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            if (eventField != null)
            {
                var eventDelegate = eventField.GetValue(ClueManager.Instance) as System.Delegate;
                if (eventDelegate == null)
                {
                    sb.AppendLine("  - OnClueCollected 订阅者: 0 (事件为 null)");
                }
                else
                {
                    var invocationList = eventDelegate.GetInvocationList();
                    sb.AppendLine($"  - OnClueCollected 订阅者: {invocationList.Length}");
                    foreach (var subscriber in invocationList)
                    {
                        sb.AppendLine($"    • {subscriber.Target?.GetType().Name ?? "静态"}.{subscriber.Method.Name}");
                    }
                }
            }
        }
        sb.AppendLine();

        // 存档文件状态
        sb.AppendLine("【存档文件】");
        if (SaveManager.Instance != null)
        {
            bool hasSave = SaveManager.Instance.HasSaveFile();
            sb.AppendLine($"  - 存档存在: {(hasSave ? "是" : "否")}");
        }

        sb.AppendLine("======================");
        return sb.ToString();
    }

    /// <summary>
    /// 测试收集线索（用于验证事件触发）。
    /// </summary>
    [ContextMenu("测试收集线索")]
    public void TestCollectClue()
    {
        if (ClueManager.Instance == null)
        {
            Debug.LogError("[Debug] ClueManager.Instance 为 null，无法测试");
            return;
        }

        var allClues = ClueManager.Instance.GetAllClues();
        if (allClues == null || allClues.Count == 0)
        {
            Debug.LogError("[Debug] 线索数据库为空");
            return;
        }

        // 找到第一个未收集的线索
        ClueDataSO testClue = null;
        foreach (var clue in allClues)
        {
            if (clue != null && !ClueManager.Instance.HasClue(clue))
            {
                testClue = clue;
                break;
            }
        }

        if (testClue == null)
        {
            Debug.Log("[Debug] 所有线索已收集");
            return;
        }

        Debug.Log($"[Debug] 测试收集线索: {testClue.clueId}");
        ClueManager.Instance.CollectClue(testClue);

        // 延迟检查状态
        Invoke(nameof(LogStatus), 0.5f);
    }

    /// <summary>
    /// 手动触发 SaveManager 重新订阅。
    /// </summary>
    [ContextMenu("强制重新订阅")]
    public void ForceResubscribe()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogError("[Debug] SaveManager.Instance 为 null");
            return;
        }

        // 通过反射调用私有方法
        var method = typeof(SaveManager).GetMethod("TrySubscribeToClueManager",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (method != null)
        {
            method.Invoke(SaveManager.Instance, null);
            Debug.Log("[Debug] 已调用 TrySubscribeToClueManager()");
            Invoke(nameof(LogStatus), 0.1f);
        }
        else
        {
            Debug.LogError("[Debug] 找不到 TrySubscribeToClueManager 方法");
        }
    }
}
