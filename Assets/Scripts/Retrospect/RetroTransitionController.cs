using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BOOOM.Retrospect
{
    /// <summary>
    /// 控制回溯时的屏幕效果，支持穿梭过场动画
    /// 在进入回溯时播放穿梭效果，然后稳定到正常回溯效果
    /// </summary>
    public class RetroTransitionController : MonoBehaviour
    {
        [Header("Volume 引用")]
        [SerializeField] private Volume _retroVolume;

        [Header("穿梭过场效果")]
        [SerializeField] private bool _enableTransition = true;
        [SerializeField] private float _transitionDuration = 1.0f;
        [Tooltip("穿梭时的最大扭曲强度")]
        [SerializeField, Range(-1f, 1f)] private float _transitionDistortionPeak = -0.8f;
        [Tooltip("穿梭时的最大色差强度")]
        [SerializeField, Range(0f, 1f)] private float _transitionChromaticPeak = 1.0f;
        [Tooltip("穿梭时的最大模糊强度（需要添加 Motion Blur）")]
        [SerializeField, Range(0f, 1f)] private float _transitionBlurIntensity = 0.5f;
        [Tooltip("穿梭动画曲线（控制加速/减速）")]
        [SerializeField] private AnimationCurve _transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [Tooltip("使用弹性曲线增强拉伸感")]
        [SerializeField] private bool _useElasticCurve = true;

        [Header("稳定回溯效果")]
        [SerializeField, Range(0f, 1f)] private float _targetWeight = 0.75f;
        [SerializeField] private float _fadeOutDuration = 0.3f;

        [Header("正常效果参数（稳定状态）")]
        [SerializeField, Range(-1f, 1f)] private float _normalDistortion = -0.12f;
        [SerializeField, Range(0f, 1f)] private float _normalChromatic = 0.35f;

        // Volume Profile 中的效果引用
        private LensDistortion _lensDistortion;
        private ChromaticAberration _chromaticAberration;
        private Vignette _vignette;
        private FilmGrain _filmGrain;
        private MotionBlur _motionBlur;

        private Coroutine _currentTransition;
        private bool _isInRetrospect = false;

        private void Awake()
        {
            if (_retroVolume != null)
            {
                _retroVolume.weight = 0f;
                CacheVolumeEffects();
            }
        }

        private void Start()
        {
            // 在 Start 中订阅，确保 RetrospectManager 已初始化
            if (RetrospectManager.Instance != null)
            {
                RetrospectManager.Instance.OnRetrospectEnter += OnEnterRetrospect;
                RetrospectManager.Instance.OnRetrospectExit += OnExitRetrospect;
                Debug.Log("[RetroTransition] 事件订阅成功");
            }
            else
            {
                Debug.LogError("[RetroTransition] RetrospectManager.Instance 为空，无法订阅事件！");
            }
        }

        private void OnDestroy()
        {
            // 取消订阅
            if (RetrospectManager.Instance != null)
            {
                RetrospectManager.Instance.OnRetrospectEnter -= OnEnterRetrospect;
                RetrospectManager.Instance.OnRetrospectExit -= OnExitRetrospect;
            }
        }

        /// <summary>
        /// 缓存 Volume Profile 中的效果引用
        /// </summary>
        private void CacheVolumeEffects()
        {
            if (_retroVolume == null || _retroVolume.profile == null) return;

            _retroVolume.profile.TryGet(out _lensDistortion);
            _retroVolume.profile.TryGet(out _chromaticAberration);
            _retroVolume.profile.TryGet(out _vignette);
            _retroVolume.profile.TryGet(out _filmGrain);
            _retroVolume.profile.TryGet(out _motionBlur);
        }

        /// <summary>
        /// 进入回溯时触发
        /// </summary>
        private void OnEnterRetrospect()
        {
            Debug.Log("[RetroTransition] OnEnterRetrospect 被调用！");

            if (_retroVolume == null)
            {
                Debug.LogWarning("[RetroTransition] Volume 未设置！");
                return;
            }

            _isInRetrospect = true;

            // 停止之前的过渡动画
            if (_currentTransition != null)
            {
                StopCoroutine(_currentTransition);
            }

            // 启动穿梭过场效果
            if (_enableTransition)
            {
                Debug.Log("[RetroTransition] 开始播放穿梭效果");
                _currentTransition = StartCoroutine(PlayTransitionEffect());
            }
            else
            {
                Debug.Log("[RetroTransition] 直接淡入（无过场）");
                // 直接淡入到正常效果
                _currentTransition = StartCoroutine(FadeToNormal());
            }
        }

        /// <summary>
        /// 退出回溯时触发
        /// </summary>
        private void OnExitRetrospect()
        {
            Debug.Log("[RetroTransition] OnExitRetrospect 被调用！");

            if (_retroVolume == null) return;

            _isInRetrospect = false;

            // 停止之前的过渡动画
            if (_currentTransition != null)
            {
                StopCoroutine(_currentTransition);
            }

            // 淡出效果
            _currentTransition = StartCoroutine(FadeOut());
        }

        /// <summary>
        /// 播放穿梭过场效果（增强版：更夸张的拉伸）
        /// </summary>
        private IEnumerator PlayTransitionEffect()
        {
            float elapsed = 0f;

            // 保存原始暗角强度
            float originalVignetteIntensity = _vignette != null ? _vignette.intensity.value : 0f;

            // 设置初始参数为正常值
            SetEffectParameters(_normalDistortion, _normalChromatic);
            _retroVolume.weight = 0f;

            while (elapsed < _transitionDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / _transitionDuration;

                // 使用弹性曲线或标准曲线
                float curveValue = _useElasticCurve ? EvaluateElasticCurve(progress) : _transitionCurve.Evaluate(progress);

                // 第一阶段（0-0.3）：快速增强扭曲和色差，模拟"冲入"时空
                if (progress < 0.3f)
                {
                    float phase1 = progress / 0.3f;
                    float distortion = Mathf.Lerp(_normalDistortion, _transitionDistortionPeak, phase1);
                    float chromatic = Mathf.Lerp(_normalChromatic, _transitionChromaticPeak, phase1);
                    float weight = Mathf.Lerp(0f, _targetWeight, phase1);

                    SetEffectParameters(distortion, chromatic);
                    _retroVolume.weight = weight;

                    // 在冲刺阶段增强暗角
                    if (_vignette != null)
                    {
                        float vignetteBoost = Mathf.Lerp(0f, 0.4f, phase1);
                        _vignette.intensity.value = originalVignetteIntensity + vignetteBoost;
                    }

                    // 在冲刺阶段添加运动模糊
                    if (_motionBlur != null)
                    {
                        _motionBlur.intensity.value = Mathf.Lerp(0f, _transitionBlurIntensity, phase1);
                    }
                }
                // 第二阶段（0.3-1.0）：逐渐稳定到正常回溯效果
                else
                {
                    float phase2 = (progress - 0.3f) / 0.7f;
                    float distortion = Mathf.Lerp(_transitionDistortionPeak, _normalDistortion, phase2);
                    float chromatic = Mathf.Lerp(_transitionChromaticPeak, _normalChromatic, phase2);

                    SetEffectParameters(distortion, chromatic);
                    _retroVolume.weight = _targetWeight;

                    // 逐渐恢复暗角
                    if (_vignette != null)
                    {
                        float vignetteBoost = Mathf.Lerp(0.4f, 0f, phase2);
                        _vignette.intensity.value = originalVignetteIntensity + vignetteBoost;
                    }

                    // 逐渐减弱运动模糊
                    if (_motionBlur != null)
                    {
                        _motionBlur.intensity.value = Mathf.Lerp(_transitionBlurIntensity, 0f, phase2);
                    }
                }

                yield return null;
            }

            // 确保最终状态正确
            SetEffectParameters(_normalDistortion, _normalChromatic);
            _retroVolume.weight = _targetWeight;

            // 恢复暗角
            if (_vignette != null)
            {
                _vignette.intensity.value = originalVignetteIntensity;
            }

            // 关闭运动模糊
            if (_motionBlur != null)
            {
                _motionBlur.intensity.value = 0f;
            }
        }

        /// <summary>
        /// 弹性曲线：产生更夸张的拉伸感
        /// 先快速冲刺到峰值，然后有弹性地回弹到目标值
        /// </summary>
        private float EvaluateElasticCurve(float t)
        {
            // 前30%快速冲到峰值
            if (t < 0.3f)
            {
                return Mathf.Pow(t / 0.3f, 2.5f) * 0.15f; // 快速加速
            }
            // 30%-60%有一个小回弹（弹性效果）
            else if (t < 0.6f)
            {
                float localT = (t - 0.3f) / 0.3f;
                // 使用正弦波产生弹性回弹
                return 0.15f + Mathf.Sin(localT * Mathf.PI) * 0.2f + localT * 0.35f;
            }
            // 60%-100%平滑到最终值
            else
            {
                float localT = (t - 0.6f) / 0.4f;
                return 0.7f + localT * 0.3f; // 平滑收尾
            }
        }

        /// <summary>
        /// 直接淡入到正常效果（无过场）
        /// </summary>
        private IEnumerator FadeToNormal()
        {
            float elapsed = 0f;
            float duration = 0.5f;

            SetEffectParameters(_normalDistortion, _normalChromatic);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                _retroVolume.weight = Mathf.Lerp(0f, _targetWeight, progress);
                yield return null;
            }

            _retroVolume.weight = _targetWeight;
        }

        /// <summary>
        /// 淡出效果
        /// </summary>
        private IEnumerator FadeOut()
        {
            float startWeight = _retroVolume.weight;
            float elapsed = 0f;

            while (elapsed < _fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / _fadeOutDuration);
                _retroVolume.weight = Mathf.Lerp(startWeight, 0f, progress);
                yield return null;
            }

            _retroVolume.weight = 0f;
        }

        /// <summary>
        /// 设置效果参数
        /// </summary>
        private void SetEffectParameters(float distortion, float chromatic)
        {
            if (_lensDistortion != null)
            {
                _lensDistortion.intensity.value = distortion;
            }

            if (_chromaticAberration != null)
            {
                _chromaticAberration.intensity.value = chromatic;
            }
        }

        /// <summary>
        /// 立即设置效果（用于测试）
        /// </summary>
        public void SetEffectImmediate(bool enabled)
        {
            if (_retroVolume == null) return;

            if (_currentTransition != null)
            {
                StopCoroutine(_currentTransition);
            }

            if (enabled)
            {
                SetEffectParameters(_normalDistortion, _normalChromatic);
                _retroVolume.weight = _targetWeight;
            }
            else
            {
                _retroVolume.weight = 0f;
            }
        }

#if UNITY_EDITOR
        [ContextMenu("测试：播放穿梭效果")]
        private void TestTransition()
        {
            if (!Application.isPlaying) return;

            if (_currentTransition != null)
            {
                StopCoroutine(_currentTransition);
            }
            _currentTransition = StartCoroutine(PlayTransitionEffect());
        }

        [ContextMenu("测试：启用效果（无过场）")]
        private void TestEnable()
        {
            if (!Application.isPlaying) return;
            SetEffectImmediate(true);
        }

        [ContextMenu("测试：禁用效果")]
        private void TestDisable()
        {
            if (!Application.isPlaying) return;
            SetEffectImmediate(false);
        }
#endif
    }
}
