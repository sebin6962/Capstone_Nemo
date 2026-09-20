using UnityEngine;
using UnityEngine.U2D.Animation;

[DefaultExecutionOrder(100)]
public class PlayerHoldingResolverMotion : MonoBehaviour
{
    [Header("Player Components")]
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private SpriteResolver spriteResolver;

    [Header("Sprite Library")]
    [SerializeField] private string categoryName = "Body";

    [Header("Holding Walk")]
    [Min(1)]
    [SerializeField] private int walkFrameCount = 4;

    [Min(0.01f)]
    [SerializeField] private float walkFrameInterval = 0.12f;

    private bool isControlling;
    private bool previousHolding;
    private bool previousMoving;
    private string previousDirection = "";

    private int currentWalkFrame;
    private float walkTimer;

    private void Awake()
    {
        if (playerManager == null)
            playerManager = GetComponent<PlayerManager>();

        if (playerAnimator == null)
            playerAnimator = GetComponent<Animator>();

        if (spriteResolver == null)
            spriteResolver = GetComponentInChildren<SpriteResolver>();
    }

    private void LateUpdate()
    {
        if (playerManager == null || spriteResolver == null)
            return;

        bool isHolding =
            HeldItemManager.Instance != null &&
            HeldItemManager.Instance.IsHoldingItem();

        // 물주기/제작 모션이 SpriteResolver를 사용 중일 때는 개입하지 않는다.
        if (playerManager.IsActionLocked)
        {
            previousHolding = isHolding;

            // 액션 종료 후 현재 방향/상태를
            // 반드시 한 번 다시 적용하도록 캐시 초기화
            previousDirection = "";
            previousMoving = false;

            return;
        }

        if (!isHolding)
        {
            if (isControlling)
                StopHoldingMotion();

            previousHolding = false;
            return;
        }

        string direction = GetDirectionName(
            playerManager.LastMoveDirection
        );

        bool isMoving = playerManager.IsMoving;

        if (!isControlling)
        {
            StartHoldingMotion();
        }
        else if (playerAnimator != null && playerAnimator.enabled)
        {
            // 다른 액션에서 Animator를 켜더라도
            // 아이템을 들고 있으면 Holding이 다시 제어권을 가져온다.
            playerAnimator.enabled = false;
        }

        bool justStartedHolding = !previousHolding;
        bool movementStateChanged = isMoving != previousMoving;
        bool directionChanged = direction != previousDirection;

        /*
         * 아이템을 든 순간과 걷기→정지 순간에는 반드시 강제 적용한다.
         * 이전 라벨 캐시 때문에 기존/걷기 프레임이 남는 현상을 막는다.
         */
        if (justStartedHolding || movementStateChanged || directionChanged)
        {
            previousDirection = direction;
            previousMoving = isMoving;

            currentWalkFrame = 0;
            walkTimer = 0f;

            if (isMoving)
                ApplyWalkFrame(direction, 0);
            else
                ApplyIdleFrame(direction);
        }
        else if (isMoving)
        {
            UpdateWalkAnimation(direction);
        }

        previousHolding = true;
    }

    private void StartHoldingMotion()
    {
        isControlling = true;

        previousDirection = "";
        previousMoving = false;
        currentWalkFrame = 0;
        walkTimer = 0f;

        // 기존 Animator가 Resolver 라벨을 덮어쓰지 않도록 정지
        if (playerAnimator != null)
            playerAnimator.enabled = false;
    }

    private void StopHoldingMotion()
    {
        isControlling = false;

        previousDirection = "";
        previousMoving = false;
        currentWalkFrame = 0;
        walkTimer = 0f;

        if (playerAnimator == null || playerManager == null)
            return;

        // 현재 플레이어가 실제로 바라보던 방향을 그대로 사용
        Vector2 direction = playerManager.LastMoveDirection;
        bool isMoving = playerManager.IsMoving;

        // Animator 다시 활성화
        playerAnimator.enabled = true;

        // 현재 방향을 Animator 파라미터에 정확하게 반영
        playerAnimator.SetFloat("MoveX", direction.x);
        playerAnimator.SetFloat("MoveY", direction.y);
        playerAnimator.SetBool("IsWalking", isMoving);

        /*
         * 아이템을 내려놓은 순간 정지 상태라면
         * 이전 Animator State가 남아있지 않도록
         * 현재 방향의 Idle State를 직접 재생한다.
         *
         * PlayerManager에서 사용하는 Idle State 이름과 동일.
         */
        if (!isMoving)
        {
            string idleStateName = GetIdleStateName(direction);

            if (!string.IsNullOrEmpty(idleStateName))
            {
                int stateHash =
                    Animator.StringToHash("Base Layer." + idleStateName);

                if (playerAnimator.HasState(0, stateHash))
                {
                    playerAnimator.Play(
                        "Base Layer." + idleStateName,
                        0,
                        0f
                    );
                }
            }
        }

        // 즉시 반영
        playerAnimator.Update(0f);
    }

    private string GetIdleStateName(Vector2 direction)
    {
        if (direction.sqrMagnitude <= 0.001f)
            return "Idle_Front";

        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            return direction.x >= 0f
                ? "Idle_Right"
                : "Idle_Left";
        }

        return direction.y >= 0f
            ? "Idle_Back"
            : "Idle_Front";
    }

    private void UpdateWalkAnimation(string direction)
    {
        walkTimer += Time.deltaTime;

        float interval =
            Mathf.Max(0.01f, walkFrameInterval);

        while (walkTimer >= interval)
        {
            walkTimer -= interval;

            currentWalkFrame =
                (currentWalkFrame + 1) %
                Mathf.Max(1, walkFrameCount);

            ApplyWalkFrame(direction, currentWalkFrame);
        }
    }

    private void ApplyIdleFrame(string direction)
    {
        ApplyLabel($"Hold_Idle_{direction}");
    }

    private void ApplyWalkFrame(
        string direction,
        int frame
    )
    {
        ApplyLabel(
            $"Hold_Walk_{direction}_{frame}"
        );
    }

    private void ApplyLabel(string label)
    {
        spriteResolver.SetCategoryAndLabel(
            categoryName,
            label
        );

        spriteResolver.ResolveSpriteToSpriteRenderer();
    }

    private string GetDirectionName(Vector2 direction)
    {
        if (direction.sqrMagnitude <= 0.001f)
            return "Front";

        if (Mathf.Abs(direction.x) >
            Mathf.Abs(direction.y))
        {
            return direction.x >= 0f
                ? "Right"
                : "Left";
        }

        return direction.y >= 0f
            ? "Back"
            : "Front";
    }

    private void OnDisable()
    {
        if (isControlling && playerAnimator != null)
            playerAnimator.enabled = true;

        isControlling = false;
        previousHolding = false;
    }
}
