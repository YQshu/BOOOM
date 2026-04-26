using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 项目启动辅助脚本。
/// 用于输出场景启动信息，并提供基础调试快捷键。
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    [Header("调试开关")]
    [SerializeField] private bool _logStartupInfo = true;
    [SerializeField] private bool _enableReloadCurrentScene = true;

    [Header("调试按键")]
    [SerializeField] private KeyCode _reloadSceneKey = KeyCode.R;

    /// <summary>
    /// 场景启动时输出关键调试信息。
    /// </summary>
    private void Start()
    {
        if (!_logStartupInfo)
        {
            return;
        }

        string sceneName = SceneManager.GetActiveScene().name;
        Debug.Log($"[Bootstrap] 场景已启动：{sceneName}", this);
    }

    /// <summary>
    /// 监听调试按键并执行对应操作。
    /// </summary>
    private void Update()
    {
        if (!_enableReloadCurrentScene)
        {
            return;
        }

        if (Input.GetKeyDown(_reloadSceneKey))
        {
            ReloadCurrentScene();
        }
    }

    /// <summary>
    /// 重新加载当前场景，便于快速回归测试。
    /// </summary>
    public void ReloadCurrentScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.buildIndex);
    }
}
