using System.Collections;
using UnityEngine;
using UnityEngine.U2D.Animation;

public class PlayerWateringResolverMotion : MonoBehaviour
{
    [Header("방향 감지 세부 설정")]
    [SerializeField] private Vector2 directionOriginOffset = new Vector2(0f, 0.35f);

    [SerializeField] private float horizontalThreshold = 0.35f;
    [SerializeField] private float verticalThreshold = 0.35f;

    [SerializeField] private float diagonalBias = 1.25f;

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
        Vector2 origin = (Vector2)transform.position + directionOriginOffset;
        Vector2 target = targetWorldPos;
        Vector2 diff = target - origin;

        Debug.Log($"[WateringMotion/Direction] origin={origin}, target={target}, diff={diff}");

        float absX = Mathf.Abs(diff.x);
        float absY = Mathf.Abs(diff.y);

        // 너무 가까운 클릭이면 현재 바라보는 방향 또는 아래 방향 사용
        if (absX < horizontalThreshold && absY < verticalThreshold)
        {
            Debug.Log("[WateringMotion/Direction] 클릭 위치가 기준점과 너무 가까워 Down 사용");
            return "Down";
        }

        // 좌우가 충분히 강할 때만 Left/Right
        if (absX > absY * diagonalBias && absX >= horizontalThreshold)
        {
            string dir = diff.x >= 0f ? "Right" : "Left";
            Debug.Log($"[WateringMotion/Direction] 좌우 방향 선택: {dir}");
            return dir;
        }

        // 상하가 충분히 강할 때만 Up/Down
        if (absY > absX * diagonalBias && absY >= verticalThreshold)
        {
            string dir = diff.y >= 0f ? "Up" : "Down";
            Debug.Log($"[WateringMotion/Direction] 상하 방향 선택: {dir}");
            return dir;
        }

        // 애매한 대각선 영역이면 y 우선
        // 농사 게임에서는 보통 위/아래 밭 판정이 더 자연스러움
        if (absY >= absX)
        {
            string dir = diff.y >= 0f ? "Up" : "Down";
            Debug.Log($"[WateringMotion/Direction] 애매한 대각선 → 상하 우선: {dir}");
            return dir;
        }
        else
        {
            string dir = diff.x >= 0f ? "Right" : "Left";
            Debug.Log($"[WateringMotion/Direction] 애매한 대각선 → 좌우 우선: {dir}");
            return dir;
        }
    }
}
