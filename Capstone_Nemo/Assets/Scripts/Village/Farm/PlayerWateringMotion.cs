using System.Collections;
using UnityEngine;

public class PlayerWateringMotion : MonoBehaviour
{
    [Header("기존 플레이어 시각 오브젝트")]
    [SerializeField] private GameObject normalVisualRoot;

    [Header("물주기 전용 시각 오브젝트")]
    [SerializeField] private GameObject wateringVisualRoot;
    [SerializeField] private Animator wateringAnimator;

    [Header("물주기 애니메이션 길이")]
    [SerializeField] private float motionDuration = 0.45f;

    [Header("물주는 동안 이동 잠금")]
    [SerializeField] private bool lockMovementDuringMotion = true;

    private PlayerManager playerManager;
    private Coroutine wateringCoroutine;

    private void Awake()
    {
        playerManager = GetComponent<PlayerManager>();

        if (wateringVisualRoot != null)
            wateringVisualRoot.SetActive(false);
    }

    public void Play(Vector3 targetWorldPos)
    {
        if (wateringVisualRoot == null || wateringAnimator == null)
            return;

        if (wateringCoroutine != null)
            StopCoroutine(wateringCoroutine);

        wateringCoroutine = StartCoroutine(PlayRoutine(targetWorldPos));
    }

    private IEnumerator PlayRoutine(Vector3 targetWorldPos)
    {
        string stateName = GetWateringStateName(targetWorldPos);

        if (lockMovementDuringMotion && playerManager != null)
            playerManager.SetActionLocked(true);

        // 머리 위에 들고 있는 물뿌리개 UI가 있으면 잠깐 숨김
        HeldItemManager.Instance?.SetHeldItemVisualVisible(false);

        // 기존 플레이어 몸을 숨기고, 물주기 전용 몸만 보여줌
        // normalVisualRoot는 Player 전체가 아니라 Sprite Library / Sprite Resolver가 있는 자식 오브젝트여야 함
        if (normalVisualRoot != null)
            normalVisualRoot.SetActive(false);

        wateringVisualRoot.SetActive(true);

        wateringAnimator.Play(stateName, 0, 0f);

        yield return new WaitForSeconds(motionDuration);

        wateringVisualRoot.SetActive(false);

        if (normalVisualRoot != null)
            normalVisualRoot.SetActive(true);

        HeldItemManager.Instance?.SetHeldItemVisualVisible(true);

        if (lockMovementDuringMotion && playerManager != null)
            playerManager.SetActionLocked(false);

        wateringCoroutine = null;
    }

    private string GetWateringStateName(Vector3 targetWorldPos)
    {
        Vector2 diff = targetWorldPos - transform.position;

        if (Mathf.Abs(diff.x) > Mathf.Abs(diff.y))
        {
            return diff.x >= 0f ? "Water_Right" : "Water_Left";
        }

        return diff.y >= 0f ? "Water_Up" : "Water_Down";
    }
}
