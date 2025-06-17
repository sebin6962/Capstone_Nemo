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
    public Vector2 lastMoveDir; // �⺻�� �ո��

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
    (StorageInventoryUIManager.Instance != null && StorageInventoryUIManager.Instance.IsOpen())) // �� �߰�
        {
            movement = Vector2.zero;
            animator.SetBool("IsWalking", false);
            // ������ �̵� ������ ����ؼ� Idle ���� ����
            animator.SetFloat("MoveX", lastMoveDir.x);
            animator.SetFloat("MoveY", lastMoveDir.y);
            return;
        }

        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        // �����̰� �ִٸ� lastMoveDir ����
        if (movement != Vector2.zero)
        {
            lastMoveDir = movement;
            animator.SetBool("IsWalking", true);
            animator.SetFloat("MoveX", movement.x);
            animator.SetFloat("MoveY", movement.y);
        }
        else
        {
            // ������ �� ������ ������ Idle�� �ݿ�
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
