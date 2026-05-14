using System.Collections;
using UnityEngine;

/// <summary>
/// 游戏场景初始化器。
/// 检查是否需要加载存档，并在所有单例初始化后执行加载。
/// </summary>
public class GameSceneInitializer : MonoBehaviour
{
    [Header("调试")]
    [SerializeField] private bool _enableLog = true;

    private const string LoadSaveKey = "LoadSaveOnStart";

    private void Start()
    {
        StartCoroutine(InitializeRoutine());
    }

    private IEnumerator InitializeRoutine()
    {
        // 延迟一帧，确保所有单例已初始化
        yield return null;

        // 检查是否需要加载存档
        if (PlayerPrefs.GetInt(LoadSaveKey, 0) == 1)
        {
            if (_enableLog)
                Debug.Log("[Init] 检测到加载存档标记，开始加载...");

            // 清除标记
            PlayerPrefs.DeleteKey(LoadSaveKey);
            PlayerPrefs.Save();

            // 加载存档
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.LoadGame();
            }
            else
            {
                Debug.LogError("[Init] SaveManager 未找到，无法加载存档");
            }
        }
        else
        {
            if (_enableLog)
                Debug.Log("[Init] 新游戏开始");
        }
    }

    /// <summary>
    /// 设置加载存档标记（主菜单调用）。
    /// </summary>
    public static void SetLoadSaveFlag()
    {
        PlayerPrefs.SetInt(LoadSaveKey, 1);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 清除加载存档标记。
    /// </summary>
    public static void ClearLoadSaveFlag()
    {
        PlayerPrefs.DeleteKey(LoadSaveKey);
        PlayerPrefs.Save();
    }
}
