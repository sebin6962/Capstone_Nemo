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
            StartHoldingMotion();

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

        if (playerAnimator != null)
        {
            playerAnimator.enabled = true;

            playerAnimator.SetBool(
                "IsWalking",
                playerManager.IsMoving
            );

            Vector2 direction =
                playerManager.LastMoveDirection;

            playerAnimator.SetFloat("MoveX", direction.x);
            playerAnimator.SetFloat("MoveY", direction.y);
            playerAnimator.Update(0f);
        }
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
