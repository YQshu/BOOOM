using UnityEngine;
using System.Collections;

/// <summary>
/// 游戏计时器辅助类，用于管理协程和延迟调用
/// </summary>
public class GameTimer : MonoBehaviour
{
    private static GameTimer _instance;
    private static readonly object _lock = new object();

    public static GameTimer Instance
    {
        get
        {
            // 只在播放模式下工作
            if (!Application.isPlaying) return null;

            lock (_lock)
            {
                if (_instance == null)
                {
                    // 先尝试在场景中查找已存在的
                    _instance = FindObjectOfType<GameTimer>();

                    if (_instance == null)
                    {
                        GameObject obj = new GameObject("GameTimer");
                        _instance = obj.AddComponent<GameTimer>();
                        DontDestroyOnLoad(obj);
                        Debug.Log("GameTimer: 已自动创建");
                    }
                }
                return _instance;
            }
        }
    }

    /// <summary>
    /// 启动协程（静态方法）
    /// </summary>
    public static Coroutine StartCoroutineStatic(IEnumerator coroutine)
    {
        if (Instance == null)
        {
            Debug.LogError("GameTimer: Instance 为空，无法启动协程");
            return null;
        }
        return Instance.StartCoroutineInternal(coroutine);
    }

    private Coroutine StartCoroutineInternal(IEnumerator coroutine)
    {
        return StartCoroutine(coroutine);
    }

    /// <summary>
    /// 停止协程（静态方法）
    /// </summary>
    public static void StopCoroutineStatic(Coroutine coroutine)
    {
        if (_instance != null && coroutine != null)
        {
            _instance.StopCoroutineInternal(coroutine);
        }
    }

    private void StopCoroutineInternal(Coroutine coroutine)
    {
        StopCoroutine(coroutine);
    }

    /// <summary>
    /// 延迟调用（静态方法）
    /// </summary>
    public static void DelayCall(float delay, System.Action callback)
    {
        if (Instance == null)
        {
            Debug.LogError("GameTimer: Instance 为空，无法延迟调用");
            callback?.Invoke();
            return;
        }
        Instance.StartCoroutine(DelayCallRoutine(delay, callback));
    }

    private static IEnumerator DelayCallRoutine(float delay, System.Action callback)
    {
        yield return new WaitForSecondsRealtime(delay);
        callback?.Invoke();
    }

    private void Awake()
    {
        // 防止重复创建
        if (_instance != null && _instance != this)
        {
            Debug.Log($"GameTimer: 销毁重复实例 {gameObject.name}");
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
            Debug.Log("GameTimer: 实例已销毁");
        }
    }

    // 可选：场景切换时输出日志（调试用）
    private void OnEnable()
    {
        Debug.Log($"GameTimer: 已激活，实例ID={GetInstanceID()}");
    }
}
