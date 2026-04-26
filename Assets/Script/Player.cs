using UnityEngine;

public class TopDownPlayerController : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 5f;
    [Header("旋转设置")]
    public float rotateSpeed = 200f; // 旋转速度

    Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        // WASD 移动
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");

        Vector2 moveDir = new Vector2(moveX, moveY).normalized;
        rb.velocity = moveDir * moveSpeed;
    }

    void Update()
    {
        // Q = 逆时针旋转
        if (Input.GetKey(KeyCode.Q))
        {
            transform.Rotate(0, 0, rotateSpeed * Time.deltaTime);
        }

        // E = 顺时针旋转
        if (Input.GetKey(KeyCode.E))
        {
            transform.Rotate(0, 0, -rotateSpeed * Time.deltaTime);
        }
    }
}