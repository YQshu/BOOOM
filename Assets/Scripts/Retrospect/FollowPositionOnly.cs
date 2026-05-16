using UnityEngine;

namespace BOOOM.Retrospect
{
    /// <summary>
    /// 只跟随目标的位置，不跟随旋转
    /// 用于让故障效果遮罩跟随人物移动但保持朝向
    /// </summary>
    public class FollowPositionOnly : MonoBehaviour
    {
        [Header("跟随目标")]
        [SerializeField] private Transform _target;

        [Header("位置偏移")]
        [SerializeField] private Vector3 _offset = Vector3.zero;

        private void LateUpdate()
        {
            if (_target == null) return;

            // 只跟随位置，保持自己的旋转不变
            transform.position = _target.position + _offset;
        }

        /// <summary>
        /// 设置跟随目标（可在代码中动态调用）
        /// </summary>
        public void SetTarget(Transform target)
        {
            _target = target;
        }

        /// <summary>
        /// 设置位置偏移
        /// </summary>
        public void SetOffset(Vector3 offset)
        {
            _offset = offset;
        }
    }
}
