using System.Collections;
using UnityEngine;
using UnityEngine.U2D.Animation;

public class PlayerWateringResolverMotion : MonoBehaviour
{
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
            Debug.LogWarning("[WateringMotion] SpriteResolver가 없습니다.");
            return;
        }

        if (motionCoroutine != null)
            StopCoroutine(motionCoroutine);

        motionCoroutine = StartCoroutine(PlayRoutine(targetWorldPos));
    }

    private IEnumerator PlayRoutine(Vector3 targetWorldPos)
    {
        string direction = GetDirectionName(targetWorldPos);

        // 기존 상태 저장
        prevCategory = spriteResolver.GetCategory();
        prevLabel = spriteResolver.GetLabel();

        // 이동 잠금
        if (playerManager != null)
            playerManager.SetActionLocked(true);

        // 머리 위 물뿌리개 UI 잠깐 숨김
        HeldItemManager.Instance?.SetHeldItemVisualVisible(false);

        // 기존 걷기 Animator가 SpriteResolver를 덮어쓰지 못하게 잠깐 정지
        if (playerAnimator != null)
            playerAnimator.enabled = false;

        for (int i = 0; i < frameCount; i++)
        {
            string label = $"Watering_{direction}_{i}";

            spriteResolver.SetCategoryAndLabel(categoryName, label);
            spriteResolver.ResolveSpriteToSpriteRenderer();

            yield return new WaitForSeconds(frameInterval);
        }

        // 원래 라벨 복구
        if (!string.IsNullOrEmpty(prevCategory) && !string.IsNullOrEmpty(prevLabel))
        {
            spriteResolver.SetCategoryAndLabel(prevCategory, prevLabel);
            spriteResolver.ResolveSpriteToSpriteRenderer();
        }

        if (playerAnimator != null)
            playerAnimator.enabled = true;

        HeldItemManager.Instance?.SetHeldItemVisualVisible(true);

        if (playerManager != null)
            playerManager.SetActionLocked(false);

        motionCoroutine = null;
    }

    private string GetDirectionName(Vector3 targetWorldPos)
    {
        Vector2 diff = targetWorldPos - transform.position;

        if (Mathf.Abs(diff.x) > Mathf.Abs(diff.y))
        {
            return diff.x >= 0f ? "Right" : "Left";
        }

        return diff.y >= 0f ? "Up" : "Down";
    }
}
