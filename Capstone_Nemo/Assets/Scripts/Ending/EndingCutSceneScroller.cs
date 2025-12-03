using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;

[System.Serializable]
public class EndingData
{
    public bool hasSeenEnding;
}

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

    public void LoadNextScene()
    {
        // 현재 선택된 서버 이름 가져오기
        string serverName = PlayerPrefs.GetString("SelectedSave", "");

        if (!string.IsNullOrEmpty(serverName))
        {
            string path = Path.Combine(Application.persistentDataPath, $"ending_{serverName}.json");
            EndingData data = new EndingData { hasSeenEnding = true };
            File.WriteAllText(path, JsonUtility.ToJson(data, true));
        }

        FadeManager.Instance.FadeToScene(nextSceneName);
    }

    void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (blackImage != null)
        {
            if (fadeInFromBlackOnStart)
            {
                // 엔딩씬 시작 시: 화면을 완전히 검게 만든 뒤
                var c = blackImage.color;
                c.a = 1f;
                blackImage.color = c;
                blackImage.gameObject.SetActive(true);

                canStartScroll = false;

                // 잠깐 유지 후 서서히 밝아지도록 코루틴 실행
                StartCoroutine(SceneStartFadeInRoutine());
            }
        }
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

    private IEnumerator SceneStartFadeInRoutine()
    {
        // 씬 로드 후, 완전히 검은 화면을 잠깐 유지
        if (startBlackStaySeconds > 0f)
            yield return new WaitForSeconds(startBlackStaySeconds);

        // 알파1에서 0으로 서서히 페이드
        if (blackImage != null)
        {
            yield return StartCoroutine(FadeBlackImage(1f, 0f, startBlackFadeSeconds));
            // 다 밝아진 뒤엔 굳이 켜둘 필요 없으면 끄기
            blackImage.gameObject.SetActive(false);
        }

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

