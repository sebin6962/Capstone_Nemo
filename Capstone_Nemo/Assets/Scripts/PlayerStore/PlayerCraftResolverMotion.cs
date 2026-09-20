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

    private bool isPlaying;

    // 제작 시작 전 Animator 활성 상태
    private bool animatorWasEnabled;

    // 이번 제작에서 바라보는 방향
    private Vector2 craftFacingDirection = Vector2.down;

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

        // 이전 제작 모션이 아직 실행 중이라면
        // 완전히 복구한 뒤 새 모션 시작
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

        string direction = GetCraftDirection(maker);
        craftFacingDirection = DirectionToVector(direction);

        /*
         * 중요:
         * Animator를 끄기 전에 현재 활성 상태를 반드시 저장한다.
         *
         * 기존 코드에서는 animatorWasEnabled에 값을 넣지 않아
         * 기본값 false 상태였고,
         * 제작 종료 후 Animator가 계속 비활성화되는 문제가 있었다.
         */
        if (playerAnimator != null)
            animatorWasEnabled = playerAnimator.enabled;

        /*
         * 제작 방향으로 플레이어 방향 변경.
         *
         * PlayerManager의 lastMoveDir도 이 방향으로 맞춰지기 때문에
         * 제작 종료 후에도 동일한 방향으로 Idle 상태가 된다.
         */
        if (playerManager != null)
        {
            playerManager.SetActionFacingDirection(
                craftFacingDirection
            );

            // 제작 중 이동 완전 잠금
            playerManager.SetActionLocked(true);
        }

        // 머리 위에 들고 있는 아이템 잠시 숨기기
        HeldItemManager.Instance?.SetHeldItemVisualVisible(false);

        /*
         * 일반 걷기 Animator가 제작 SpriteResolver를
         * 덮어쓰지 못하도록 잠시 정지
         */
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

        /*
         * SpriteResolver의 제작 전 라벨을 직접 복구하지 않는다.
         *
         * 제작 전 라벨을 복구하면
         * PlayerManager의 lastMoveDir(제작 방향)과
         * SpriteResolver 방향이 서로 달라져
         * 제작 종료 순간 방향이 바뀌어 보일 수 있다.
         */

        // Animator 원래 상태 복구
        if (playerAnimator != null)
        {
            playerAnimator.enabled = animatorWasEnabled;
        }

        /*
         * Animator를 다시 켠 다음
         * 제작했던 방향을 확실하게 적용한다.
         */
        if (playerManager != null)
        {
            playerManager.SetActionFacingDirection(
                craftFacingDirection
            );
        }

        /*
         * Animator를 즉시 한 번 평가해서
         * Crafting Sprite가 한 프레임 남아 있지 않게 한다.
         */
        if (playerAnimator != null &&
            playerAnimator.enabled)
        {
            playerAnimator.Update(0f);
        }

        HeldItemManager.Instance?.SetHeldItemVisualVisible(true);

        // 모든 복구가 끝난 다음 이동 잠금 해제
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