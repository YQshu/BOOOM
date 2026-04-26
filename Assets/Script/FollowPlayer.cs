using UnityEngine;

public class FlashlightFollowPlayer : MonoBehaviour
{
    // 拖入你的玩家
    public Transform player;

    void Update()
    {
        // 手电筒位置 = 玩家位置
        transform.position = player.position;

        // 手电筒旋转 = 玩家旋转（完全跟着玩家转）
        transform.rotation = player.rotation;
    }
}