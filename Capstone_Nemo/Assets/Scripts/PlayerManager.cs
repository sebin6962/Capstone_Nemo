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

    private enum AxisLock { None, Horizontal, Vertical }
    private AxisLock axisLock = AxisLock.None;

    private float prevHx = 0f;
    private float prevVy = 0f;

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
    { // 초기 바라보는 방향을 enum 기준으로 설정
        switch (initialFacing)
        {
            case InitialFacing.Up:
                lastMoveDir = Vector2.up;      // (0, 1)
                break;
            case InitialFacing.Down:
                lastMoveDir = Vector2.down;    // (0, -1)
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

        //팝업 활성화 시 플레이어 이동 잠금
        if ((BoxInventoryManager.Instance != null && BoxInventoryManager.Instance.IsInventoryOpen()) ||
    (PopupInventoryUIManager.Instance != null && PopupInventoryUIManager.Instance.IsPopupOpen()) ||
    (PlayerStoreBoxInventoryUIManager.Instance != null && PlayerStoreBoxInventoryUIManager.Instance.IsOpen()) ||
    (DoGamUIManager.Instance != null && DoGamUIManager.Instance.IsOpen()) ||
    (StorageInventoryUIManager.Instance != null && StorageInventoryUIManager.Instance.IsOpen()) ||
                (TreeInteract.Instance != null && TreeInteract.Instance.IsOpen()) ||
                (TreeLevelUnlocker.Instance != null && TreeLevelUnlocker.Instance.IsPlayingUnlockSequence))
        {
            movement = Vector2.zero;
            animator.SetBool("IsWalking", false);
            // 마지막 이동 방향을 사용해서 Idle 방향 고정
            animator.SetFloat("MoveX", lastMoveDir.x);
            animator.SetFloat("MoveY", lastMoveDir.y);

            if (SFXManager.Instance != null)
                SFXManager.Instance.StopPlayerWalkLoop();
            return;
        }

        //movement.x = Input.GetAxisRaw("Horizontal");
        //movement.y = Input.GetAxisRaw("Vertical");

        // 원시 입력
        float hx = Input.GetAxisRaw("Horizontal"); // -1,0,1
        float vy = Input.GetAxisRaw("Vertical");   // -1,0,1

        // --- 축 잠금 결정 ---
        if (axisLock == AxisLock.None)
        {
            if (hx != 0f && vy == 0f) axisLock = AxisLock.Horizontal;
            else if (vy != 0f && hx == 0f) axisLock = AxisLock.Vertical;
            else if (hx != 0f && vy != 0f)
            {
                // 둘 다 눌린 첫 프레임: 먼저 "변한" 축을 잠금
                bool hBecameNonZero = (prevHx == 0f && hx != 0f);
                bool vBecameNonZero = (prevVy == 0f && vy != 0f);

                if (hBecameNonZero && !vBecameNonZero) axisLock = AxisLock.Horizontal;
                else if (vBecameNonZero && !hBecameNonZero) axisLock = AxisLock.Vertical;
                else
                {
                    // 둘 다 동시에 눌린 경우: 키다운 우선순위로 판정
                    bool anyHDown = Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow) ||
                                    Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D);
                    bool anyVDown = Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow) ||
                                    Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.S);

                    if (anyHDown && !anyVDown) axisLock = AxisLock.Horizontal;
                    else if (anyVDown && !anyHDown) axisLock = AxisLock.Vertical;
                    // 여전히 애매하면 수평 우선 등 정책을 정해도 됨
                }
            }
        }
        else
        {
            // 잠금 유지 조건, 해제/전환
            if (axisLock == AxisLock.Horizontal)
            {
                if (hx == 0f)
                    axisLock = (vy != 0f) ? AxisLock.Vertical : AxisLock.None;
            }
            else if (axisLock == AxisLock.Vertical)
            {
                if (vy == 0f)
                    axisLock = (hx != 0f) ? AxisLock.Horizontal : AxisLock.None;
            }
        }

        // --- 잠금 축에 따라 실제 이동 벡터 결정 ---
        if (axisLock == AxisLock.Horizontal)
            movement = new Vector2(Mathf.Sign(hx), 0f);
        else if (axisLock == AxisLock.Vertical)
            movement = new Vector2(0f, Mathf.Sign(vy));
        else
            movement = Vector2.zero; // 둘 다 0 또는 아직 결정 안 됨

        // 움직이고 있다면 lastMoveDir 갱신
        if (movement != Vector2.zero)
        {
            lastMoveDir = movement;
            animator.SetBool("IsWalking", true);
            animator.SetFloat("MoveX", movement.x);
            animator.SetFloat("MoveY", movement.y);

            if (SFXManager.Instance != null)
                SFXManager.Instance.PlayPlayerWalkLoop();
        }
        else
        {
            // 멈췄을 때 마지막 방향을 Idle에 반영
            animator.SetBool("IsWalking", false);
            animator.SetFloat("MoveX", lastMoveDir.x);
            animator.SetFloat("MoveY", lastMoveDir.y);
            //Debug.Log("Manager Idle: lastMoveDir=" + lastMoveDir + ", Animator MoveY=" + animator.GetFloat("MoveY"));

            if (SFXManager.Instance != null)
                SFXManager.Instance.StopPlayerWalkLoop();
        }

        // 다음 프레임 판정을 위한 이전값 저장
        prevHx = hx;
        prevVy = vy;
    }

    void FixedUpdate()
    {
        if ((BoxInventoryManager.Instance != null && BoxInventoryManager.Instance.IsInventoryOpen()) ||
    (PopupInventoryUIManager.Instance != null && PopupInventoryUIManager.Instance.IsPopupOpen())||
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

        // 이동/축 잠금 초기화
        movement = Vector2.zero;
        axisLock = AxisLock.None;
        prevHx = 0f;
        prevVy = 0f;

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
