using UnityEngine;
using UnityEngine.Playables;
using System.Collections;

/// <summary>
/// 支持暂停、回溯、快进的 Timeline 管理器。
/// 全程使用 Pause/Resume 控制，不调用 Stop()，避免 playableGraph 销毁导致位置/旋转异常。
/// </summary>
public class TimelineRewindManager : MonoBehaviour
{
    [Header("Timeline 组件")]
    [SerializeField] private PlayableDirector _timelineDirector;

    [Header("回溯设置")]
    [Range(0.1f, 2.0f)]
    [SerializeField] private float _rewindSpeed = 0.5f;

    [Header("自动播放")]
    [SerializeField] private bool _autoPlayOnStart = true;

    // 内部状态
    private bool _isRewinding = false;
    private Coroutine _rewindCoroutine;
    private double _cachedDuration = 0;
    private bool _isPaused = false;

    private void Start()
    {
        if (_timelineDirector == null)
            _timelineDirector = GetComponent<PlayableDirector>();

        if (_timelineDirector != null && _timelineDirector.playableAsset != null)
        {
            // Hold模式：播完后暂停在最后一帧，playableGraph不销毁
            _timelineDirector.extrapolationMode = DirectorWrapMode.Hold;
            _cachedDuration = _timelineDirector.duration;

            if (_autoPlayOnStart)
                Play();
        }
    }

    private void OnDestroy()
    {
        StopRewinding();
    }

    /// <summary>
    /// 播放 Timeline（从当前时间继续，或从头开始）。
    /// </summary>
    public void Play()
    {
        if (_timelineDirector == null) return;

        StopRewinding();
        _isPaused = false;

        // 若graph未激活（首次播放），用Play()启动
        if (_timelineDirector.state != PlayState.Playing && _timelineDirector.state != PlayState.Paused)
        {
            _timelineDirector.Play();
        }
        else
        {
            _timelineDirector.Resume();
        }

        Debug.Log($"[Rewind] 播放，时间：{_timelineDirector.time:F2}");
    }

    /// <summary>
    /// 暂停 Timeline（保持playableGraph存活，NPC位置/旋转不变）。
    /// </summary>
    public void Pause()
    {
        if (_timelineDirector == null) return;

        StopRewinding();
        _isPaused = true;
        _timelineDirector.Pause();

        Debug.Log($"[Rewind] 暂停，时间：{_timelineDirector.time:F2}");
    }

    /// <summary>
    /// 恢复播放。
    /// </summary>
    public void Resume()
    {
        if (_timelineDirector == null) return;

        StopRewinding();
        _isPaused = false;
        _timelineDirector.Resume();

        Debug.Log($"[Rewind] 恢复播放，时间：{_timelineDirector.time:F2}");
    }

    /// <summary>
    /// 停止并重置 Timeline。
    /// </summary>
    public void Stop()
    {
        if (_timelineDirector == null) return;

        StopRewinding();
        _isPaused = false;
        _timelineDirector.Stop();
        Debug.Log("[Rewind] 停止");
    }

    /// <summary>
    /// 跳转到指定时间并刷新画面。
    /// </summary>
    public void ScrubTo(double time)
    {
        if (_timelineDirector == null) return;

        time = System.Math.Max(0, System.Math.Min(time, _cachedDuration));

        // 确保graph存活：如果未启动过，先Play再Pause
        if (_timelineDirector.state != PlayState.Playing && _timelineDirector.state != PlayState.Paused)
        {
            _timelineDirector.Play();
            _timelineDirector.Pause();
        }

        _timelineDirector.time = time;
        _timelineDirector.Evaluate();
    }

    /// <summary>
    /// 开始按住回溯。
    /// </summary>
    public void StartRewinding()
    {
        if (_timelineDirector == null) return;
        if (_isRewinding) return;
        if (_timelineDirector.playableAsset == null) return;

        // 暂停正常播放（不用Stop，保持graph存活）
        if (_timelineDirector.state == PlayState.Playing)
            _timelineDirector.Pause();

        _isRewinding = true;
        _isPaused = true;

        if (_rewindCoroutine != null)
            StopCoroutine(_rewindCoroutine);
        _rewindCoroutine = StartCoroutine(RewindCoroutine());

        Debug.Log($"[Rewind] 开始回溯，时间：{_timelineDirector.time:F2}");
    }

    /// <summary>
    /// 停止按住回溯。
    /// </summary>
    public void StopRewinding()
    {
        if (!_isRewinding) return;

        _isRewinding = false;

        if (_rewindCoroutine != null)
        {
            StopCoroutine(_rewindCoroutine);
            _rewindCoroutine = null;
        }

        Debug.Log($"[Rewind] 停止回溯，时间：{_timelineDirector?.time:F2}");
    }

    /// <summary>
    /// 回溯协程：每帧递减时间并Evaluate。
    /// </summary>
    private IEnumerator RewindCoroutine()
    {
        while (_isRewinding && _timelineDirector != null)
        {
            if (_timelineDirector.playableAsset == null) break;

            double step = Time.unscaledDeltaTime * _rewindSpeed;
            double newTime = _timelineDirector.time - step;

            if (newTime <= 0)
            {
                _timelineDirector.time = 0;
                _timelineDirector.Evaluate();
                Debug.Log("[Rewind] 回溯到起点");
                break;
            }

            _timelineDirector.time = newTime;
            _timelineDirector.Evaluate();

            yield return null;
        }

        _isRewinding = false;
        _rewindCoroutine = null;
    }

    public bool IsPlaying()
    {
        return !_isPaused && !_isRewinding && _timelineDirector != null && _timelineDirector.state == PlayState.Playing;
    }

    public bool IsRewinding() => _isRewinding;
    public bool IsPaused() => _isPaused;

    public float GetCurrentProgress()
    {
        if (_timelineDirector == null || _cachedDuration <= 0)
            return 0f;
        return (float)(_timelineDirector.time / _cachedDuration);
    }

    public void SetRewindSpeed(float speed)
    {
        _rewindSpeed = Mathf.Clamp(speed, 0.1f, 2.0f);
    }

    /// <summary>
    /// 运行时动态绑定新的 PlayableDirector（切换嫌疑人时调用）。
    /// </summary>
    public void BindDirector(PlayableDirector director)
    {
        StopRewinding();
        _timelineDirector = director;
        _cachedDuration = (director != null && director.playableAsset != null)
            ? director.duration : 0;
        _isPaused = false;
        _isRewinding = false;

        if (director != null)
            director.extrapolationMode = DirectorWrapMode.Hold;

        Debug.Log($"[Rewind] 绑定新 Director：{(director != null ? director.name : "null")}");
    }

    /// <summary>
    /// 获取当前绑定的 PlayableDirector。
    /// </summary>
    public PlayableDirector GetDirector() => _timelineDirector;
}
