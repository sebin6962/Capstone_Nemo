using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;

public class EndingCutSceneScroller : MonoBehaviour
{
    [Header("스크롤할 카메라")]
    public Camera targetCamera;

    [Header("컷신 하단 기준점")]
    public Transform bottomLimit;

    [Header("카메라 내려가는 속도")]
    public float scrollSpeed = 1.0f;

    [Header("하단에 도착 후 잠깐 멈추는 시간")]
    public float beforeFadeBlackDelay = 2f;

    [Header("엔딩용 검은 화면 이미지")]
    public Image blackImage;

    [Tooltip("검은 화면이 완전히 차오르는 시간")]
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

        [Tooltip("이 자막이 서서히 나타나는 시간")]
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


    [Header("씬 시작 연출")]
    [Tooltip("엔딩씬이 로드되면 검은 화면에서 서서히 밝아지게 할지 여부")]
    public bool fadeInFromBlackOnStart = true;

    [Tooltip("엔딩씬이 로드된 뒤, 밝아지기 전에 잠깐 유지할 시간")]
    public float startBlackStaySeconds = 0.5f;

    [Tooltip("검은 화면에서 엔딩씬 화면으로 서서히 밝아지는 시간")]
    public float startBlackFadeSeconds = 1.5f;

    // 페이드가 끝나야지만 카메라가 움직일 수 있게 하는 플래그
    private bool canStartScroll = false;

    [Header("엔딩 후 돌아갈 씬 이름")]
    public string nextSceneName = "TreeScene";

    [Header("엔딩 컷신 파티클들")]
    public ParticleSystem[] cutsceneParticles;

    [Header("크레딧 전 컷씬 패널")]
    [SerializeField] private GameObject openingCutscenePanel;

    [SerializeField] private CanvasGroup openingCutsceneCanvasGroup;

    [SerializeField] private float openingFadeInDuration = 0.8f;

    [SerializeField] private float openingDisplayDuration = 3f;

    [SerializeField] private float openingFadeOutDuration = 0.8f;

    [Header("크레딧 콘텐츠")]
    [Tooltip("크레딧 그림과 텍스트만 포함합니다. 카메라와 이 스크립트 오브젝트는 넣지 마세요.")]
    [SerializeField] private GameObject creditsRoot;

    public void LoadNextScene()
    {
        string serverName =
            PlayerPrefs.GetString(
                "SelectedSave",
                ""
            );

        if (string.IsNullOrWhiteSpace(serverName))
        {
            Debug.LogError(
                "[EndingCutSceneScroller] 선택된 세이브가 없어 " +
                "엔딩 완료 상태를 저장할 수 없습니다."
            );
        }
        else if (!SaveService.EnsureLoaded(serverName))
        {
            Debug.LogError(
                "[EndingCutSceneScroller] 통합 세이브를 " +
                $"불러올 수 없습니다: {serverName}"
            );
        }
        else
        {
            if (SaveService.CurrentData.endingData == null)
            {
                SaveService.CurrentData.endingData =
                    new EndingData();
            }

            SaveService.CurrentData
                .endingData
                .hasSeenEnding = true;

            SaveService.CurrentData
                .endingMigrationCompleted = true;

            if (!SaveService.SaveCurrent())
            {
                Debug.LogError(
                    "[EndingCutSceneScroller] 엔딩 완료 상태 " +
                    "저장에 실패했습니다."
                );
            }
        }

        FadeManager.Instance.FadeToScene(
            nextSceneName
        );
    }

    private void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        canStartScroll = false;
        isEnding = false;

        if (creditsRoot != null)
            creditsRoot.SetActive(false);

        if (openingCutscenePanel != null)
        {
            openingCutscenePanel.SetActive(false);

            if (openingCutsceneCanvasGroup == null)
            {
                openingCutsceneCanvasGroup =
                    openingCutscenePanel.GetComponent<CanvasGroup>();
            }
        }

        if (openingCutsceneCanvasGroup != null)
            openingCutsceneCanvasGroup.alpha = 0f;

        if (blackImage != null)
        {
            Color color = blackImage.color;
            color.a = fadeInFromBlackOnStart ? 1f : 0f;
            blackImage.color = color;
            blackImage.gameObject.SetActive(fadeInFromBlackOnStart);
        }

        StartCoroutine(SceneStartSequenceRoutine());
    }

    private void PlayCutsceneParticles()
    {
        if (cutsceneParticles == null) return;

        foreach (var ps in cutsceneParticles)
        {
            if (ps == null) continue;

            ps.Clear();  // 이전 잔상 제거
            ps.Play();
        }
    }

    private void StopCutsceneParticles()
    {
        if (cutsceneParticles == null) return;

        foreach (var ps in cutsceneParticles)
        {
            if (ps == null) continue;
            ps.Stop();
        }
    }

    private IEnumerator SceneStartSequenceRoutine()
    {
        // 1. EndingScene 로딩 후 검은 화면에서 공개
        if (fadeInFromBlackOnStart && blackImage != null)
        {
            if (startBlackStaySeconds > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    startBlackStaySeconds
                );
            }

            yield return StartCoroutine(
                FadeBlackImage(1f, 0f, startBlackFadeSeconds)
            );

            blackImage.gameObject.SetActive(false);
        }

        // 2. 컷씬 패널 한 장 표시
        if (openingCutscenePanel != null)
        {
            openingCutscenePanel.SetActive(true);

            if (openingCutsceneCanvasGroup != null)
            {
                yield return StartCoroutine(
                    FadeCanvasGroup(
                        openingCutsceneCanvasGroup,
                        0f,
                        1f,
                        openingFadeInDuration
                    )
                );
            }

            if (openingDisplayDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    openingDisplayDuration
                );
            }

            if (openingCutsceneCanvasGroup != null)
            {
                yield return StartCoroutine(
                    FadeCanvasGroup(
                        openingCutsceneCanvasGroup,
                        1f,
                        0f,
                        openingFadeOutDuration
                    )
                );
            }

            openingCutscenePanel.SetActive(false);
        }

        // 3. 크레딧 시작
        if (creditsRoot != null)
            creditsRoot.SetActive(true);

        PlayCutsceneParticles();

        canStartScroll = true;
    }

    void Update()
    {
        // 페이드 중이면 바로 리턴
        if (!canStartScroll)
            return;

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

        StopCutsceneParticles();

        // 검은 이미지 페이드
        if (blackImage != null)
        {
            if (!blackImage.gameObject.activeSelf)
                blackImage.gameObject.SetActive(true);

            yield return StartCoroutine(FadeBlackImage(0f, 1f, blackFadeSeconds));
        }

        

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

    private IEnumerator FadeCanvasGroup(
    CanvasGroup canvasGroup,
    float from,
    float to,
    float duration)
    {
        if (canvasGroup == null)
            yield break;

        canvasGroup.alpha = from;

        if (duration <= 0f)
        {
            canvasGroup.alpha = to;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            t = Mathf.SmoothStep(0f, 1f, t);

            canvasGroup.alpha = Mathf.Lerp(from, to, t);

            yield return null;
        }

        canvasGroup.alpha = to;
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

        // 자막들 순서대로 켜기
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

        // 다음 컷씬 페이드 또는 씬 전환 호출(현재 TreeScene으로 씬 전환 필요)
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

