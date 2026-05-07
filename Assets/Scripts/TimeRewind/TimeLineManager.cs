using UnityEngine;
using UnityEngine.Playables;
using System.Collections;

/// <summary>
/// 支持按住回溯的 Timeline 管理器 - 完全重写版
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
            _cachedDuration = _timelineDirector.duration;

            if (_autoPlayOnStart)
            {
                Play();
            }
        }
    }

    private void OnDestroy()
    {
        StopRewinding();
    }

    /// <summary>
    /// 播放 Timeline
    /// </summary>
    public void Play()
    {
        if (_timelineDirector == null) return;

        StopRewinding();
        _isPaused = false;
        _timelineDirector.Play();
        Debug.Log($"Timeline 播放");
    }

    /// <summary>
    /// 暂停 Timeline
    /// </summary>
    public void Pause()
    {
        if (_timelineDirector == null) return;

        // 保存当前时间
        double currentTime = _timelineDirector.time;
        Debug.Log($"暂停请求，当前时间: {currentTime}");

        // 停止播放
        _timelineDirector.Stop();

        // 重新设置到相同时间并暂停
        _timelineDirector.time = currentTime;
        _timelineDirector.Evaluate();
        _isPaused = true;

        Debug.Log($"暂停完成，时间保持在: {_timelineDirector.time}");
    }

    /// <summary>
    /// 恢复播放
    /// </summary>
    public void Resume()
    {
        if (_timelineDirector == null) return;

        StopRewinding();
        _isPaused = false;
        _timelineDirector.Play();
        Debug.Log($"Timeline 恢复播放");
    }

    /// <summary>
    /// 停止并重置 Timeline
    /// </summary>
    public void Stop()
    {
        if (_timelineDirector == null) return;

        StopRewinding();
        _isPaused = false;
        _timelineDirector.Stop();
        Debug.Log($"Timeline 停止");
    }

    /// <summary>
    /// 开始按住回溯
    /// </summary>
    public void StartRewinding()
    {
        if (_timelineDirector == null) return;
        if (_isRewinding) return;
        if (_timelineDirector.playableAsset == null) return;

        // 如果正在播放，先停止
        if (_timelineDirector.state == PlayState.Playing)
        {
            double currentTime = _timelineDirector.time;
            _timelineDirector.Stop();
            _timelineDirector.time = currentTime;
            _timelineDirector.Evaluate();
        }

        _isRewinding = true;
        _isPaused = true;

        if (_rewindCoroutine != null)
        {
            StopCoroutine(_rewindCoroutine);
        }
        _rewindCoroutine = StartCoroutine(RewindCoroutine());

        Debug.Log($"开始回溯，起始时间: {_timelineDirector.time}");
    }

    /// <summary>
    /// 停止按住回溯
    /// </summary>
    public void StopRewinding()
    {
        if (!_isRewinding) return;

        double currentTime = _timelineDirector != null ? _timelineDirector.time : 0;
        Debug.Log($"停止回溯，当前时间: {currentTime}");

        _isRewinding = false;

        if (_rewindCoroutine != null)
        {
            StopCoroutine(_rewindCoroutine);
            _rewindCoroutine = null;
        }

        // 确保画面停留在当前位置
        if (_timelineDirector != null)
        {
            _timelineDirector.time = currentTime;
            _timelineDirector.Evaluate();
        }
    }

    /// <summary>
    /// 回溯协程
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
                newTime = 0;
                _timelineDirector.time = newTime;
                _timelineDirector.Evaluate();
                Debug.Log("回溯到起点");
                break;
            }

            newTime = System.Math.Max(0, System.Math.Min(newTime, _cachedDuration));
            _timelineDirector.time = newTime;
            _timelineDirector.Evaluate();

            yield return null;
        }

        _isRewinding = false;
        _rewindCoroutine = null;
        Debug.Log($"回溯结束，最终时间: {_timelineDirector?.time}");
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
}
