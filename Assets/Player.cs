using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public static Player Instance;
    [Header("移动设置")]
    public float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Vector2 moveInput;

    void Awake()
    {
        // 单例模式
        if (Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    void Start()
    {
        // 获取或添加 Rigidbody2D 组件
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            // 如果没有 Rigidbody2D，自动添加一个
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f; // 俯视角不需要重力
        }

        // 设置 Rigidbody2D 属性
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    void Update()
    {
        // 获取输入
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");

        moveInput = new Vector2(moveX, moveY).normalized;
    }

    void FixedUpdate()
    {
        // 在 FixedUpdate 中移动物理对象
        if (rb != null)
        {
            Vector2 newPosition = rb.position + moveInput * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(newPosition);
        }
    }
}
