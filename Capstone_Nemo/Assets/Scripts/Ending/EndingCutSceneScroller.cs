using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class EndingCutSceneScroller : MonoBehaviour
{
    [Header("스크롤할 카메라 (비워두면 Main Camera 사용)")]
    public Camera targetCamera;

    [Header("컷신 하단 기준점 (배경 맨 아래 위치에 빈 오브젝트 하나 두고 연결)")]
    public Transform bottomLimit;

    [Header("카메라 내려가는 속도")]
    public float scrollSpeed = 1.0f;

    [Header("하단에 도착 후 잠깐 멈추는 시간")]
    public float beforeFadeBlackDelay = 2f;

    [Header("엔딩용 검은 화면 이미지 (UI Image)")]
    public Image blackImage;

    [Tooltip("검은 화면이 완전히 차오르는 시간(초)")]
    public float blackFadeSeconds = 1.5f;

    private bool isEnding = false;

    [Header("자막 시퀀스")]
    public bool useSubtitles = true;

    [Tooltip("검은 화면이 다 찬 후 첫 자막이 뜨기까지 대기 시간")]
    public float delayBeforeFirstSubtitle = 2f;

    [System.Serializable]
    public class SubtitleEntry
    {
        public GameObject subtitleObject;

        [Tooltip("이전 자막이 켜지고 나서 이 자막이 켜지기까지 대기 시간")]
        public float delayFromPrevious = 1f;

        [Tooltip("이 자막이 서서히 나타나는 시간(초)")]
        public float fadeInDuration = 1f;
    }

    [Tooltip("자막 목록")]
    public SubtitleEntry[] subtitles;

    [Tooltip("마지막 자막이 켜진 후, 모든 자막을 비활성화하기까지 대기 시간")]
    public float delayBeforeHideAllSubtitles = 2f;

    [Header("다음 컷씬")]
    [Tooltip("모든 자막이 꺼진 후, 다음 컷씬 페이드까지 대기 시간")]
    public float delayBeforeNextCutsceneFade = 0f;

    [Tooltip("다음 컷씬 페이드(또는 씬 전환)를 여기 이벤트에 연결")]
    public UnityEvent onNextCutsceneFade;

    void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        // 시작할 때 검은 이미지 알파를 0으로 맞춰 둔다.
        if (blackImage != null)
        {
            var c = blackImage.color;
            c.a = 0f;
            blackImage.color = c;
        }
    }

    void Update()
    {
        if (isEnding) return;
        if (targetCamera == null || bottomLimit == null) return;

        // 카메라 하단 y = 카메라 위치 - orthographicSize
        float camBottom = targetCamera.transform.position.y - targetCamera.orthographicSize;
        float cutBottom = bottomLimit.position.y;

        // 아직 컷 하단보다 위에 있으면 아래로 이동
        if (camBottom > cutBottom)
        {
            float move = scrollSpeed * Time.deltaTime;
            targetCamera.transform.position -= new Vector3(0f, move, 0f);
            camBottom = targetCamera.transform.position.y - targetCamera.orthographicSize;
        }

        // 하단에 닿거나 지나가면 검은 화면 페이드인 시작
        if (camBottom <= cutBottom)
        {
            isEnding = true;
            StartCoroutine(FadeToBlackRoutine());
        }
    }

    private IEnumerator FadeToBlackRoutine()
    {
        // 도착 후 잠깐 멈추기
        if (beforeFadeBlackDelay > 0f)
            yield return new WaitForSeconds(beforeFadeBlackDelay);

        // 검은 이미지 페이드
        if (blackImage != null)
        {
            if (!blackImage.gameObject.activeSelf)
                blackImage.gameObject.SetActive(true);

            yield return StartCoroutine(FadeBlackImage(0f, 1f, blackFadeSeconds));
        }

        // 검은 화면이 다 찬 후 자막 / 다음 컷씬 시퀀스 실행
        yield return StartCoroutine(SubtitleSequenceRoutine());
    }

    private IEnumerator FadeBlackImage(float from, float to, float duration)
    {
        float t = 0f;
        Color color = blackImage.color;
        color.a = from;
        blackImage.color = color;

        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(from, to, t / duration);
            color.a = a;
            blackImage.color = color;
            yield return null;
        }

        color.a = to;
        blackImage.color = color;
    }

    private IEnumerator SubtitleSequenceRoutine()
    {
        // 자막을 안 쓰는 경우-> 바로 다음 컷씬 이벤트만 처리
        if (!useSubtitles || subtitles == null || subtitles.Length == 0)
        {
            if (delayBeforeNextCutsceneFade > 0f)
                yield return new WaitForSeconds(delayBeforeNextCutsceneFade);

            if (onNextCutsceneFade != null)
                onNextCutsceneFade.Invoke();

            yield break;
        }

        // 시작 시 모든 자막 비활성화 + 알파 0으로 초기화
        foreach (var s in subtitles)
        {
            if (s.subtitleObject != null)
            {
                s.subtitleObject.SetActive(false);

                CanvasGroup cg = s.subtitleObject.GetComponent<CanvasGroup>();
                if (cg == null)
                    cg = s.subtitleObject.AddComponent<CanvasGroup>();

                cg.alpha = 0f;
            }
        }

        // 검은 화면이 다 찬 뒤 첫 자막까지 대기
        if (delayBeforeFirstSubtitle > 0f)
            yield return new WaitForSeconds(delayBeforeFirstSubtitle);

        // 자막들 순서대로 켜기 (페이드 인)
        for (int i = 0; i < subtitles.Length; i++)
        {
            var entry = subtitles[i];

            if (i > 0 && entry.delayFromPrevious > 0f)
                yield return new WaitForSeconds(entry.delayFromPrevious);

            if (entry.subtitleObject != null)
            {
                float fadeDuration = Mathf.Max(0.01f, entry.fadeInDuration);
                // 이전 자막의 페이드가 끝날 때까지 기다리도록 순차 실행
                yield return StartCoroutine(FadeInSubtitle(entry.subtitleObject, fadeDuration));
            }
        }

        // 마지막 자막이 켜진 후 잠시 대기
        if (delayBeforeHideAllSubtitles > 0f)
            yield return new WaitForSeconds(delayBeforeHideAllSubtitles);

        // 모든 자막 비활성화
        foreach (var s in subtitles)
        {
            if (s.subtitleObject != null)
                s.subtitleObject.SetActive(false);
        }

        // 다음 컷씬 페이드 전 대기
        if (delayBeforeNextCutsceneFade > 0f)
            yield return new WaitForSeconds(delayBeforeNextCutsceneFade);

        // 다음 컷씬 페이드(또는 씬 전환) 호출
        if (onNextCutsceneFade != null)
            onNextCutsceneFade.Invoke();
    }

    private IEnumerator FadeInSubtitle(GameObject obj, float duration)
    {
        if (obj == null)
            yield break;

        obj.SetActive(true);

        CanvasGroup cg = obj.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = obj.AddComponent<CanvasGroup>();

        cg.alpha = 0f;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / duration);
            cg.alpha = a;
            yield return null;
        }

        cg.alpha = 1f;
    }

}

