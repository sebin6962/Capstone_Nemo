using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;

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

    [Header("첫 번째 컷신 자막 (아래 공용 UI 사용)")]
    public bool useFirstCutsceneSubtitles = true;
    [TextArea(3, 12)] public string firstSubtitleContent;
    [Tooltip("Cutscene 문자열 테이블의 키. 비우면 Content 원문 사용")]
    public string firstSubtitleLocalizationKey = "cutscene.ending.01";
    [Tooltip("첫 번째 컷신 자막이 표시되기 전 추가 대기 시간(초). 두 번째 컷신에는 적용되지 않습니다.")]
    [Min(0f)] public float firstCutsceneSubtitleStartDelay = 2f;

    [Header("우주선 상호작용 크레딧 (선택)")]
    [SerializeField] private EndingCreditsController endingCredits;
    [Min(0f)][SerializeField] private float creditsRevealFadeSeconds = 2f;
    [Tooltip("두 번째 컷신에서 크레딧으로 전환할 때 검은 화면을 잠깐 유지하는 시간")]
    [Min(0f)][SerializeField] private float creditsBlackHoldSeconds = 0.35f;
    [Tooltip("우주 배경이 완전히 나타난 후 크레딧 텍스트가 내려오기 전 대기 시간")]
    [Min(0f)][SerializeField] private float creditsScrollStartDelay = 1.5f;

    private readonly List<TextMeshProUGUI> firstLines = new List<TextMeshProUGUI>();
    private bool firstSubtitleStarted;
    private bool firstSubtitleFinished = true;
    private int firstLastClickFrame = -1;

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

    private void Awake()
    {
        // 씬 전환 페이드 뒤에서 실제 스크롤 컷신을 먼저 준비합니다.
        if (creditsRoot != null)
            creditsRoot.SetActive(true);

        // 엔딩씬에 진입하는 첫 프레임부터 검은 화면 적용
        if (blackImage != null && fadeInFromBlackOnStart)
        {
            Color color = blackImage.color;
            color.a = 1f;
            blackImage.color = color;
            blackImage.gameObject.SetActive(true);
        }
    }

    private void Start()
    {
        InitializeSecondCutscene();
        if (targetCamera == null)
            targetCamera = Camera.main;

        canStartScroll = false;
        isEnding = false;

        // 페이드 도중에도 컷신 내용이 렌더링되도록 유지합니다.
        if (creditsRoot != null)
            creditsRoot.SetActive(true);

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
        bool hasOpeningPanel = openingCutscenePanel != null;
        bool hasBlackCover =
            fadeInFromBlackOnStart && blackImage != null;

        // 검은 화면 뒤에서 첫 번째 컷신 준비
        if (hasOpeningPanel && hasBlackCover)
        {
            openingCutscenePanel.SetActive(true);

            if (openingCutsceneCanvasGroup != null)
                openingCutsceneCanvasGroup.alpha = 0f;
        }

        if (hasBlackCover)
        {
            // 기존 검은 화면 유지 시간
            if (startBlackStaySeconds > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    startBlackStaySeconds
                );
            }

            // 검은 화면을 유지한 채 첫 번째 컷신 표시 준비
            if (hasOpeningPanel &&
                openingCutsceneCanvasGroup != null)
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

            // 첫 번째 컷신 준비 후 검은 화면 제거
            yield return StartCoroutine(
                FadeBlackImage(
                    1f,
                    0f,
                    startBlackFadeSeconds
                )
            );

            blackImage.gameObject.SetActive(false);
        }

        // 첫 번째 컷신 재생
        if (hasOpeningPanel)
        {
            if (!openingCutscenePanel.activeSelf)
                openingCutscenePanel.SetActive(true);

            // 시작 검은 화면을 사용하지 않는 경우
            if (!hasBlackCover &&
                openingCutsceneCanvasGroup != null)
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

        // 기존 크레딧 및 파티클 연출 유지
        if (creditsRoot != null)
            creditsRoot.SetActive(true);

        PlayCutsceneParticles();

        canStartScroll = true;
        if (useFirstCutsceneSubtitles && secondSubtitleContainer != null && secondSubtitleLinePrefab != null)
        {
            firstSubtitleFinished = false;
            firstSubtitleStarted = true;
            StartCoroutine(FirstCutsceneSubtitleRoutine());
        }
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

        // 첫 컷신 자막이 아직 진행 중이면 끝날 때까지 마지막 화면 유지
        if (firstSubtitleStarted)
            yield return new WaitUntil(() => firstSubtitleFinished);

        // 인트로 컷신과 동일: 먼저 1.5초 동안 검은 화면으로 전환한 뒤 다음 컷 공개
        bool secondAvailable = useSecondCutscene && secondCutscenePanel != null;
        if (secondAvailable && blackImage != null)
        {
            blackImage.gameObject.SetActive(true);
            yield return StartCoroutine(FadeBlackImage(0f, 1f, blackFadeSeconds));
        }

        StopCutsceneParticles();

        yield return SecondCutsceneRoutine();

        // 크레딧 전환 전용 Screen Space - Overlay 검은 커버를 사용합니다.
        // 기존 EndingBlackImage의 Canvas 정렬 순서에 관계없이 두 번째 컷신과
        // 우주 크레딧을 모두 덮도록 하여 화면이 갑자기 바뀌는 현상을 방지합니다.
        EnsureCreditsTransitionCover();
        yield return StartCoroutine(FadeCreditsCover(0f, 1f, blackFadeSeconds));

        // 완전히 검은 뒤에 두 번째 컷신을 정리합니다.
        HideSecondCutscene();

        // 기존 EndingBlackImage는 이제 필요 없으므로 가린 상태에서 숨깁니다.
        // 우주 크레딧과 중복된 검은 화면이 페이드에 영향을 주지 않게 합니다.
        if (blackImage != null)
            blackImage.gameObject.SetActive(false);

        if (endingCredits != null && endingCredits.BeginCredits())
        {
            Canvas.ForceUpdateCanvases();
            yield return null;

            if (creditsBlackHoldSeconds > 0f)
                yield return new WaitForSecondsRealtime(creditsBlackHoldSeconds);

            yield return StartCoroutine(FadeCreditsCover(1f, 0f, creditsRevealFadeSeconds));

            if (creditsScrollStartDelay > 0f)
                yield return new WaitForSecondsRealtime(creditsScrollStartDelay);

            endingCredits.StartScrolling();
            yield return new WaitUntil(() => endingCredits == null || endingCredits.IsFinished);

            // 크레딧 종료도 동일한 커버로 마무리합니다.
            yield return StartCoroutine(FadeCreditsCover(0f, 1f, blackFadeSeconds));
            if (endingCredits != null) endingCredits.HideCredits();
        }
        else
        {
            Debug.LogWarning("[EndingCutSceneScroller] 우주 크레딧을 시작하지 못했습니다. 마지막 자막 없이 씬으로 복귀합니다.", this);
        }

        // 엔딩 완료 기록과 기존 TreeScene 복귀 로직은 보존합니다.
        LoadNextScene();
    }

    private IEnumerator LoadEndingLocalizedText(string key, string fallback,
        System.Action<string> onComplete)
    {
        string result = fallback ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(key))
        {
            var handle = LocalizationSettings.StringDatabase.GetLocalizedStringAsync("Cutscene", key);
            yield return handle;
            if (handle.Status == AsyncOperationStatus.Succeeded &&
                !string.IsNullOrWhiteSpace(handle.Result))
                result = handle.Result;
            else
                Debug.LogWarning("[EndingCutSceneScroller] 번역 키를 찾지 못해 원문 사용: " + key);
            Addressables.Release(handle);
        }
        onComplete?.Invoke(result);
    }

    private IEnumerator FirstCutsceneSubtitleRoutine()
    {
        firstLastClickFrame = Time.frameCount;
        string raw = firstSubtitleContent ?? string.Empty;
        yield return LoadEndingLocalizedText(firstSubtitleLocalizationKey, raw,
            result => raw = result);

        if (string.IsNullOrWhiteSpace(raw))
        {
            Debug.LogWarning("[EndingCutSceneScroller] 첫 컷신 자막이 비었습니다. First Subtitle Content 또는 cutscene.ending.01 번역을 확인하세요.", this);
            firstSubtitleFinished = true;
            yield break;
        }

        // 첫 번째 컷신 전용 추가 대기. 자막과 그라데이션이 나타나기 전에 적용합니다.
        // 공용 첫 줄 딜레이는 기존대로 이후에 적용됩니다.
        if (firstCutsceneSubtitleStartDelay > 0f)
            yield return FirstSubtitleDelay(firstCutsceneSubtitleStartDelay, false);

        ClearFirstSubtitleLines();
        string delimiter = string.IsNullOrEmpty(sharedSegmentDelimiter) ? "||" : sharedSegmentDelimiter;
        string[] segments = raw.Split(new[] { delimiter }, System.StringSplitOptions.None);
        foreach (string segment in segments)
        {
            var line = Instantiate(secondSubtitleLinePrefab, secondSubtitleContainer);
            line.gameObject.SetActive(true);
            line.alignment = sharedForceTopLeftAlign ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.Top;
            line.enableWordWrapping = true;
            line.enableAutoSizing = false;
            line.richText = true;
            line.lineSpacing = sharedTextLineSpacing;
            line.raycastTarget = false;
            line.text = segment.Replace("\\n", "\n").TrimEnd('\r', '\n', ' ');
            var lineGroup = line.GetComponent<CanvasGroup>();
            if (lineGroup == null) lineGroup = line.gameObject.AddComponent<CanvasGroup>();
            lineGroup.alpha = 1f;
            line.maxVisibleCharacters = 0;
            var le = line.GetComponent<LayoutElement>();
            if (le != null) { le.minHeight = 0f; le.preferredHeight = -1f; le.flexibleHeight = 0f; }
            firstLines.Add(line);
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(secondSubtitleContainer);
        if (secondSubtitleGroup != null) secondSubtitleGroup.alpha = 1f;
        if (secondGradientGroup != null)
            StartCoroutine(FadeCanvasGroup(secondGradientGroup, 0f, 1f, sharedGradientFadeSeconds));
        yield return FirstSubtitleDelay(sharedFirstLineDelay, false);
        for (int i = 0; i < firstLines.Count; i++)
        {
            yield return TypeFirstLine(firstLines[i]);
            if (i < firstLines.Count - 1)
                yield return FirstSubtitleDelay(sharedBetweenLinesDelay, sharedClickCompletesCurrentLine);
        }
        yield return FirstSubtitleDelay(sharedAfterAllLinesHoldSeconds, sharedClickToSkipAfterAllLines);

        Coroutine textFade = null;
        Coroutine gradientFade = null;
        if (secondSubtitleGroup != null)
            textFade = StartCoroutine(FadeCanvasGroup(secondSubtitleGroup, secondSubtitleGroup.alpha, 0f, sharedOverlayFadeOutSeconds));
        if (secondGradientGroup != null)
            gradientFade = StartCoroutine(FadeCanvasGroup(secondGradientGroup, secondGradientGroup.alpha, 0f, sharedOverlayFadeOutSeconds));
        if (textFade != null) yield return textFade;
        if (gradientFade != null) yield return gradientFade;
        ClearFirstSubtitleLines();
        firstSubtitleFinished = true;
    }

    private bool ConsumeFirstClick()
    {
        if (firstLastClickFrame == Time.frameCount || !Input.GetMouseButtonDown(0)) return false;
        firstLastClickFrame = Time.frameCount;
        return true;
    }

    private IEnumerator FirstSubtitleDelay(float seconds, bool allowClick)
    {
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            if (allowClick && ConsumeFirstClick()) yield break;
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private IEnumerator TypeFirstLine(TextMeshProUGUI line)
    {
        line.ForceMeshUpdate();
        int count = line.textInfo.characterCount;
        float shown = 0f;
        while (shown < count)
        {
            if (sharedClickCompletesCurrentLine && ConsumeFirstClick()) break;
            shown += Mathf.Max(1f, sharedCharsPerSecond) * Time.unscaledDeltaTime;
            line.maxVisibleCharacters = Mathf.Min(count, Mathf.FloorToInt(shown));
            yield return null;
        }
        line.maxVisibleCharacters = count;
    }

    private void ClearFirstSubtitleLines()
    {
        foreach (var line in firstLines)
            if (line != null) { line.gameObject.SetActive(false); Destroy(line.gameObject); }
        firstLines.Clear();
    }

    // 우주 크레딧 전용 커버입니다. 전역 FadeManager와 기존 컷신 Canvas는 건드리지 않습니다.
    // SubtitleCanvas(100)와 CreditsCanvas(90)보다 앞에 렌더링됩니다.
    private Image creditsTransitionCover;

    private void EnsureCreditsTransitionCover()
    {
        if (creditsTransitionCover != null) return;

        GameObject canvasObject = new GameObject(
            "EndingCreditsTransitionCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        Canvas coverCanvas = canvasObject.GetComponent<Canvas>();
        coverCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        coverCanvas.overrideSorting = true;
        coverCanvas.sortingOrder = 200; // FadeManager(32767)보다 낮음

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GameObject imageObject = new GameObject(
            "BlackCover", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(canvasObject.transform, false);
        creditsTransitionCover = imageObject.GetComponent<Image>();
        creditsTransitionCover.color = new Color(0f, 0f, 0f, 0f);
        creditsTransitionCover.raycastTarget = false;
        RectTransform rect = creditsTransitionCover.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    // 두 번째 컷신 → 검은 화면 → 우주 크레딧 페이드 전용.
    private IEnumerator FadeCreditsCover(float from, float to, float duration)
    {
        EnsureCreditsTransitionCover();
        Color color = creditsTransitionCover.color;
        if (duration <= 0f)
        {
            color.a = to;
            creditsTransitionCover.color = color;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            color.a = Mathf.Lerp(from, to, t);
            creditsTransitionCover.color = color;
            yield return null;
        }
        color.a = to;
        creditsTransitionCover.color = color;
    }

    private IEnumerator FadeBlackImage(float from, float to, float duration)
    {
        float t = 0f;
        Color color = blackImage.color;
        color.a = from;
        blackImage.color = color;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
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

    [Header("두 번째 컷신")]
    public bool useSecondCutscene = true;
    [Tooltip("이 컴포넌트가 붙은 오브젝트와 별도의 전체 화면 UI 패널")]
    public GameObject secondCutscenePanel;
    public CanvasGroup secondCutsceneGroup;
    [Min(0f)] public float secondFadeInSeconds = 1.5f;
    [Min(0f)] public float secondPanelHoldSeconds = 4f;

    [Header("두 번째 컷신 이미지 Pan / Zoom")]
    public RectTransform secondTargetImage;
    public bool secondUseImagePan;
    public Vector2 secondStartAnchoredPos;
    public Vector2 secondEndAnchoredPos;
    public bool secondUseZoom;
    public Vector3 secondStartScale = Vector3.one;
    public Vector3 secondEndScale = Vector3.one;
    [Min(0f)] public float secondMoveDuration = 3f;

    [Header("두 번째 컷신 자막 내용")]
    [TextArea(3, 12)] public string secondSubtitleContent;
    [Tooltip("Cutscene 문자열 테이블의 키. 비우면 Content 원문 사용")]
    public string secondSubtitleLocalizationKey = "cutscene.ending.02";

    [Header("공용 자막 UI (1·2컷 모두 재사용)")]
    [InspectorName("Subtitle Container (공용)")]
    public RectTransform secondSubtitleContainer;
    [InspectorName("Subtitle Line Prefab (공용)")]
    public TextMeshProUGUI secondSubtitleLinePrefab;
    [InspectorName("Subtitle Group (공용)")]
    public CanvasGroup secondSubtitleGroup;

    [Header("인트로와 동일한 공용 자막 타이밍")]
    [Min(0f)] public float sharedFirstLineDelay = 1f;
    [Min(0f)] public float sharedBetweenLinesDelay = 4f;
    [Min(0f)] public float sharedLineFadeSeconds = 0.5f; // 인트로 타자기 모드에서는 사용하지 않음
    [Min(0f)] public float sharedAfterAllLinesHoldSeconds = 9f;
    [Min(1f)] public float sharedCharsPerSecond = 28f;
    public string sharedSegmentDelimiter = "||";
    public bool sharedClickCompletesCurrentLine = true;
    public bool sharedClickToSkipAfterAllLines = true;
    [Min(0f)] public float sharedLineSpacingPx = 50f;
    public float sharedTextLineSpacing = 8f;
    public bool sharedForceTopLeftAlign = false;

    [Header("공용 자막 그라데이션")]
    [InspectorName("Gradient Overlay (공용)")]
    public Image secondGradientOverlay;
    [Tooltip("인트로처럼 그라데이션 이미지가 이미 지정돼 있다면 끄기")]
    public bool secondGenerateGradient = false;
    [Tooltip("기존 RectTransform 배치 유지 권장")]
    public bool secondAutoLayoutGradient = false;
    [Min(1f)] public float secondGradientHeight = 800f;
    [Range(0f, 1f)] public float secondBottomAlpha = 0.915f;
    [Range(0f, 1f)] public float secondTopAlpha = 0f;
    [Min(0f)] public float sharedGradientFadeSeconds = 0.8f;
    [Min(0f)] public float sharedOverlayFadeOutSeconds = 0.7f;

    private CanvasGroup secondGradientGroup;
    private Sprite secondGeneratedGradientSprite;
    private Texture2D secondGeneratedGradientTexture;
    private Coroutine secondPanCoroutine;
    private readonly List<TextMeshProUGUI> secondLines = new List<TextMeshProUGUI>();
    private int secondLastClickFrame = -1;

    // 공용 UI는 월드 스크롤과 무관한 화면 좌표계에서만 표시합니다.
    // FadeManager의 최상위 Canvas(32767)는 변경하지 않습니다.
    private Canvas runtimeSubtitleCanvas;

    private void EnsureSharedSubtitleScreenCanvas()
    {
        if (secondSubtitleContainer == null || secondGradientOverlay == null)
        {
            Debug.LogWarning("[EndingCutSceneScroller] 공용 SubtitleContainer / Gradient Overlay 연결을 확인하세요.", this);
            return;
        }

        // 기존 UI가 월드 Canvas에 있거나 서로 다른 Canvas에 있더라도
        // 하나의 전용 화면 Canvas로 모아 카메라 이동 중 위치가 고정되게 합니다.
        Canvas containerCanvas = secondSubtitleContainer.GetComponentInParent<Canvas>();
        Canvas gradientCanvas = secondGradientOverlay.GetComponentInParent<Canvas>();
        // 이미 Inspector에서 만든 화면 고정 Canvas를 사용 중이라면
        // 위치·앵커·크기를 절대 덮어쓰지 않습니다.
        // Canvas 이름과 무관하게 기존 Screen Space - Overlay Canvas를 재사용합니다.
        if (containerCanvas != null && containerCanvas == gradientCanvas &&
            containerCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            runtimeSubtitleCanvas = containerCanvas;
        }
        else
        {
            GameObject go = new GameObject("EndingSharedSubtitleCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            runtimeSubtitleCanvas = go.GetComponent<Canvas>();
            runtimeSubtitleCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform gradientRt = secondGradientOverlay.rectTransform;
            RectTransform subtitleRt = secondSubtitleContainer;
            gradientRt.SetParent(go.transform, false);
            subtitleRt.SetParent(go.transform, false);

            // 월드 좌표를 남기지 않고 화면 하단에 다시 배치합니다.
            gradientRt.anchorMin = new Vector2(0f, 0f);
            gradientRt.anchorMax = new Vector2(1f, 0f);
            gradientRt.pivot = new Vector2(0.5f, 0f);
            gradientRt.anchoredPosition = Vector2.zero;
            gradientRt.sizeDelta = new Vector2(0f, secondGradientHeight);
            gradientRt.localScale = Vector3.one;
            gradientRt.localRotation = Quaternion.identity;

            subtitleRt.anchorMin = new Vector2(0.08f, 0f);
            subtitleRt.anchorMax = new Vector2(0.92f, 0f);
            subtitleRt.pivot = new Vector2(0.5f, 0f);
            subtitleRt.anchoredPosition = new Vector2(0f, 85f);
            subtitleRt.sizeDelta = new Vector2(0f, 430f);
            subtitleRt.localScale = Vector3.one;
            subtitleRt.localRotation = Quaternion.identity;
        }

        runtimeSubtitleCanvas.overrideSorting = true;
        runtimeSubtitleCanvas.sortingOrder = 100;
        runtimeSubtitleCanvas.enabled = true;
        runtimeSubtitleCanvas.gameObject.SetActive(true);
        secondGradientOverlay.gameObject.SetActive(true);
        secondSubtitleContainer.gameObject.SetActive(true);
        secondGradientOverlay.transform.SetAsFirstSibling();
        secondSubtitleContainer.SetAsLastSibling();

        // 해당 씬의 검은 페이드 이미지는 자막보다 앞에 있어야 컷 전환이 자연스럽습니다.
        // 전역 FadeManager가 소유한 Canvas는 건드리지 않습니다.
        if (blackImage != null)
        {
            Canvas blackCanvas = blackImage.GetComponentInParent<Canvas>();
            if (blackCanvas != null && blackCanvas != runtimeSubtitleCanvas &&
                blackCanvas.renderMode == RenderMode.ScreenSpaceOverlay &&
                (FadeManager.Instance == null || blackCanvas.gameObject != FadeManager.Instance.gameObject &&
                 !blackCanvas.transform.IsChildOf(FadeManager.Instance.transform)))
            {
                blackCanvas.overrideSorting = true;
                blackCanvas.sortingOrder = 101;
            }
        }
    }

    private CanvasGroup GetOrAddSecondGroup(GameObject obj)
    {
        var group = obj.GetComponent<CanvasGroup>();
        return group != null ? group : obj.AddComponent<CanvasGroup>();
    }

    private void InitializeSecondCutscene()
    {
        EnsureSharedSubtitleScreenCanvas();
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
            if (secondCutscenePanel != null &&
                secondSubtitleContainer.IsChildOf(secondCutscenePanel.transform))
                Debug.LogWarning("[EndingCutSceneScroller] 공용 SubtitleContainer를 CutScene02 밖(Canvas 아래)으로 옮겨주세요.", this);
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
                layout.spacing = sharedLineSpacingPx;
            }
        }
        if (secondGradientOverlay != null)
        {
            var rt = secondGradientOverlay.rectTransform;
            // 인트로 CutSceneManager처럼 높이는 Inspector 수치를 반영하고 기존 앵커는 유지
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, secondGradientHeight);
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
            if (secondCutscenePanel != null &&
                secondGradientOverlay.transform.IsChildOf(secondCutscenePanel.transform))
                Debug.LogWarning("[EndingCutSceneScroller] 공용 GradientOverlay를 CutScene02 밖(Canvas 아래)으로 옮겨주세요.", this);
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
        if (blackImage != null && blackImage.gameObject.activeSelf)
        {
            // 검은 화면 아래에서 두 번째 컷을 완성한 후 1.5초 동안 밝히기
            secondCutsceneGroup.alpha = 1f;
            yield return FadeBlackImage(1f, 0f, secondFadeInSeconds);
            blackImage.gameObject.SetActive(false);
        }
        else
        {
            // 개별 씬에서 검은 커버가 없다면 기존의 컷 이미지 페이드 사용
            yield return FadeCanvasGroup(secondCutsceneGroup, 0f, 1f, secondFadeInSeconds);
        }

        string secondLocalizedText = secondSubtitleContent ?? string.Empty;
        yield return LoadEndingLocalizedText(secondSubtitleLocalizationKey, secondLocalizedText,
            result => secondLocalizedText = result);
        bool hasSubtitles = !string.IsNullOrWhiteSpace(secondLocalizedText);
        if (hasSubtitles && secondSubtitleContainer != null && secondSubtitleLinePrefab != null)
        {
            // Fade in the gradient concurrently with subtitle timing, and join it before fading out.
            Coroutine gradientFade = null;
            if (secondGradientGroup != null)
                gradientFade = StartCoroutine(FadeCanvasGroup(secondGradientGroup, 0f, 1f, sharedGradientFadeSeconds));
            yield return ShowSecondSubtitles(secondLocalizedText);
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
            subtitleFade = StartCoroutine(FadeCanvasGroup(secondSubtitleGroup, secondSubtitleGroup.alpha, 0f, sharedOverlayFadeOutSeconds));
        if (secondGradientGroup != null)
            overlayFade = StartCoroutine(FadeCanvasGroup(secondGradientGroup, secondGradientGroup.alpha, 0f, sharedOverlayFadeOutSeconds));
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

    private IEnumerator ShowSecondSubtitles(string subtitleText)
    {
        ClearSecondLines();
        string delimiter = string.IsNullOrEmpty(sharedSegmentDelimiter) ? "||" : sharedSegmentDelimiter;
        string[] segments = subtitleText.Split(new[] { delimiter }, System.StringSplitOptions.None);
        foreach (string segment in segments)
        {
            var line = Instantiate(secondSubtitleLinePrefab, secondSubtitleContainer);
            line.gameObject.SetActive(true);
            line.alignment = sharedForceTopLeftAlign ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.Top;
            line.enableWordWrapping = true;
            line.enableAutoSizing = false;
            line.richText = true;
            line.raycastTarget = false;
            line.lineSpacing = sharedTextLineSpacing;
            GetOrAddSecondGroup(line.gameObject).alpha = 1f;
            line.text = segment.Replace("\\n", "\n").TrimEnd('\r', '\n', ' ');
            line.maxVisibleCharacters = 0;
            var le = line.GetComponent<LayoutElement>();
            if (le != null) { le.minHeight = 0f; le.preferredHeight = -1f; le.flexibleHeight = 0f; }
            secondLines.Add(line);
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(secondSubtitleContainer);
        yield return SecondDelay(sharedFirstLineDelay, false);
        for (int i = 0; i < secondLines.Count; i++)
        {
            yield return TypeSecondLine(secondLines[i]);
            if (i < secondLines.Count - 1)
                yield return SecondDelay(sharedBetweenLinesDelay, sharedClickCompletesCurrentLine);
        }
        yield return SecondDelay(sharedAfterAllLinesHoldSeconds, sharedClickToSkipAfterAllLines);
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
            if (sharedClickCompletesCurrentLine && ConsumeSecondClick()) break;
            shown += Mathf.Max(1f, sharedCharsPerSecond) * Time.unscaledDeltaTime;
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

