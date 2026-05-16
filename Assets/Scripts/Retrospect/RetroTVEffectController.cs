using UnityEngine;
using UnityEngine.Rendering;

namespace BOOOM.Retrospect
{
    /// <summary>
    /// 控制回溯时的 RetroTV 屏幕效果
    /// 在进入回溯时启用复古电视效果，退出时禁用
    /// </summary>
    public class RetroTVEffectController : MonoBehaviour
    {
        [Header("效果设置")]
        [SerializeField] private Volume _retroTVVolume;
        [SerializeField] private float _fadeInDuration = 0.5f;
        [SerializeField] private float _fadeOutDuration = 0.3f;

        [Header("效果强度")]
        [SerializeField, Range(0f, 1f)] private float _targetWeight = 0.8f;

        private float _currentWeight = 0f;
        private bool _isTransitioning = false;
        private float _transitionTimer = 0f;
        private bool _isFadingIn = false;

        private void Awake()
        {
            if (_retroTVVolume != null)
            {
                _retroTVVolume.weight = 0f;
            }
        }

        private void OnEnable()
        {
            // 订阅回溯事件
            if (RetrospectManager.Instance != null)
            {
                RetrospectManager.Instance.OnRetrospectEnter += EnableEffect;
                RetrospectManager.Instance.OnRetrospectExit += DisableEffect;
            }
        }

        private void OnDisable()
        {
            // 取消订阅
            if (RetrospectManager.Instance != null)
            {
                RetrospectManager.Instance.OnRetrospectEnter -= EnableEffect;
                RetrospectManager.Instance.OnRetrospectExit -= DisableEffect;
            }
        }

        private void Update()
        {
            if (_isTransitioning && _retroTVVolume != null)
            {
                _transitionTimer += Time.deltaTime;

                float duration = _isFadingIn ? _fadeInDuration : _fadeOutDuration;
                float progress = Mathf.Clamp01(_transitionTimer / duration);

                if (_isFadingIn)
                {
                    _currentWeight = Mathf.Lerp(0f, _targetWeight, progress);
                }
                else
                {
                    _currentWeight = Mathf.Lerp(_targetWeight, 0f, progress);
                }

                _retroTVVolume.weight = _currentWeight;

                if (progress >= 1f)
                {
                    _isTransitioning = false;
                }
            }
        }

        /// <summary>
        /// 启用 RetroTV 效果（进入回溯时调用）
        /// </summary>
        public void EnableEffect()
        {
            if (_retroTVVolume == null)
            {
                Debug.LogWarning("RetroTV Volume 未设置！");
                return;
            }

            _isFadingIn = true;
            _isTransitioning = true;
            _transitionTimer = 0f;
        }

        /// <summary>
        /// 禁用 RetroTV 效果（退出回溯时调用）
        /// </summary>
        public void DisableEffect()
        {
            if (_retroTVVolume == null) return;

            _isFadingIn = false;
            _isTransitioning = true;
            _transitionTimer = 0f;
        }

        /// <summary>
        /// 立即设置效果强度（用于测试）
        /// </summary>
        public void SetEffectImmediate(bool enabled)
        {
            if (_retroTVVolume == null) return;

            _isTransitioning = false;
            _currentWeight = enabled ? _targetWeight : 0f;
            _retroTVVolume.weight = _currentWeight;
        }

#if UNITY_EDITOR
        [ContextMenu("测试：启用效果")]
        private void TestEnable()
        {
            EnableEffect();
        }

        [ContextMenu("测试：禁用效果")]
        private void TestDisable()
        {
            DisableEffect();
        }
#endif
    }
}
