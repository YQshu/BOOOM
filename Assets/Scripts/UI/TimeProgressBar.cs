using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Playables;
using UnityEngine.UI;

/// <summary>
/// 时间进度条UI。
/// 实时显示PlayableDirector的播放进度，支持拖动跳转。
/// 挂载在Slider所在的GameObject上。
/// </summary>
[RequireComponent(typeof(Slider))]
public class TimeProgressBar : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("引用")]
    [Tooltip("要追踪的PlayableDirector，留空则运行时自动查找")]
    [SerializeField] private PlayableDirector _director;

    [Header("配置")]
    [Tooltip("是否允许拖动进度条跳转时间")]
    [SerializeField] private bool _allowScrubbing = true;

    private Slider _slider;
    private bool _isDragging;

    private void Awake()
    {
        _slider = GetComponent<Slider>();
    }

    private void Start()
    {
        if (_director == null)
            _director = FindObjectOfType<PlayableDirector>();

        _slider.minValue = 0f;
        _slider.maxValue = 1f;
        _slider.interactable = _allowScrubbing;
    }

    private void Update()
    {
        // 拖动期间不自动更新，避免与用户输入冲突
        if (_director == null || _isDragging) return;

        double duration = _director.duration;
        if (duration <= 0) return;

        _slider.SetValueWithoutNotify((float)(_director.time / duration));
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!_allowScrubbing) return;
        _isDragging = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_allowScrubbing) return;
        _isDragging = false;

        // 拖动结束后同步一次时间
        if (_director != null)
        {
            _director.time = _slider.value * _director.duration;
            _director.Evaluate();
        }
    }

    /// <summary>
    /// 运行时动态绑定到新的PlayableDirector（切换角色Timeline时调用）。
    /// </summary>
    public void BindDirector(PlayableDirector director)
    {
        _director = director;
    }
}
