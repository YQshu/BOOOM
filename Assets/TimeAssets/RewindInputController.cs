using UnityEngine;

/// <summary>
/// 按住回溯键实现慢速倒带
/// </summary>
public class RewindInput : MonoBehaviour
{
    [Header("按键设置")]
    [Tooltip("按住回溯键")]
    [SerializeField] private KeyCode _rewindKey = KeyCode.R;
    [Tooltip("播放/暂停键")]
    [SerializeField] private KeyCode _playPauseKey = KeyCode.Space;
    [Tooltip("停止键")]
    [SerializeField] private KeyCode _stopKey = KeyCode.Escape;
    [Tooltip("重置键")]
    [SerializeField] private KeyCode _resetKey = KeyCode.Home;

    [Header("目标管理器")]
    [SerializeField] private TimelineRewindManager _rewindManager;

    private bool _isRewinding = false;

    private void Update()
    {
        if (_rewindManager == null) return;

        // 检查是否播放完毕
        bool isCompleted = _rewindManager.IsCompleted();

        // 按住回溯键 → 开始/持续回溯（仅当未播放完毕时）
        if (Input.GetKeyDown(_rewindKey) && !isCompleted)
        {
            _rewindManager.StartRewinding();
            _isRewinding = true;
        }

        // 松开回溯键 → 停止回溯
        if (Input.GetKeyUp(_rewindKey))
        {
            _rewindManager.StopRewinding();
            _isRewinding = false;
        }

        // 播放/暂停（回溯时不响应）
        if (Input.GetKeyDown(_playPauseKey) && !_isRewinding)
        {
            if (isCompleted)
            {
                // 如果播放完毕，重新播放
                _rewindManager.Play();
            }
            else if (_rewindManager.IsPlaying())
            {
                _rewindManager.Pause();
            }
            else
            {
                _rewindManager.Play();
            }
        }

        // 停止
        if (Input.GetKeyDown(_stopKey))
        {
            _rewindManager.Stop();
        }

        // 重置到起点
        if (Input.GetKeyDown(_resetKey))
        {
            _rewindManager.Stop();
            _rewindManager.Play();
        }
    }

    private void OnGUI()
    {
        if (_rewindManager == null) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label($"Timeline 状态: {(_rewindManager.IsPlaying() ? "播放中" : "暂停")}");
        GUILayout.Label($"回溯中: {(_rewindManager.IsRewinding() ? "是" : "否")}");
        GUILayout.Label($"当前进度: {_rewindManager.GetCurrentProgress():P}");
        GUILayout.Label($"是否完成: {(_rewindManager.IsCompleted() ? "是" : "否")}");
        GUILayout.Space(10);
        GUILayout.Label($"操作说明:");
        GUILayout.Label($"  按住 {_rewindKey} - 慢速回溯");
        GUILayout.Label($"  空格 - 播放/暂停");
        GUILayout.Label($"  {_stopKey} - 停止");
        GUILayout.Label($"  {_resetKey} - 重置播放");
        GUILayout.EndArea();
    }
}
