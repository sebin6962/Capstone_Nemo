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
    private Vector2 lastMoveDir; // 기본은 앞모습

    // 하루가 끝난 상태인지
    private bool isDayEnding = false;

    public enum InitialFacing { Up, Down, Left, Right }
    [SerializeField] private InitialFacing initialFacing = InitialFacing.Down;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        // 초기 바라보는 방향을 enum 기준으로 설정
        switch (initialFacing)
        {
            case InitialFacing.Up:
                lastMoveDir = Vector2.up;
                break;
            case InitialFacing.Down:
                lastMoveDir = Vector2.down;
                break;
            case InitialFacing.Left:
                lastMoveDir = Vector2.left;
                break;
            case InitialFacing.Right:
                lastMoveDir = Vector2.right;
                break;
        }

        if (animator != null)
        {
            animator.SetBool("IsWalking", false);
            animator.SetFloat("MoveX", lastMoveDir.x);
            animator.SetFloat("MoveY", lastMoveDir.y);
        }
    }

    void Update()
    {
        // 하루가 끝난 상태라면, 계속 걷기 막고 소리도 끔
        if (isDayEnding)
        {
            movement = Vector2.zero;

            if (animator != null)
            {
                animator.SetBool("IsWalking", false);
                animator.SetFloat("MoveX", lastMoveDir.x);
                animator.SetFloat("MoveY", lastMoveDir.y);
            }

            if (SFXManager.Instance != null)
                SFXManager.Instance.StopPlayerWalkLoop();

            return;
        }

        // 팝업 활성화 시 플레이어 이동 잠금
        if ((BoxInventoryManager.Instance != null && BoxInventoryManager.Instance.IsInventoryOpen()) ||
            (PopupInventoryUIManager.Instance != null && PopupInventoryUIManager.Instance.IsPopupOpen()) ||
            (PlayerStoreBoxInventoryUIManager.Instance != null && PlayerStoreBoxInventoryUIManager.Instance.IsOpen()) ||
            (DoGamUIManager.Instance != null && DoGamUIManager.Instance.IsOpen()) ||
            (StorageInventoryUIManager.Instance != null && StorageInventoryUIManager.Instance.IsOpen()) ||
            (StatueColorChangeUIManager.Instance != null && StatueColorChangeUIManager.Instance.IsOpen()) ||
            (TreeInteract.Instance != null && TreeInteract.Instance.IsOpen()) ||
            (TreeLevelUnlocker.Instance != null && TreeLevelUnlocker.Instance.IsPlayingUnlockSequence))
        {
            movement = Vector2.zero;
            animator.SetBool("IsWalking", false);
            animator.SetFloat("MoveX", lastMoveDir.x);
            animator.SetFloat("MoveY", lastMoveDir.y);

            if (SFXManager.Instance != null)
                SFXManager.Instance.StopPlayerWalkLoop();
            return;
        }

        // 원시 입력
        float hx = Input.GetAxisRaw("Horizontal"); // -1,0,1
        float vy = Input.GetAxisRaw("Vertical");   // -1,0,1

        // 대각선 이동 가능
        movement = new Vector2(hx, vy).normalized;

        if (movement != Vector2.zero)
        {
            animator.SetBool("IsWalking", true);

            // 대각선 포함: 좌우 입력이 있으면 좌/우 애니메이션 우선
            if (hx > 0f)
            {
                lastMoveDir = Vector2.right;
                animator.SetFloat("MoveX", 1f);
                animator.SetFloat("MoveY", 0f);
            }
            else if (hx < 0f)
            {
                lastMoveDir = Vector2.left;
                animator.SetFloat("MoveX", -1f);
                animator.SetFloat("MoveY", 0f);
            }
            else if (vy > 0f)
            {
                lastMoveDir = Vector2.up;
                animator.SetFloat("MoveX", 0f);
                animator.SetFloat("MoveY", 1f);
            }
            else if (vy < 0f)
            {
                lastMoveDir = Vector2.down;
                animator.SetFloat("MoveX", 0f);
                animator.SetFloat("MoveY", -1f);
            }

            if (SFXManager.Instance != null)
                SFXManager.Instance.PlayPlayerWalkLoop();
        }
        else
        {
            animator.SetBool("IsWalking", false);
            animator.SetFloat("MoveX", lastMoveDir.x);
            animator.SetFloat("MoveY", lastMoveDir.y);

            if (SFXManager.Instance != null)
                SFXManager.Instance.StopPlayerWalkLoop();
        }
    }

    void FixedUpdate()
    {
        if (((SkinShopUIManager.Instance != null && SkinShopUIManager.Instance.IsOpen()) || BoxInventoryManager.Instance != null && BoxInventoryManager.Instance.IsInventoryOpen()) ||
            (PopupInventoryUIManager.Instance != null && PopupInventoryUIManager.Instance.IsPopupOpen()) ||
            (PlayerStoreBoxInventoryUIManager.Instance != null && PlayerStoreBoxInventoryUIManager.Instance.IsOpen()) ||
            (DoGamUIManager.Instance != null && DoGamUIManager.Instance.IsOpen()) ||
            (StorageInventoryUIManager.Instance != null && StorageInventoryUIManager.Instance.IsOpen()) ||
            (TreeLevelUnlocker.Instance != null && TreeLevelUnlocker.Instance.IsPlayingUnlockSequence))
        {
            return;
        }

        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }

    void OnEnable()
    {
        TimeManager.OnNewDayStarted += HandleNewDayStarted;
    }

    void OnDisable()
    {
        TimeManager.OnNewDayStarted -= HandleNewDayStarted;
    }

    private void HandleNewDayStarted()
    {
        // 하루가 끝났다고 표시
        isDayEnding = true;

        // 이동 초기화
        movement = Vector2.zero;

        // 애니메이션을 Idle로
        if (animator != null)
        {
            animator.SetBool("IsWalking", false);
            animator.SetFloat("MoveX", lastMoveDir.x);
            animator.SetFloat("MoveY", lastMoveDir.y);
        }

        // 걷는 소리 정지
        if (SFXManager.Instance != null)
            SFXManager.Instance.StopPlayerWalkLoop();
    }
}
