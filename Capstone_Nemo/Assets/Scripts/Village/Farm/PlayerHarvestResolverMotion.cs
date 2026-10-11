
using System.Collections;
using UnityEngine;
using UnityEngine.U2D.Animation;

// 기존 제작 스프라이트를 재사용하는 수확 전용 모션
public class PlayerHarvestResolverMotion : MonoBehaviour
{
    [Header("플레이어 Animator")]
    [SerializeField] private Animator playerAnimator;

    [Header("플레이어 SpriteResolver")]
    [SerializeField] private SpriteResolver spriteResolver;

    [Header("Sprite Library")]
    [SerializeField] private string categoryName = "Player";
    [SerializeField] private string motionPrefix = "Crafting";

    [Header("수확 모션")]
    [Min(1)]
    [SerializeField] private int frameCount = 4;

    [Min(0.01f)]
    [SerializeField] private float frameInterval = 0.1f;

    [Header("수확 방향 판정")]
    [SerializeField] private float horizontalThreshold = 0.5f;

    private PlayerManager playerManager;
    private Coroutine motionCoroutine;

    private bool isPlaying;
    private bool animatorWasEnabled;

    private Vector2 facingDirection = Vector2.down;

    private void Awake()
    {
        playerManager = GetComponent<PlayerManager>();

        if (playerManager == null)
            playerManager = GetComponentInParent<PlayerManager>();

        if (playerAnimator == null)
            playerAnimator = GetComponent<Animator>();

        if (playerAnimator == null)
            playerAnimator = GetComponentInChildren<Animator>(true);

        if (spriteResolver == null)
            spriteResolver = GetComponentInChildren<SpriteResolver>(true);
    }

    public void Play(Vector3 targetWorldPosition)
    {
        if (!isActiveAndEnabled)
        {
            Debug.LogWarning(
                "[HarvestMotion] 컴포넌트가 비활성화되어 있습니다."
            );
            return;
        }

        if (spriteResolver == null)
        {
            Debug.LogWarning(
                "[HarvestMotion] SpriteResolver가 연결되지 않았습니다."
            );
            return;
        }

        if (motionCoroutine != null)
        {
            StopCoroutine(motionCoroutine);
            motionCoroutine = null;
            RestorePlayerState();
        }


        Vector3 playerPosition =
            playerManager != null
                ? playerManager.transform.position
                : transform.position;

        Vector2 delta = targetWorldPosition - playerPosition;

        string direction;

        // X축 차이가 일정 이상이면 좌우 방향 우선
        if (Mathf.Abs(delta.x) >= horizontalThreshold)
        {
            direction = delta.x >= 0f ? "Right" : "Left";
        }
        else
        {
            direction = delta.y >= 0f ? "Up" : "Down";
        }


        switch (direction)
        {
            case "Up":
                facingDirection = Vector2.up;
                break;

            case "Down":
                facingDirection = Vector2.down;
                break;

            case "Left":
                facingDirection = Vector2.left;
                break;

            case "Right":
                facingDirection = Vector2.right;
                break;
        }

        motionCoroutine = StartCoroutine(PlayRoutine(direction));
    }

    private IEnumerator PlayRoutine(string direction)
    {
        isPlaying = true;

        animatorWasEnabled =
            playerAnimator != null && playerAnimator.enabled;

        if (playerManager != null)
        {
            playerManager.SetActionFacingDirection(facingDirection);
            playerManager.SetActionLocked(true);
        }

        HeldItemManager.Instance?.SetHeldItemVisualVisible(false);

        if (playerAnimator != null)
            playerAnimator.enabled = false;

        Debug.Log(
            $"[HarvestMotion] 수확 모션 시작: {direction}"
        );

        for (int i = 0; i < frameCount; i++)
        {
            string label = $"{motionPrefix}_{direction}_{i}";

            spriteResolver.SetCategoryAndLabel(
                categoryName,
                label
            );

            spriteResolver.ResolveSpriteToSpriteRenderer();

            yield return new WaitForSeconds(frameInterval);
        }

        RestorePlayerState();
        motionCoroutine = null;
    }

    private void RestorePlayerState()
    {
        if (!isPlaying)
            return;

        if (playerAnimator != null)
            playerAnimator.enabled = animatorWasEnabled;

        if (playerManager != null)
            playerManager.SetActionFacingDirection(facingDirection);

        if (playerAnimator != null && playerAnimator.enabled)
            playerAnimator.Update(0f);

        HeldItemManager.Instance?.SetHeldItemVisualVisible(true);

        if (playerManager != null)
            playerManager.SetActionLocked(false);

        isPlaying = false;
    }

    private void OnDisable()
    {
        if (motionCoroutine != null)
        {
            StopCoroutine(motionCoroutine);
            motionCoroutine = null;
        }

        RestorePlayerState();
    }
}
