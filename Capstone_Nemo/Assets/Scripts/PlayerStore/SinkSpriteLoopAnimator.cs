using System.Collections;
using UnityEngine;

public class SinkSpriteLoopAnimator : MonoBehaviour
{
    [Header("연결 대상")]
    [SerializeField] private SinkInfo sinkInfo;
    [SerializeField] private SpriteRenderer targetRenderer;

    [Header("기본 스프라이트")]
    [SerializeField] private Sprite idleSprite;

    [Header("물 긷는 중 루프 스프라이트")]
    [SerializeField] private Sprite[] runningFrames;

    [Header("재생 설정")]
    [SerializeField] private float frameInterval = 0.15f;
    [SerializeField] private bool resetToIdleOnEnd = true;
    [SerializeField] private bool playImmediatelyIfAlreadyRunning = true;

    private Coroutine loopCoroutine;

    private void Reset()
    {
        sinkInfo = GetComponent<SinkInfo>();
        targetRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Awake()
    {
        if (sinkInfo == null)
            sinkInfo = GetComponent<SinkInfo>();

        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<SpriteRenderer>();

        if (idleSprite == null && targetRenderer != null)
            idleSprite = targetRenderer.sprite;
    }

    private void OnEnable()
    {
        if (sinkInfo == null)
            sinkInfo = GetComponent<SinkInfo>();

        if (sinkInfo != null)
        {
            sinkInfo.SinkVisualStarted += StartLoop;
            sinkInfo.SinkVisualEnded += StopLoop;
        }

        if (playImmediatelyIfAlreadyRunning && sinkInfo != null && sinkInfo.IsRunning)
            StartLoop();
        else
            SetIdleSprite();
    }

    private void OnDisable()
    {
        if (sinkInfo != null)
        {
            sinkInfo.SinkVisualStarted -= StartLoop;
            sinkInfo.SinkVisualEnded -= StopLoop;
        }

        StopLoop();
    }

    private void StartLoop()
    {
        if (targetRenderer == null)
            return;

        if (runningFrames == null || runningFrames.Length == 0)
        {
            Debug.LogWarning($"[SinkSpriteLoopAnimator] {name}: 물 긷는 중 프레임이 없습니다.");
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
            if (runningFrames[index] != null)
                targetRenderer.sprite = runningFrames[index];

            index = (index + 1) % runningFrames.Length;

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
