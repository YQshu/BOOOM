using UnityEngine;

/// <summary>
/// 视野遮罩控制器（SpriteMask方案，无需Shader）。
///
/// 使用方式：
/// 1. 在Canvas（Screen Space Overlay）下创建全屏Image作为DarkOverlay
///    - Color: (0, 0, 0, 0.65)
///    - Mask Interaction: Visible Outside Mask
/// 2. 在NPC子物体上挂SpriteMask + 本脚本
///    - SpriteMask使用圆形Sprite
/// 3. DarkOverlay会在SpriteMask圆形区域内保持透明，圆外变暗
/// </summary>
[RequireComponent(typeof(SpriteMask))]
public class VisionMaskController : MonoBehaviour
{
    [Header("视野配置")]
    [Tooltip("视野圆形半径（Unity单位）")]
    [SerializeField] private float _radius = 4f;
    [Tooltip("是否在运行时动态跟随父物体（通常保持true）")]
    [SerializeField] private bool _followParent = true;

    [Header("动画（可选）")]
    [Tooltip("是否启用呼吸感脉冲动画")]
    [SerializeField] private bool _enablePulse;
    [Tooltip("脉冲幅度（半径变化量）")]
    [SerializeField] private float _pulseAmplitude = 0.2f;
    [Tooltip("脉冲速度")]
    [SerializeField] private float _pulseSpeed = 1.5f;

    private SpriteMask _mask;
    private float _baseRadius;

    private void Awake()
    {
        _mask = GetComponent<SpriteMask>();
        _baseRadius = _radius;
    }

    private void Start()
    {
        ApplyRadius(_radius);
    }

    private void Update()
    {
        if (!_enablePulse) return;

        float pulse = Mathf.Sin(Time.time * _pulseSpeed) * _pulseAmplitude;
        ApplyRadius(_baseRadius + pulse);
    }

    /// <summary>
    /// 设置视野半径。
    /// </summary>
    public void SetRadius(float radius)
    {
        _baseRadius = radius;
        _radius = radius;
        ApplyRadius(radius);
    }

    /// <summary>
    /// 显示/隐藏视野遮罩（切换角色时使用）。
    /// </summary>
    public void SetVisible(bool visible)
    {
        _mask.enabled = visible;
    }

    private void ApplyRadius(float r)
    {
        // SpriteMask的缩放直接决定遮罩大小
        // 圆形Sprite默认直径为1单位，所以scale = diameter = radius * 2
        transform.localScale = new Vector3(r * 2f, r * 2f, 1f);
    }
}
