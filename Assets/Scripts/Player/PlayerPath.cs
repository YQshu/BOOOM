using UnityEngine;

public class PlayerPath2D : MonoBehaviour
{
    [Header("路径点")]
    public Transform[] pathPoints;

    [Header("移动速度")]
    public float moveSpeed = 3f;

    private int currentPoint = 0;

    void Update()
    {
        MoveAlongPath();
    }

    void MoveAlongPath()
    {
        if (currentPoint >= pathPoints.Length)
            return;

        // 2D 移动
        transform.position = Vector2.MoveTowards(
            transform.position,
            pathPoints[currentPoint].position,
            moveSpeed * Time.deltaTime
        );

        // 到达一个点，走下一个
        if (Vector2.Distance(transform.position, pathPoints[currentPoint].position) < 0.1f)
        {
            currentPoint++;
        }
    }

    // 外部调用：跳到第几个点（剧情触发用）
    public void GoToPoint(int index)
    {
        currentPoint = index;
    }

    // 从头开始走
    public void RestartPath()
    {
        currentPoint = 0;
    }
}
