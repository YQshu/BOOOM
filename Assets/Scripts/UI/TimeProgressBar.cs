using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Playables;
using UnityEngine.UI;

/// <summary>
/// 时间进度条UI。
/// 实时显示PlayableDirector的播放进度，支持拖动跳转。
/// 拖动过程中实时更新NPC位置。
/// </summary>
[RequireComponent(typeof(Slider))]
public class TimeProgressBar : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("引用")]
    [Tooltip("要追踪的PlayableDirector，留空则运行时自动查找")]
    [SerializeField] private PlayableDirector _director;
    [Tooltip("TimelineRewindManager引用，留空则自动查找")]
    [SerializeField] private TimelineRewindManager _rewindManager;

    [Header("配置")]
    [Tooltip("是否允许拖动进度条跳转时间")]
    [SerializeField] private bool _allowScrubbing = true;

    private Slider _slider;
    private bool _isDragging;
    private bool _wasPlayingBeforeDrag;

    private void Awake()
    {
        _slider = GetComponent<Slider>();
    }

    private void Start()
    {
        if (_director == null)
            _director = FindObjectOfType<PlayableDirector>();
        if (_rewindManager == null)
            _rewindManager = FindObjectOfType<TimelineRewindManager>();

        _slider.minValue = 0f;
        _slider.maxValue = 1f;
        _slider.interactable = _allowScrubbing;
    }

    private void Update()
    {
        if (_director == null || _isDragging) return;

        double duration = _director.duration;
        if (duration <= 0) return;

        _slider.SetValueWithoutNotify((float)(_director.time / duration));
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!_allowScrubbing || _director == null) return;
        _isDragging = true;

        _wasPlayingBeforeDrag = _director.state == PlayState.Playing;

        // 暂停进入拖拽模式
        if (_wasPlayingBeforeDrag)
        {
            if (_rewindManager != null)
                _rewindManager.Pause();
            else
                _director.Pause();
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_allowScrubbing || !_isDragging || _director == null) return;

        double duration = _director.duration;
        if (duration <= 0) return;

        double targetTime = _slider.value * duration;

        // 通过ScrubTo确保graph存活并正确刷新位置
        if (_rewindManager != null)
            _rewindManager.ScrubTo(targetTime);
        else
        {
            _director.time = targetTime;
            _director.Evaluate();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_allowScrubbing || _director == null) return;
        _isDragging = false;

        // 最终同步
        double targetTime = _slider.value * _director.duration;
        if (_rewindManager != null)
            _rewindManager.ScrubTo(targetTime);
        else
        {
            _director.time = targetTime;
            _director.Evaluate();
        }

        // 拖拽前在播放则恢复
        if (_wasPlayingBeforeDrag)
        {
            if (_rewindManager != null)
                _rewindManager.Resume();
            else
                _director.Resume();
        }
    }

    /// <summary>
    /// 运行时动态绑定到新的PlayableDirector。
    /// </summary>
    public void BindDirector(PlayableDirector director)
    {
        _director = director;
    }

    /// <summary>
    /// 设置进度条是否可交互。
    /// </summary>
    public void SetInteractable(bool interactable)
    {
        _allowScrubbing = interactable;
        if (_slider != null)
            _slider.interactable = interactable;
    }
}
