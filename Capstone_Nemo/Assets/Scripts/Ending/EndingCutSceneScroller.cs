using System.Collections;
using System.Collections.Generic;
using TMPro;
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
        InitializeSecondCutscene();
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

        yield return SecondCutsceneRoutine();

        // 검은 이미지 페이드
        if (blackImage != null)
        {
            if (!blackImage.gameObject.activeSelf)
                blackImage.gameObject.SetActive(true);

            yield return StartCoroutine(FadeBlackImage(0f, 1f, blackFadeSeconds));
        }



        HideSecondCutscene();
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

    [Header("두 번째 컷신")]
    public bool useSecondCutscene = true;
    [Tooltip("이 컴포넌트가 붙은 오브젝트와 별도의 전체 화면 UI 패널")]
    public GameObject secondCutscenePanel;
    public CanvasGroup secondCutsceneGroup;
    [Min(0f)] public float secondFadeInSeconds = 0.8f;
    [Min(0f)] public float secondPanelHoldSeconds = 3f;

    [Header("두 번째 컷신 이미지 Pan / Zoom")]
    public RectTransform secondTargetImage;
    public bool secondUseImagePan;
    public Vector2 secondStartAnchoredPos;
    public Vector2 secondEndAnchoredPos;
    public bool secondUseZoom;
    public Vector3 secondStartScale = Vector3.one;
    public Vector3 secondEndScale = Vector3.one;
    [Min(0f)] public float secondMoveDuration = 3f;

    [Header("두 번째 컷신 TMP 누적 자막")]
    [TextArea(3, 12)] public string secondSubtitleContent;
    public string secondSegmentDelimiter = "||";
    public RectTransform secondSubtitleContainer;
    public TextMeshProUGUI secondSubtitleLinePrefab;
    public CanvasGroup secondSubtitleGroup;
    public float secondLineSpacingPx = 6f;
    public float secondTextLineSpacing = 1f;
    [Min(0f)] public float secondFirstLineDelay = 0.5f;
    [Min(0f)] public float secondBetweenLinesDelay = 0.8f;
    [Min(0f)] public float secondAfterAllLinesHoldSeconds = 0.8f;
    [Min(1f)] public float secondCharsPerSecond = 28f;
    public bool secondClickCompletesCurrentLine = true;
    public bool secondClickToSkipAfterAllLines = true;

    [Header("두 번째 컷신 하단 그라데이션")]
    public Image secondGradientOverlay;
    [Tooltip("켜면 기존 Source Image 대신 아래/위 알파로 그라데이션을 생성합니다.")]
    public bool secondGenerateGradient = true;
    [Tooltip("켜면 부모 하단에 자동 배치합니다. 끄면 인스펙터 RectTransform 설정을 유지합니다.")]
    public bool secondAutoLayoutGradient = false;
    [Min(1f)] public float secondGradientHeight = 320f;
    [Range(0f, 1f)] public float secondBottomAlpha = 0.65f;
    [Range(0f, 1f)] public float secondTopAlpha;
    [Min(0f)] public float secondGradientFadeSeconds = 0.3f;
    [Min(0f)] public float secondOverlayFadeOutSeconds = 0.35f;

    private CanvasGroup secondGradientGroup;
    private Sprite secondGeneratedGradientSprite;
    private Texture2D secondGeneratedGradientTexture;
    private Coroutine secondPanCoroutine;
    private readonly List<TextMeshProUGUI> secondLines = new List<TextMeshProUGUI>();
    private int secondLastClickFrame = -1;

    private CanvasGroup GetOrAddSecondGroup(GameObject obj)
    {
        var group = obj.GetComponent<CanvasGroup>();
        return group != null ? group : obj.AddComponent<CanvasGroup>();
    }

    private void InitializeSecondCutscene()
    {
        if (secondCutscenePanel == null) return;
        if (secondCutscenePanel == gameObject || transform.IsChildOf(secondCutscenePanel.transform))
        {
            Debug.LogError("[EndingCutSceneScroller] 두 번째 패널은 스크립트 오브젝트의 부모가 될 수 없습니다.", this);
            useSecondCutscene = false;
            return;
        }
        if (secondCutsceneGroup == null)
            secondCutsceneGroup = GetOrAddSecondGroup(secondCutscenePanel);
        secondCutsceneGroup.alpha = 0f;
        secondCutsceneGroup.interactable = false;
        secondCutsceneGroup.blocksRaycasts = false;
        secondCutscenePanel.SetActive(false);

        if (secondSubtitleContainer != null)
        {
            if (secondSubtitleGroup == null)
                secondSubtitleGroup = GetOrAddSecondGroup(secondSubtitleContainer.gameObject);
            secondSubtitleGroup.alpha = 0f;
            var layout = secondSubtitleContainer.GetComponent<VerticalLayoutGroup>();
            if (layout != null)
            {
                layout.childAlignment = TextAnchor.UpperCenter;
                layout.childControlWidth = true;
                layout.childForceExpandWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandHeight = false;
                layout.spacing = secondLineSpacingPx;
            }
        }
        if (secondGradientOverlay != null)
        {
            var rt = secondGradientOverlay.rectTransform;
            if (secondAutoLayoutGradient)
            {
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(0f, secondGradientHeight);
                rt.localRotation = Quaternion.identity;
                rt.localScale = Vector3.one;
            }
            secondGradientGroup = GetOrAddSecondGroup(secondGradientOverlay.gameObject);
            secondGradientGroup.alpha = 0f;
            secondGradientOverlay.raycastTarget = false;
            if (secondGenerateGradient || secondGradientOverlay.sprite == null)
            {
                const int height = 128;
                secondGeneratedGradientTexture = new Texture2D(4, height, TextureFormat.RGBA32, false);
                secondGeneratedGradientTexture.wrapMode = TextureWrapMode.Clamp;
                secondGeneratedGradientTexture.filterMode = FilterMode.Bilinear;
                for (int y = 0; y < height; y++)
                {
                    var color = new Color(0f, 0f, 0f,
                        Mathf.Lerp(secondBottomAlpha, secondTopAlpha, y / (float)(height - 1)));
                    for (int x = 0; x < 4; x++) secondGeneratedGradientTexture.SetPixel(x, y, color);
                }
                secondGeneratedGradientTexture.Apply();
                secondGeneratedGradientSprite = Sprite.Create(secondGeneratedGradientTexture,
                    new Rect(0, 0, 4, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                secondGradientOverlay.overrideSprite = null;
                secondGradientOverlay.sprite = secondGeneratedGradientSprite;
                secondGradientOverlay.color = Color.white;
                secondGradientOverlay.type = Image.Type.Simple;
                secondGradientOverlay.preserveAspect = false;
                secondGradientOverlay.useSpriteMesh = false;
                secondGradientOverlay.material = null;
            }
        }
    }

    private IEnumerator SecondCutsceneRoutine()
    {
        if (!useSecondCutscene || secondCutscenePanel == null) yield break;

        secondLastClickFrame = Time.frameCount;
        secondCutsceneGroup.alpha = 0f;
        if (secondSubtitleGroup != null) secondSubtitleGroup.alpha = 1f;
        if (secondGradientGroup != null) secondGradientGroup.alpha = 0f;
        ApplySecondImageMotion(0f);
        secondCutscenePanel.SetActive(true);
        secondPanCoroutine = StartCoroutine(AnimateSecondImage());
        yield return FadeCanvasGroup(secondCutsceneGroup, 0f, 1f, secondFadeInSeconds);

        bool hasSubtitles = !string.IsNullOrWhiteSpace(secondSubtitleContent);
        if (hasSubtitles && secondSubtitleContainer != null && secondSubtitleLinePrefab != null)
        {
            // Fade in the gradient concurrently with subtitle timing, and join it before fading out.
            Coroutine gradientFade = null;
            if (secondGradientGroup != null)
                gradientFade = StartCoroutine(FadeCanvasGroup(secondGradientGroup, 0f, 1f, secondGradientFadeSeconds));
            yield return ShowSecondSubtitles();
            if (gradientFade != null) yield return gradientFade;
        }
        else
        {
            if (hasSubtitles)
                Debug.LogWarning("[EndingCutSceneScroller] 두 번째 자막 Container / Prefab 연결이 필요합니다.", this);
            yield return SecondDelay(secondPanelHoldSeconds, false);
        }

        Coroutine subtitleFade = null;
        Coroutine overlayFade = null;
        if (secondSubtitleGroup != null)
            subtitleFade = StartCoroutine(FadeCanvasGroup(secondSubtitleGroup, secondSubtitleGroup.alpha, 0f, secondOverlayFadeOutSeconds));
        if (secondGradientGroup != null)
            overlayFade = StartCoroutine(FadeCanvasGroup(secondGradientGroup, secondGradientGroup.alpha, 0f, secondOverlayFadeOutSeconds));
        if (subtitleFade != null) yield return subtitleFade;
        if (overlayFade != null) yield return overlayFade;
        // Keep the image visible until FadeToBlackRoutine has fully covered it.
    }

    private void ApplySecondImageMotion(float t)
    {
        if (secondTargetImage == null) return;
        if (secondUseImagePan)
            secondTargetImage.anchoredPosition = Vector2.Lerp(secondStartAnchoredPos, secondEndAnchoredPos, t);
        if (secondUseZoom)
            secondTargetImage.localScale = Vector3.Lerp(secondStartScale, secondEndScale, t);
    }

    private IEnumerator AnimateSecondImage()
    {
        if (secondTargetImage == null || (!secondUseImagePan && !secondUseZoom)) yield break;
        float elapsed = 0f;
        while (elapsed < secondMoveDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            ApplySecondImageMotion(Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / secondMoveDuration)));
            yield return null;
        }
        ApplySecondImageMotion(1f);
    }

    private IEnumerator ShowSecondSubtitles()
    {
        ClearSecondLines();
        string delimiter = string.IsNullOrEmpty(secondSegmentDelimiter) ? "||" : secondSegmentDelimiter;
        string[] segments = secondSubtitleContent.Split(new[] { delimiter }, System.StringSplitOptions.None);
        foreach (string segment in segments)
        {
            var line = Instantiate(secondSubtitleLinePrefab, secondSubtitleContainer);
            line.gameObject.SetActive(true);
            line.alignment = TextAlignmentOptions.Top;
            line.enableWordWrapping = true;
            line.enableAutoSizing = false;
            line.richText = true;
            line.raycastTarget = false;
            line.lineSpacing = secondTextLineSpacing;
            GetOrAddSecondGroup(line.gameObject).alpha = 1f;
            line.text = segment.Replace("\\n", "\n").TrimEnd('\r', '\n', ' ');
            line.maxVisibleCharacters = 0;
            var le = line.GetComponent<LayoutElement>();
            if (le != null) { le.minHeight = 0f; le.preferredHeight = -1f; le.flexibleHeight = 0f; }
            secondLines.Add(line);
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(secondSubtitleContainer);
        yield return SecondDelay(secondFirstLineDelay, false);
        for (int i = 0; i < secondLines.Count; i++)
        {
            yield return TypeSecondLine(secondLines[i]);
            if (i < secondLines.Count - 1)
                yield return SecondDelay(secondBetweenLinesDelay, secondClickCompletesCurrentLine);
        }
        yield return SecondDelay(secondAfterAllLinesHoldSeconds, secondClickToSkipAfterAllLines);
    }

    private bool ConsumeSecondClick()
    {
        if (secondLastClickFrame == Time.frameCount || !Input.GetMouseButtonDown(0)) return false;
        secondLastClickFrame = Time.frameCount;
        return true;
    }

    private IEnumerator SecondDelay(float seconds, bool allowClick)
    {
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            if (allowClick && ConsumeSecondClick()) yield break;
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private IEnumerator TypeSecondLine(TextMeshProUGUI line)
    {
        line.ForceMeshUpdate();
        int count = line.textInfo.characterCount;
        float shown = 0f;
        while (shown < count)
        {
            if (secondClickCompletesCurrentLine && ConsumeSecondClick()) break;
            shown += Mathf.Max(1f, secondCharsPerSecond) * Time.unscaledDeltaTime;
            line.maxVisibleCharacters = Mathf.Min(count, Mathf.FloorToInt(shown));
            yield return null;
        }
        line.maxVisibleCharacters = count;
    }

    private void ClearSecondLines()
    {
        foreach (var line in secondLines)
        {
            if (line == null) continue;
            line.gameObject.SetActive(false);
            Destroy(line.gameObject);
        }
        secondLines.Clear();
    }

    private void HideSecondCutscene()
    {
        if (secondPanCoroutine != null) StopCoroutine(secondPanCoroutine);
        secondPanCoroutine = null;
        ClearSecondLines();
        if (useSecondCutscene && secondCutscenePanel != null) secondCutscenePanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (secondGeneratedGradientSprite != null) Destroy(secondGeneratedGradientSprite);
        if (secondGeneratedGradientTexture != null) Destroy(secondGeneratedGradientTexture);
    }

}

