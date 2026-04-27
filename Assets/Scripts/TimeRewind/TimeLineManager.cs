using UnityEngine;
using UnityEngine.Playables;
using System.Collections;

/// <summary>
/// 支持按住回溯的 Timeline 管理器
/// </summary>
public class TimelineRewindManager : MonoBehaviour
{
    [Header("Timeline 组件")]
    [SerializeField] private PlayableDirector _timelineDirector;

    [Header("回溯设置")]
    [Tooltip("回溯速度（0.1-2.0），值越大倒放越快，推荐0.5")]
    [Range(0.1f, 2.0f)]
    [SerializeField] private float _rewindSpeed = 0.5f;

    [Header("自动播放")]
    [Tooltip("场景启动时自动播放 Timeline")]
    [SerializeField] private bool _autoPlayOnStart = true;

    // 内部状态
    private bool _isPlaying = false;
    private bool _isRewinding = false;
    private Coroutine _rewindCoroutine;
    private double _cachedDuration = 0;
    private bool _hasCompleted = false;  // 标记是否已播放完成

    private void Start()
    {
        if (_timelineDirector == null)
            _timelineDirector = GetComponent<PlayableDirector>();

        if (_timelineDirector != null)
        {
            _timelineDirector.stopped += OnTimelineStopped;
            _timelineDirector.played += OnTimelinePlayed;

            // 缓存时长
            if (_timelineDirector.playableAsset != null)
            {
                _cachedDuration = _timelineDirector.duration;
            }

            if (_autoPlayOnStart)
            {
                Play();
            }
        }
        else
        {
            Debug.LogError("TimelineRewindManager: 找不到 PlayableDirector 组件！");
        }
    }

    private void OnDestroy()
    {
        if (_timelineDirector != null)
        {
            _timelineDirector.stopped -= OnTimelineStopped;
            _timelineDirector.played -= OnTimelinePlayed;
        }
    }

    private void Update()
    {
        if (_timelineDirector == null) return;

        // 更新缓存的时长
        if (_timelineDirector.playableAsset != null)
        {
            _cachedDuration = _timelineDirector.duration;
        }

        // 检测是否到达结尾（播放完成）
        if (_isPlaying && _timelineDirector.state == PlayState.Playing)
        {
            if (_timelineDirector.time >= _cachedDuration - 0.01f && _cachedDuration > 0)
            {
                _hasCompleted = true;
                _isPlaying = false;
                Debug.Log("TimelineRewindManager: Timeline 播放完成");
            }
        }
    }

    /// <summary>
    /// 播放 Timeline
    /// </summary>
    public void Play()
    {
        if (_timelineDirector == null) return;

        // 如果正在回溯，停止回溯
        if (_isRewinding)
        {
            StopRewinding();
        }

        // 如果已经播放完成，重置到起点再播放
        if (_hasCompleted)
        {
            SeekToTime(0);
            _hasCompleted = false;
        }

        _isPlaying = true;
        _timelineDirector.Play();
        Debug.Log($"TimelineRewindManager: 开始播放");
    }

    /// <summary>
    /// 暂停 Timeline
    /// </summary>
    public void Pause()
    {
        if (_timelineDirector == null) return;

        // 如果已经播放完成，不允许暂停
        if (_hasCompleted)
        {
            Debug.Log("TimelineRewindManager: 已播放完成，请按播放键重新开始");
            return;
        }

        if (_timelineDirector.state == PlayState.Playing)
        {
            _timelineDirector.Pause();
            Debug.Log($"TimelineRewindManager: 已暂停");
        }
    }

    /// <summary>
    /// 恢复播放
    /// </summary>
    public void Resume()
    {
        if (_timelineDirector == null) return;

        // 如果已经播放完成，重新播放
        if (_hasCompleted)
        {
            Play();
            return;
        }

        StopRewinding();

        if (_timelineDirector.state != PlayState.Playing)
        {
            _timelineDirector.Resume();
            Debug.Log($"TimelineRewindManager: 恢复播放");
        }
    }

    /// <summary>
    /// 停止并重置 Timeline
    /// </summary>
    public void Stop()
    {
        if (_timelineDirector == null) return;

        StopRewinding();
        _timelineDirector.Stop();
        _hasCompleted = false;
        // OnTimelineStopped 回调会处理 _isPlaying
    }

    /// <summary>
    /// 跳转到指定时间
    /// </summary>
    public void SeekToTime(double time)
    {
        if (_timelineDirector == null) return;

        time = System.Math.Max(0, System.Math.Min(time, _cachedDuration));
        _timelineDirector.time = time;
        _timelineDirector.Evaluate();

        // 跳转后清除完成标记
        if (time < _cachedDuration - 0.01f)
        {
            _hasCompleted = false;
        }
    }

    /// <summary>
    /// 开始按住回溯（由输入脚本调用）
    /// </summary>
    public void StartRewinding()
    {
        if (_timelineDirector == null) return;
        if (_isRewinding) return;

        // 只有在 Timeline 存在时才回溯
        if (_timelineDirector.playableAsset == null) return;

        // 如果已经播放完成，不允许回溯（因为没有可回溯的内容）
        if (_hasCompleted)
        {
            Debug.Log("TimelineRewindManager: 已播放完成，无法回溯，请先重置或重新播放");
            return;
        }

        _isRewinding = true;

        // 如果正在播放，先暂停
        if (_timelineDirector.state == PlayState.Playing)
        {
            _timelineDirector.Pause();
        }

        // 启动回溯协程
        if (_rewindCoroutine != null)
        {
            StopCoroutine(_rewindCoroutine);
        }
        _rewindCoroutine = StartCoroutine(RewindCoroutine());

        Debug.Log("TimelineRewindManager: 开始按住回溯");
    }

    /// <summary>
    /// 停止按住回溯（由输入脚本调用）
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

        Debug.Log("TimelineRewindManager: 停止按住回溯");
    }

    /// <summary>
    /// 回溯协程 - 每帧以固定速度倒退
    /// </summary>
    private IEnumerator RewindCoroutine()
    {
        while (_isRewinding && _timelineDirector != null)
        {
            // 每帧倒退一小段时间
            double step = Time.unscaledDeltaTime * _rewindSpeed;
            double newTime = _timelineDirector.time - step;

            // 限制范围
            if (newTime <= 0)
            {
                newTime = 0;
                _timelineDirector.time = newTime;
                _timelineDirector.Evaluate();
                Debug.Log("TimelineRewindManager: 回溯到起点");
                // 清除完成标记（因为现在不在终点了）
                _hasCompleted = false;
                break; // 到达起点，停止回溯
            }

            newTime = System.Math.Max(0, System.Math.Min(newTime, _cachedDuration));
            _timelineDirector.time = newTime;
            _timelineDirector.Evaluate();

            // 回溯过程中清除完成标记
            if (_hasCompleted && newTime < _cachedDuration - 0.01f)
            {
                _hasCompleted = false;
            }

            yield return null;
        }

        _isRewinding = false;
        _rewindCoroutine = null;
    }

    private void OnTimelinePlayed(PlayableDirector director)
    {
        _isPlaying = true;
        Debug.Log("TimelineRewindManager: Timeline 开始播放");
    }

    private void OnTimelineStopped(PlayableDirector director)
    {
        _isPlaying = false;
        StopRewinding();
        Debug.Log("TimelineRewindManager: Timeline 已停止");
    }

    public bool IsPlaying()
    {
        return _isPlaying && _timelineDirector != null && _timelineDirector.state == PlayState.Playing && !_hasCompleted;
    }

    public bool IsRewinding() => _isRewinding;

    public float GetCurrentProgress()
    {
        if (_timelineDirector == null || _cachedDuration <= 0)
            return 0f;
        return (float)(_timelineDirector.time / _cachedDuration);
    }

    public double GetCurrentTime() => _timelineDirector != null ? _timelineDirector.time : 0;
    public double GetDuration() => _cachedDuration;

    public void SetRewindSpeed(float speed)
    {
        _rewindSpeed = Mathf.Clamp(speed, 0.1f, 2.0f);
    }

    public bool IsCompleted()
    {
        return _hasCompleted;
    }
}
