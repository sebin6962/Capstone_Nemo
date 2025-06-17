using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerManager : MonoBehaviour
{
    public float moveSpeed = 10f;
    Rigidbody2D rb;
    public Animator animator;

    private Vector2 movement;
    public Vector2 lastMoveDir; // 기본은 앞모습

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if ((BoxInventoryManager.Instance != null && BoxInventoryManager.Instance.IsInventoryOpen()) ||
    (PopupInventoryUIManager.Instance != null && PopupInventoryUIManager.Instance.IsPopupOpen()) ||
    (PlayerStoreBoxInventoryUIManager.Instance != null && PlayerStoreBoxInventoryUIManager.Instance.IsOpen()) ||
    (DoGamUIManager.Instance != null && DoGamUIManager.Instance.IsOpen()) ||
    (StorageInventoryUIManager.Instance != null && StorageInventoryUIManager.Instance.IsOpen())) // ← 추가
        {
            movement = Vector2.zero;
            animator.SetBool("IsWalking", false);
            // 마지막 이동 방향을 사용해서 Idle 방향 고정
            animator.SetFloat("MoveX", lastMoveDir.x);
            animator.SetFloat("MoveY", lastMoveDir.y);
            return;
        }

        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        // 움직이고 있다면 lastMoveDir 갱신
        if (movement != Vector2.zero)
        {
            lastMoveDir = movement;
            animator.SetBool("IsWalking", true);
            animator.SetFloat("MoveX", movement.x);
            animator.SetFloat("MoveY", movement.y);
        }
        else
        {
            // 멈췄을 때 마지막 방향을 Idle에 반영
            animator.SetBool("IsWalking", false);
            animator.SetFloat("MoveX", lastMoveDir.x);
            animator.SetFloat("MoveY", lastMoveDir.y);
            Debug.Log("Manager Idle: lastMoveDir=" + lastMoveDir + ", Animator MoveY=" + animator.GetFloat("MoveY"));
        }
    }

    void FixedUpdate()
    {
        if ((BoxInventoryManager.Instance != null && BoxInventoryManager.Instance.IsInventoryOpen()) ||
    (PopupInventoryUIManager.Instance != null && PopupInventoryUIManager.Instance.IsPopupOpen())||
    (PlayerStoreBoxInventoryUIManager.Instance != null && PlayerStoreBoxInventoryUIManager.Instance.IsOpen()) ||
    (DoGamUIManager.Instance != null && DoGamUIManager.Instance.IsOpen()) ||
    (StorageInventoryUIManager.Instance != null && StorageInventoryUIManager.Instance.IsOpen()))
        {
            return;
        }

        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }

    
}
