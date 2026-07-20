using System.Collections;
using UnityEngine;
using UnityEngine.U2D.Animation;

public class PlayerCraftResolverMotion : MonoBehaviour
{
    [Header("기존 플레이어 Animator")]
    [SerializeField] private Animator playerAnimator;

    [Header("플레이어 SpriteResolver")]
    [SerializeField] private SpriteResolver spriteResolver;

    [Header("Sprite Library 카테고리")]
    [SerializeField] private string categoryName = "Player";

    [Header("제작 모션 프레임")]
    [SerializeField] private int frameCount = 4;

    [SerializeField] private float frameInterval = 0.1f;

    private PlayerManager playerManager;
    private Coroutine motionCoroutine;

    private string previousCategory;
    private string previousLabel;

    private bool isPlaying;

    private void Awake()
    {
        playerManager = GetComponent<PlayerManager>();

        if (playerAnimator == null)
            playerAnimator = GetComponent<Animator>();

        if (spriteResolver == null)
            spriteResolver = GetComponentInChildren<SpriteResolver>();
    }

    public void Play(MakerInfo maker)
    {
        if (maker == null)
            return;

        if (spriteResolver == null)
        {
            Debug.LogWarning(
                "[CraftMotion] SpriteResolver가 연결되지 않았습니다."
            );
            return;
        }

        // 이전 제작 모션이 아직 재생 중이면 정상 복구 후 다시 시작
        if (motionCoroutine != null)
        {
            StopCoroutine(motionCoroutine);
            motionCoroutine = null;
            RestorePlayerState();
        }

        motionCoroutine = StartCoroutine(PlayRoutine(maker));
    }

    private IEnumerator PlayRoutine(MakerInfo maker)
    {
        isPlaying = true;

        previousCategory = spriteResolver.GetCategory();
        previousLabel = spriteResolver.GetLabel();

        string direction = GetCraftDirection(maker);

        // 제작하는 동안 이동 정지
        if (playerManager != null)
            playerManager.SetActionLocked(true);

        // 머리 위에 들고 있는 아이템 잠시 숨기기
        HeldItemManager.Instance?.SetHeldItemVisualVisible(false);

        // 걷기 Animator가 제작 스프라이트를 덮어쓰지 않도록 정지
        if (playerAnimator != null)
            playerAnimator.enabled = false;

        for (int i = 0; i < frameCount; i++)
        {
            string label = $"Crafting_{direction}_{i}";

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

    private string GetCraftDirection(MakerInfo maker)
    {
        if (maker.craftMotionType == CraftMotionType.Up)
            return "Up";

        if (maker.craftMotionType == CraftMotionType.Down)
            return "Down";

        // Side는 제작대 위치에 따라 좌우 결정
        float differenceX =
            maker.transform.position.x - transform.position.x;

        return differenceX >= 0f ? "Right" : "Left";
    }

    private void RestorePlayerState()
    {
        if (!isPlaying)
            return;

        if (!string.IsNullOrEmpty(previousCategory) &&
            !string.IsNullOrEmpty(previousLabel))
        {
            spriteResolver.SetCategoryAndLabel(
                previousCategory,
                previousLabel
            );

            spriteResolver.ResolveSpriteToSpriteRenderer();
        }

        if (playerAnimator != null)
            playerAnimator.enabled = true;

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
