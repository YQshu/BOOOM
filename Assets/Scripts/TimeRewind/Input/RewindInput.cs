using UnityEngine;

/// <summary>
/// 按住回溯键实现慢速倒带
/// </summary>
public class RewindInput : MonoBehaviour
{
    [Header("按键设置")]
    [Tooltip("按住回溯键")]
    [SerializeField] private KeyCode _rewindKey = KeyCode.R;
    [Tooltip("播放键")]
    [SerializeField] private KeyCode _playKey = KeyCode.Space;
    [Tooltip("暂停键")]
    [SerializeField] private KeyCode _pauseKey = KeyCode.P;

    [Header("目标管理器")]
    [SerializeField] private TimelineRewindManager _rewindManager;

    private bool _isRewinding = false;

    private void Update()
    {
        if (_rewindManager == null) return;

        // 按住回溯键 → 开始回溯
        if (Input.GetKeyDown(_rewindKey))
        {
            _rewindManager.StartRewinding();
            _isRewinding = true;
        }

        // 松开回溯键 → 停止回溯（停留在当前位置）
        if (Input.GetKeyUp(_rewindKey))
        {
            _rewindManager.StopRewinding();
            _isRewinding = false;
        }

        // 播放（如果已经停止回溯且未播放）
        if (Input.GetKeyDown(_playKey) && !_isRewinding)
        {
            if (_rewindManager.IsPlaying())
            {
                _rewindManager.Pause();
            }
            else
            {
                _rewindManager.Play();
            }
        }

        // 暂停
        if (Input.GetKeyDown(_pauseKey))
        {
            _rewindManager.Pause();
        }
    }

    private void OnGUI()
    {
        if (_rewindManager == null) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 150));
        GUILayout.Label($"Timeline 状态: {(_rewindManager.IsPlaying() ? "播放中" : "暂停")}");
        GUILayout.Label($"回溯中: {(_rewindManager.IsRewinding() ? "是" : "否")}");
        GUILayout.Label($"当前进度: {_rewindManager.GetCurrentProgress():P}");
        GUILayout.Space(10);
        GUILayout.Label($"按住 {_rewindKey} 慢速回溯");
        GUILayout.Label($"松开 {_rewindKey} 停止在当前画面");
        GUILayout.EndArea();
    }
}
