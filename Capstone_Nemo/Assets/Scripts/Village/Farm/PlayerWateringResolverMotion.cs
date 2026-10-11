
using System.Collections;
using UnityEngine;
using UnityEngine.U2D.Animation;

public class PlayerWateringResolverMotion : MonoBehaviour
{
    [Header("방향 판정")]
    [SerializeField]
    private Vector2 directionOriginOffset =
        new Vector2(0f, 0.35f);

    [SerializeField] private float horizontalThreshold = 0.5f;

    [Header("기존 플레이어 Animator")]
    [SerializeField] private Animator playerAnimator;

    [Header("기존 플레이어 SpriteResolver")]
    [SerializeField] private SpriteResolver spriteResolver;

    [Header("Sprite Library 카테고리명")]
    [SerializeField] private string categoryName = "Player";

    [Header("물주기 프레임 수")]
    [SerializeField] private int frameCount = 4;

    [Header("프레임 간격")]
    [SerializeField] private float frameInterval = 0.1f;

    private PlayerManager playerManager;
    private Coroutine motionCoroutine;

    private string prevCategory;
    private string prevLabel;

    private bool animatorWasEnabled;

    private void Awake()
    {
        playerManager = GetComponent<PlayerManager>();

        if (playerAnimator == null)
            playerAnimator = GetComponent<Animator>();

        if (spriteResolver == null)
            spriteResolver = GetComponentInChildren<SpriteResolver>();
    }

    public void Play(Vector3 targetWorldPos)
    {
        if (spriteResolver == null)
        {
            Debug.LogWarning(
                "[WateringMotion] SpriteResolver가 없습니다."
            );
            return;
        }

        if (motionCoroutine != null)
            StopCoroutine(motionCoroutine);

        motionCoroutine = StartCoroutine(
            PlayRoutine(targetWorldPos)
        );
    }

    private IEnumerator PlayRoutine(Vector3 targetWorldPos)
    {
        string direction = GetDirectionName(targetWorldPos);

        if (playerManager != null)
        {
            playerManager.SetActionFacingDirection(
                DirectionToVector(direction)
            );
        }

        // 기존 상태 저장
        prevCategory = spriteResolver.GetCategory();
        prevLabel = spriteResolver.GetLabel();

        // 이동 잠금
        if (playerManager != null)
            playerManager.SetActionLocked(true);

        // 물뿌리개 UI 숨김
        HeldItemManager.Instance?.SetHeldItemVisualVisible(false);

        // Animator 상태 저장 후 정지
        if (playerAnimator != null)
        {
            animatorWasEnabled = playerAnimator.enabled;
            playerAnimator.enabled = false;
        }

        for (int i = 0; i < frameCount; i++)
        {
            string label = $"Watering_{direction}_{i}";

            spriteResolver.SetCategoryAndLabel(
                categoryName,
                label
            );

            spriteResolver.ResolveSpriteToSpriteRenderer();

            yield return new WaitForSeconds(frameInterval);
        }

        // 기존 SpriteResolver 라벨 복구
        if (!string.IsNullOrEmpty(prevCategory) &&
            !string.IsNullOrEmpty(prevLabel))
        {
            spriteResolver.SetCategoryAndLabel(
                prevCategory,
                prevLabel
            );

            spriteResolver.ResolveSpriteToSpriteRenderer();
        }

        // Animator 상태 복구
        if (playerAnimator != null)
            playerAnimator.enabled = animatorWasEnabled;

        HeldItemManager.Instance?.SetHeldItemVisualVisible(true);

        if (playerManager != null)
            playerManager.SetActionLocked(false);

        motionCoroutine = null;
    }

    private Vector2 DirectionToVector(string direction)
    {
        switch (direction)
        {
            case "Up":
                return Vector2.up;

            case "Down":
                return Vector2.down;

            case "Left":
                return Vector2.left;

            case "Right":
                return Vector2.right;
        }

        return Vector2.down;
    }


    private string GetDirectionName(Vector3 targetWorldPos)
    {
        Vector2 origin =
            (Vector2)transform.position + directionOriginOffset;

        Vector2 diff = (Vector2)targetWorldPos - origin;

        float absX = Mathf.Abs(diff.x);
        float absY = Mathf.Abs(diff.y);

        // 좌우 방향에 약간의 가중치를 적용
        // 1.2 = 좌우 판정이 조금 더 쉽게 나오도록 설정
        float horizontalBias = 1.2f;

        if (absX * horizontalBias >= absY)
        {
            return diff.x >= 0f ? "Right" : "Left";
        }

        return diff.y >= 0f ? "Up" : "Down";
    }

}
