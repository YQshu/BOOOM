using UnityEngine;

/// <summary>
/// SaveManager 诊断工具。
/// 在游戏场景中添加此组件，检查 SaveManager 是否正常工作。
/// </summary>
public class SaveManagerDebugger : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("=== SaveManager 诊断开始 ===");

        // 检查 SaveManager 是否存在
        if (SaveManager.Instance == null)
        {
            Debug.LogError("[诊断] SaveManager.Instance 为 null！");
            Debug.LogError("[诊断] 请在场景中创建一个空物体，添加 SaveManager 组件");
        }
        else
        {
            Debug.Log("[诊断] ✓ SaveManager.Instance 存在");
            Debug.Log($"[诊断] SaveManager 对象名称：{SaveManager.Instance.gameObject.name}");
        }

        // 检查 ClueManager 是否存在
        if (ClueManager.Instance == null)
        {
            Debug.LogWarning("[诊断] ClueManager.Instance 为 null");
        }
        else
        {
            Debug.Log("[诊断] ✓ ClueManager.Instance 存在");

            // 延迟检查事件订阅
            StartCoroutine(CheckEventSubscription());
        }

        // 检查 PlayerController 是否存在
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player == null)
        {
            Debug.LogWarning("[诊断] 场景中没有 PlayerController");
        }
        else
        {
            Debug.Log("[诊断] ✓ PlayerController 存在");
        }

        // 检查 RoomManager 是否存在
        if (RoomManager.Instance == null)
        {
            Debug.LogWarning("[诊断] RoomManager.Instance 为 null");
        }
        else
        {
            Debug.Log("[诊断] ✓ RoomManager.Instance 存在");
        }

        Debug.Log("=== SaveManager 诊断结束 ===");
    }

    private System.Collections.IEnumerator CheckEventSubscription()
    {
        // 等待 2 秒，确保订阅完成
        yield return new WaitForSeconds(2f);

        Debug.Log("=== 检查事件订阅 ===");

        if (ClueManager.Instance != null)
        {
            // 尝试触发一个测试事件
            Debug.Log("[诊断] 准备测试线索收集事件...");
            Debug.Log("[诊断] 请手动收集一个线索，观察是否触发自动保存");
        }
    }

    [ContextMenu("测试保存")]
    private void TestSave()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveGame();
            Debug.Log("[诊断] 手动触发保存");
        }
        else
        {
            Debug.LogError("[诊断] SaveManager.Instance 为 null，无法保存");
        }
    }

    [ContextMenu("测试加载")]
    private void TestLoad()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.LoadGame();
            Debug.Log("[诊断] 手动触发加载");
        }
        else
        {
            Debug.LogError("[诊断] SaveManager.Instance 为 null，无法加载");
        }
    }

    [ContextMenu("检查存档文件")]
    private void CheckSaveFile()
    {
        if (SaveManager.Instance != null)
        {
            bool hasSave = SaveManager.Instance.HasSaveFile();
            Debug.Log($"[诊断] 存档文件存在：{hasSave}");
        }
        else
        {
            Debug.LogError("[诊断] SaveManager.Instance 为 null");
        }
    }
}
