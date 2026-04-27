/// <summary>
/// 可被回溯系统驱动的对象接口。
/// </summary>
public interface IRewindable
{
    /// <summary>
    /// 设置对象回溯状态。
    /// </summary>
    /// <param name="state">新的回溯状态。</param>
    void SetRewindState(RewindState state);

    /// <summary>
    /// 清空对象回溯历史记录。
    /// </summary>
    void ClearHistory();
}
