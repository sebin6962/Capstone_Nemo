using System.Collections;
using UnityEngine;

public class CraftSpriteLoopAnimator : MonoBehaviour
{
    [Header("연결 대상")]
    [SerializeField] private MakerInfo makerInfo;
    [SerializeField] private SpriteRenderer targetRenderer;

    [Header("기본 스프라이트")]
    [SerializeField] private Sprite idleSprite;

    [Header("제작 중 루프 스프라이트")]
    [SerializeField] private Sprite[] craftingFrames;

    [Header("재생 설정")]
    [SerializeField] private float frameInterval = 0.15f;
    [SerializeField] private bool resetToIdleOnEnd = true;
    [SerializeField] private bool playImmediatelyIfAlreadyProducing = true;

    private Coroutine loopCoroutine;

    private void Reset()
    {
        makerInfo = GetComponent<MakerInfo>();
        targetRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Awake()
    {
        if (makerInfo == null)
            makerInfo = GetComponent<MakerInfo>();

        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<SpriteRenderer>();

        if (idleSprite == null && targetRenderer != null)
            idleSprite = targetRenderer.sprite;
    }

    private void OnEnable()
    {
        if (makerInfo == null)
            makerInfo = GetComponent<MakerInfo>();

        if (makerInfo != null)
        {
            makerInfo.CraftVisualStarted += StartLoop;
            makerInfo.CraftVisualEnded += StopLoop;
        }

        // 씬 복원 등으로 이미 제작 중인 상태라면 바로 루프 시작
        if (playImmediatelyIfAlreadyProducing && makerInfo != null && makerInfo.isProducing)
        {
            StartLoop();
        }
        else
        {
            SetIdleSprite();
        }
    }

    private void OnDisable()
    {
        if (makerInfo != null)
        {
            makerInfo.CraftVisualStarted -= StartLoop;
            makerInfo.CraftVisualEnded -= StopLoop;
        }

        StopLoop();
    }

    private void StartLoop()
    {
        if (targetRenderer == null)
            return;

        if (craftingFrames == null || craftingFrames.Length == 0)
        {
            Debug.LogWarning($"[CraftSpriteLoopAnimator] {name}: 제작 중 프레임이 없습니다.");
            return;
        }

        if (loopCoroutine != null)
            StopCoroutine(loopCoroutine);

        loopCoroutine = StartCoroutine(SpriteLoopRoutine());
    }

    private IEnumerator SpriteLoopRoutine()
    {
        int index = 0;

        while (true)
        {
            if (craftingFrames[index] != null)
                targetRenderer.sprite = craftingFrames[index];

            index = (index + 1) % craftingFrames.Length;

            yield return new WaitForSeconds(frameInterval);
        }
    }

    private void StopLoop()
    {
        if (loopCoroutine != null)
        {
            StopCoroutine(loopCoroutine);
            loopCoroutine = null;
        }

        if (resetToIdleOnEnd)
            SetIdleSprite();
    }

    private void SetIdleSprite()
    {
        if (targetRenderer != null && idleSprite != null)
            targetRenderer.sprite = idleSprite;
    }
}
