using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class NextDayCutscene : MonoBehaviour
{
    [Header("컷 패널 (여러 개 가능, 순서대로)")]
    public List<GameObject> cutPanels = new List<GameObject>(); // 0,1,2...

    [Header("페이드용 검은 화면 (Canvas 최상단 Image)")]
    public Image fadeImage; // 풀스크린 검정 이미지(초기 Alpha=1 권장)

    [Header("타이밍(초)")]
    [Tooltip("각 패널이 완전 표시된 상태로 유지될 시간")]
    public float holdSeconds = 3.0f;
    [Tooltip("페이드 인/아웃 시간(천천히/자연스럽게)")]
    public float fadeSeconds = 1.5f;

    [Header("마지막 컷 처리")]
    [Tooltip("마지막 컷에서 페이드아웃 후 전환(OFF면 바로 전환)")]
    public bool fadeOutOnLastCut = true;

    [Header("끝난 뒤 실행(원래 씬 전환 메서드 연결)")]
    public UnityEvent onFinished;

    // 버튼이 호출할 메서드
    public void Play()
    {
        StopAllCoroutines();
        StartCoroutine(RunSequence());
    }

    private void Awake()
    {
        // 패널들 비활성화
        foreach (var p in cutPanels)
            if (p != null) p.SetActive(false);

        // 검은 화면 준비
        if (fadeImage != null)
        {
            var c = fadeImage.color;
            c.a = 1f;                // 시작은 검은 화면
            fadeImage.color = c;
            fadeImage.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[NextDayCutscene] fadeImage가 비어있습니다.");
        }
    }

    private IEnumerator RunSequence()
    {
        if (fadeImage == null || cutPanels == null || cutPanels.Count == 0)
        {
            Debug.LogWarning("[NextDayCutscene] 패널/페이드 세팅 누락, 바로 전환합니다.");
            onFinished?.Invoke();
            yield break;
        }

        for (int i = 0; i < cutPanels.Count; i++)
        {
            // 현재 컷 활성화
            cutPanels[i].SetActive(true);

            // 검은 화면 -> 컷으로 페이드 인
            yield return Fade(1f, 0f, fadeSeconds);

            // 컷 유지
            yield return new WaitForSeconds(holdSeconds);

            // 마지막 컷이면: 옵션에 따라 처리
            bool isLast = (i == cutPanels.Count - 1);
            if (isLast)
            {
                if (fadeOutOnLastCut)
                {
                    // 컷 -> 검은 화면 페이드 아웃 후 전환
                    yield return Fade(0f, 1f, fadeSeconds);
                    cutPanels[i].SetActive(false);
                }
                // 씬 전환(원래 호출하던 전환 메서드를 onFinished에 연결)
                onFinished?.Invoke();
                yield break;
            }

            // 다음 컷을 위해 페이드 아웃
            yield return Fade(0f, 1f, fadeSeconds);
            cutPanels[i].SetActive(false);
        }

        // 안전망
        onFinished?.Invoke();
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float t = 0f;
        var color = fadeImage.color;
        color.a = from;
        fadeImage.color = color;

        while (t < duration)
        {
            t += Time.deltaTime;
            color.a = Mathf.Lerp(from, to, t / duration);
            fadeImage.color = color;
            yield return null;
        }
        color.a = to;
        fadeImage.color = color;
    }
}

