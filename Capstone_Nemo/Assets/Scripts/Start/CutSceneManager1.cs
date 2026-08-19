using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CutSceneManager1 : MonoBehaviour
{
    [Header("기존 컷 패널 (책 페이지 방식에서는 비활성화 상태로 유지)")]
    public List<GameObject> cutPanels = new List<GameObject>();

    [Header("페이드용 검은 화면")]
    public Image fadeImage;

    [Header("기본 타이밍")]
    [Tooltip("자막이 없는 컷을 보여주는 시간(초)")]
    public float panelHoldSeconds = 3f;

    [Tooltip("첫 화면 및 씬 전환 페이드 시간(초)")]
    public float fadeSeconds = 1.5f;

    [Tooltip("기존 호환용 값")]
    public float cutSceneDuration = 3f;

    [Tooltip("버튼 대신 오른쪽 모서리를 직접 드래그해서 넘김")]
    public bool allowManualPageDrag = true;

    [System.Serializable]
    public class SubtitleEntry
    {
        [TextArea(2, 10)]
        public string content;
    }

    [System.Serializable]
    public class CutImagePanEntry
    {
        [Tooltip("이동 또는 확대할 UI 이미지")]
        public RectTransform targetImage;

        public bool useImagePan = false;
        public Vector2 startAnchoredPos;
        public Vector2 endAnchoredPos;

        public bool useZoom = false;
        public Vector3 startScale = Vector3.one;
        public Vector3 endScale = Vector3.one;

        public float moveDuration = 3f;
    }

    [Header("자막 (책 컷 순서와 동일)")]
    [Tooltip("한 컷 안에서 문장을 나누려면 || 를 사용")]
    public List<SubtitleEntry> subtitles = new List<SubtitleEntry>();

    [Header("기존 컷 이미지 이동/확대 데이터")]
    [Tooltip("책 스프라이트 방식에서는 자동 실행되지 않으며 기존 데이터 보존용")]
    public List<CutImagePanEntry> cutImagePans = new List<CutImagePanEntry>();

    [Tooltip("자막이 추가될 컨테이너")]
    public RectTransform subtitleContainer;

    [Tooltip("자막 한 줄 프리팹")]
    public TextMeshProUGUI subtitleLinePrefab;

    [Tooltip("자막 컨테이너의 CanvasGroup")]
    public CanvasGroup subtitleGroup;

    [Header("자막 줄 스타일")]
    public float lineSpacingPx = 6f;
    public float textLineSpacing = 1f;
    public bool forceTopLeftAlign = true;

    [Header("자막 타이밍")]
    public float firstLineDelay = 0.5f;
    public float betweenLinesDelay = 0.8f;
    public float lineFadeSeconds = 0.5f;
    public float afterAllLinesHoldSeconds = 0.8f;

    [Header("자막 표시 규칙")]
    public string segmentDelimiter = "||";

    [Header("Subtitle Gradient")]
    public Image gradientOverlay;
    public float gradientHeight = 320f;
    [Range(0f, 1f)] public float bottomAlpha = 0.65f;
    [Range(0f, 1f)] public float topAlpha = 0f;
    public float gradientFadeSeconds = 0.3f;

    private CanvasGroup gradientGroup;

    [Header("페이지를 넘기기 전 자막/그라데이션 페이드아웃")]
    public float overlayFadeOutSeconds = 0.35f;

    [Header("타자기 옵션")]
    [Tooltip("초당 표시할 글자 수")]
    public float charsPerSecond = 28f;

    [Tooltip("타자기 재생 중 클릭하면 현재 문장을 즉시 완성")]
    public bool clickCompletesCurrentLine = true;

    [Tooltip("모든 문장이 표시된 뒤 클릭하면 남은 대기 시간을 생략")]
    public bool clickToSkipAfterAllLines = true;

    [Tooltip("페이지를 넘길 수 있을 때 표시할 화살표")]
    public GameObject nextArrowIndicator;

    [Tooltip("스킵 버튼이 나타나기 전 대기 시간")]
    public float arrowDelaySeconds = 2f;

    [Header("컷신 건너뛰기 버튼")]
    public GameObject skipButton;
    public float skipButtonFadeSeconds = 0.4f;

    [Header("책 페이지 넘김")]
    [Tooltip("에셋의 Book 컴포넌트")]
    public Book book;

    [Tooltip("같은 책 오브젝트의 AutoFlip 컴포넌트")]
    public AutoFlip autoFlip;

    [Tooltip("오른쪽 페이지 위에 배치한 투명 UI Button")]
    public Button rightPageClickButton;

    [Tooltip("책에 들어간 실제 컷(펼침면)의 개수")]
    [Min(1)]
    public int bookCutCount = 1;

    [Tooltip("자막 연출 종료 후 페이지 클릭 활성화까지의 짧은 대기")]
    public float pageClickEnableDelay = 0.2f;

    [Tooltip("마지막 컷도 빈 페이지 쪽으로 넘긴 뒤 씬 전환")]
    public bool flipLastPageBeforeTransition = true;

    private CanvasGroup skipButtonGroup;
    private Coroutine currentImagePanCoroutine;

    private bool isSkipping;
    private bool pageTurnRequested;
    private bool pageFlipCompleted;
    private bool isPageFlipping;

    private void Awake()
    {
        foreach (GameObject panel in cutPanels)
        {
            if (panel != null)
                panel.SetActive(false);
        }

        if (fadeImage != null)
        {
            Color color = fadeImage.color;
            color.a = 1f;
            fadeImage.color = color;
            fadeImage.gameObject.SetActive(true);
            fadeImage.raycastTarget = false;
        }

        SetupSubtitleContainer();
        SetupGradient();
        SetupSkipButton();
        SetupBookPageTurn();
    }

    private void Start()
    {
        SetPageTurnUI(false);

        if (skipButton != null)
        {
            skipButton.SetActive(false);

            if (skipButtonGroup != null)
                skipButtonGroup.alpha = 0f;
        }

        StartCoroutine(PlayCutAndTransition());

        if (skipButton != null)
            StartCoroutine(ShowSkipButtonAfterDelay());
    }

    private void OnDestroy()
    {
        if (book != null && book.OnFlip != null)
            book.OnFlip.RemoveListener(OnBookPageFlipped);

        if (rightPageClickButton != null)
            rightPageClickButton.onClick.RemoveListener(OnClickRightPage);
    }

    private void SetupSubtitleContainer()
    {
        if (subtitleContainer == null)
            return;

        subtitleContainer.anchorMin = new Vector2(0f, 0f);
        subtitleContainer.anchorMax = new Vector2(1f, 0f);
        subtitleContainer.pivot = new Vector2(0.5f, 0f);

        VerticalLayoutGroup layout = subtitleContainer.GetComponent<VerticalLayoutGroup>();
        if (layout != null)
        {
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            layout.spacing = lineSpacingPx;
        }

        if (subtitleGroup == null)
            subtitleGroup = subtitleContainer.GetComponent<CanvasGroup>();

        if (subtitleGroup == null)
            subtitleGroup = subtitleContainer.gameObject.AddComponent<CanvasGroup>();

        subtitleGroup.alpha = 1f;
        ClearSubtitleContainer();
    }

    private void SetupGradient()
    {
        if (gradientOverlay == null)
            return;

        RectTransform rectTransform = gradientOverlay.rectTransform;
        rectTransform.anchorMin = new Vector2(0f, 0f);
        rectTransform.anchorMax = new Vector2(1f, 0f);
        rectTransform.pivot = new Vector2(0.5f, 0f);
        rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, gradientHeight);

        gradientGroup = gradientOverlay.GetComponent<CanvasGroup>();
        if (gradientGroup == null)
            gradientGroup = gradientOverlay.gameObject.AddComponent<CanvasGroup>();

        gradientGroup.alpha = 0f;
        gradientOverlay.raycastTarget = false;

        if (gradientOverlay.sprite == null)
        {
            gradientOverlay.sprite = MakeVerticalGradientSprite(
                4,
                Mathf.RoundToInt(gradientHeight),
                new Color(0f, 0f, 0f, topAlpha),
                new Color(0f, 0f, 0f, bottomAlpha)
            );

            gradientOverlay.type = Image.Type.Simple;
        }
    }

    private void SetupSkipButton()
    {
        if (skipButton == null)
            return;

        skipButtonGroup = skipButton.GetComponent<CanvasGroup>();
        if (skipButtonGroup == null)
            skipButtonGroup = skipButton.AddComponent<CanvasGroup>();

        skipButtonGroup.alpha = 0f;
        skipButton.SetActive(false);
    }

    private void SetupBookPageTurn()
    {
        if (book != null)
        {
            book.interactable = false;

            if (book.OnFlip == null)
                book.OnFlip = new UnityEngine.Events.UnityEvent();

            book.OnFlip.RemoveListener(OnBookPageFlipped);
            book.OnFlip.AddListener(OnBookPageFlipped);
        }

        if (autoFlip != null)
        {
            autoFlip.AutoStartFlip = false;
            autoFlip.Mode = FlipMode.RightToLeft;
            autoFlip.AnimationFramesCount = Mathf.Max(1, autoFlip.AnimationFramesCount);

            if (autoFlip.ControledBook == null)
                autoFlip.ControledBook = book;
        }

        if (rightPageClickButton != null)
        {
            rightPageClickButton.onClick.RemoveListener(OnClickRightPage);
            rightPageClickButton.onClick.AddListener(OnClickRightPage);
            rightPageClickButton.interactable = false;
            rightPageClickButton.gameObject.SetActive(false);
        }
    }

    private IEnumerator ShowSkipButtonAfterDelay()
    {
        yield return new WaitForSeconds(arrowDelaySeconds);

        if (isSkipping || skipButton == null)
            yield break;

        skipButton.SetActive(true);

        if (skipButtonGroup != null)
        {
            skipButtonGroup.alpha = 0f;
            yield return FadeCanvasGroup(skipButtonGroup, 0f, 1f, skipButtonFadeSeconds);
        }
    }

    private IEnumerator PlayCutAndTransition()
    {
        if (fadeImage == null || book == null || autoFlip == null)
        {
            Debug.LogError("[CutSceneManager] FadeImage, Book, AutoFlip 연결을 확인해주세요.");
            yield return TransitionToVillage();
            yield break;
        }

        int totalCuts = Mathf.Max(1, bookCutCount);

        yield return Fade(1f, 0f, fadeSeconds);

        for (int cutIndex = 0; cutIndex < totalCuts; cutIndex++)
        {
            if (isSkipping)
                yield break;

            pageTurnRequested = false;
            pageFlipCompleted = false;
            isPageFlipping = false;

            ClearSubtitleContainer();

            if (subtitleGroup != null)
                subtitleGroup.alpha = 1f;

            if (gradientGroup != null)
                gradientGroup.alpha = 0f;

            if (HasSubtitle(cutIndex))
            {
                yield return ShowSubtitleStackRoutine(cutIndex);
            }
            else if (panelHoldSeconds > 0f)
            {
                yield return new WaitForSeconds(panelHoldSeconds);
            }

            if (isSkipping)
                yield break;

            if (pageClickEnableDelay > 0f)
                yield return new WaitForSeconds(pageClickEnableDelay);

            if (allowManualPageDrag)
            {
                // 자막이 페이지와 따로 떠 있는 상태에서 종이가 말리지 않도록
                // 드래그 활성화 전에 자막을 먼저 숨김
                yield return FadeOutSubsAndGradient(overlayFadeOutSeconds);
                ClearSubtitleContainer();

                pageFlipCompleted = false;
                isPageFlipping = true;

                // 이때부터 오른쪽 모서리를 잡을 수 있음
                SetPageTurnUI(true);

                // 충분히 넘기지 않고 놓으면 Book이 원위치로 돌아오며
                // OnFlip이 호출되지 않으므로 계속 다시 잡을 수 있음
                while (!pageFlipCompleted)
                {
                    if (isSkipping)
                        yield break;

                    yield return null;
                }

                SetPageTurnUI(false);
                isPageFlipping = false;
            }
            else
            {
                // 기존 투명 버튼 자동 넘김 방식
                pageTurnRequested = false;
                SetPageTurnUI(true);

                while (!pageTurnRequested)
                {
                    if (isSkipping)
                        yield break;

                    yield return null;
                }

                SetPageTurnUI(false);

                yield return FadeOutSubsAndGradient(overlayFadeOutSeconds);
                ClearSubtitleContainer();
            }

            bool isLastCut = cutIndex == totalCuts - 1;

            if (isLastCut && !flipLastPageBeforeTransition)
            {
                yield return TransitionToVillage();
                yield break;
            }

            if (book.currentPage >= book.TotalPageCount)
            {
                Debug.LogWarning(
                    $"[CutSceneManager] 넘길 페이지가 없습니다. " +
                    $"CurrentPage={book.currentPage}, TotalPageCount={book.TotalPageCount}"
                );

                yield return TransitionToVillage();
                yield break;
            }

            // 수동 드래그 방식에서는 사용자가 이미 페이지를 넘겼으므로
            // AutoFlip을 다시 실행하면 안 됨
            if (!allowManualPageDrag)
            {
                pageFlipCompleted = false;
                isPageFlipping = true;

                autoFlip.FlipRightPage();

                while (!pageFlipCompleted)
                {
                    if (isSkipping)
                        yield break;

                    yield return null;
                }

                isPageFlipping = false;
            }

            if (isLastCut)
            {
                yield return TransitionToVillage();
                yield break;
            }
        }
    }

    public void OnClickRightPage()
    {
        if (isSkipping || isPageFlipping || pageTurnRequested)
            return;

        if (rightPageClickButton == null || !rightPageClickButton.interactable)
            return;

        if (SFXManager.Instance != null)
            SFXManager.Instance.PlayIntroClickSFX();

        pageTurnRequested = true;
        SetPageTurnUI(false);
    }

    private void OnBookPageFlipped()
    {
        pageFlipCompleted = true;
    }

    private void SetPageTurnUI(bool active)
    {
        // 모서리 드래그 방식
        if (book != null)
            book.interactable = active && allowManualPageDrag;

        // 자동 넘김 버튼 방식
        if (rightPageClickButton != null)
        {
            bool buttonActive = active && !allowManualPageDrag;

            rightPageClickButton.gameObject.SetActive(buttonActive);
            rightPageClickButton.interactable = buttonActive;
        }

        if (nextArrowIndicator != null)
            nextArrowIndicator.SetActive(active);
    }

    private IEnumerator TransitionToVillage()
    {
        SetPageTurnUI(false);

        if (skipButton != null)
            skipButton.SetActive(false);

        if (fadeImage != null)
            yield return Fade(fadeImage.color.a, 1f, fadeSeconds * 0.5f);

        if (VillageSceneManager.Instance != null)
        {
            Destroy(VillageSceneManager.Instance.gameObject);
            VillageSceneManager.Instance = null;
        }

        PlayerPrefs.SetInt("StartTimeOnEnter", 1);

        if (FadeManager.Instance != null)
            FadeManager.Instance.FadeToScene("VillageScene");
        else
            Debug.LogError("[CutSceneManager] FadeManager.Instance가 없습니다.");

        yield return null;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (fadeImage == null)
            yield break;

        Color color = fadeImage.color;
        color.a = from;
        fadeImage.color = color;

        if (duration <= 0f)
        {
            color.a = to;
            fadeImage.color = color;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            fadeImage.color = color;
            yield return null;
        }

        color.a = to;
        fadeImage.color = color;
    }

    private bool HasSubtitle(int index)
    {
        if (subtitles == null || index < 0 || index >= subtitles.Count)
            return false;

        SubtitleEntry entry = subtitles[index];
        return entry != null && !string.IsNullOrWhiteSpace(entry.content);
    }

    private IEnumerator ShowSubtitleStackRoutine(int index)
    {
        if (gradientGroup != null)
        {
            StartCoroutine(
                FadeCanvasGroup(
                    gradientGroup,
                    gradientGroup.alpha,
                    1f,
                    gradientFadeSeconds
                )
            );
        }

        if (subtitleContainer == null || subtitleLinePrefab == null)
            yield break;

        string raw = subtitles[index] != null ? subtitles[index].content : string.Empty;
        if (string.IsNullOrWhiteSpace(raw))
            yield break;

        string[] segments = raw.Split(
            new[] { segmentDelimiter },
            System.StringSplitOptions.None
        );

        ClearSubtitleContainer();
        List<TextMeshProUGUI> lines = new List<TextMeshProUGUI>(segments.Length);

        for (int i = 0; i < segments.Length; i++)
        {
            string segment = (segments[i] ?? string.Empty)
                .Replace("\\n", "\n")
                .TrimEnd('\r', '\n', ' ');

            TextMeshProUGUI line = Instantiate(subtitleLinePrefab, subtitleContainer);

            if (forceTopLeftAlign)
                line.alignment = TextAlignmentOptions.TopLeft;
            else
                line.alignment = TextAlignmentOptions.Top;

            line.enableWordWrapping = true;
            line.enableAutoSizing = false;
            line.lineSpacing = textLineSpacing;
            line.text = segment;

            CanvasGroup lineGroup = line.GetComponent<CanvasGroup>();
            if (lineGroup == null)
                lineGroup = line.gameObject.AddComponent<CanvasGroup>();

            lineGroup.alpha = 1f;

            line.ForceMeshUpdate();
            line.maxVisibleCharacters = 0;
            lines.Add(line);

            LayoutElement layoutElement = line.GetComponent<LayoutElement>();
            if (layoutElement != null)
            {
                layoutElement.minHeight = 0f;
                layoutElement.preferredHeight = -1f;
                layoutElement.flexibleHeight = 0f;
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(subtitleContainer);

        if (firstLineDelay > 0f)
            yield return new WaitForSeconds(firstLineDelay);

        for (int i = 0; i < lines.Count; i++)
        {
            yield return TypeLine(lines[i]);

            if (i >= lines.Count - 1 || betweenLinesDelay <= 0f)
                continue;

            float elapsed = 0f;
            while (elapsed < betweenLinesDelay)
            {
                if (clickCompletesCurrentLine && Input.GetMouseButtonDown(0))
                    break;

                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        if (afterAllLinesHoldSeconds <= 0f)
            yield break;

        if (!clickToSkipAfterAllLines)
        {
            yield return new WaitForSeconds(afterAllLinesHoldSeconds);
            yield break;
        }

        float holdElapsed = 0f;
        while (holdElapsed < afterAllLinesHoldSeconds)
        {
            if (Input.GetMouseButtonDown(0))
                break;

            holdElapsed += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator TypeLine(TextMeshProUGUI line)
    {
        if (line == null)
            yield break;

        line.ForceMeshUpdate();

        int totalCharacters = line.textInfo.characterCount;
        float secondsPerCharacter = 1f / Mathf.Max(1f, charsPerSecond);
        int visibleCharacters = 0;

        while (visibleCharacters < totalCharacters)
        {
            if (clickCompletesCurrentLine && Input.GetMouseButtonDown(0))
            {
                if (SFXManager.Instance != null)
                    SFXManager.Instance.PlayIntroClickSFX();

                line.maxVisibleCharacters = totalCharacters;
                yield break;
            }

            visibleCharacters++;
            line.maxVisibleCharacters = visibleCharacters;

            float elapsed = 0f;
            while (elapsed < secondsPerCharacter)
            {
                if (clickCompletesCurrentLine && Input.GetMouseButtonDown(0))
                {
                    line.maxVisibleCharacters = totalCharacters;
                    yield break;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        line.maxVisibleCharacters = totalCharacters;
    }

    private IEnumerator FadeOutSubsAndGradient(float duration)
    {
        Coroutine subtitleFade = null;
        Coroutine gradientFade = null;

        if (subtitleGroup != null)
        {
            subtitleFade = StartCoroutine(
                FadeCanvasGroup(subtitleGroup, subtitleGroup.alpha, 0f, duration)
            );
        }

        if (gradientGroup != null)
        {
            gradientFade = StartCoroutine(
                FadeCanvasGroup(gradientGroup, gradientGroup.alpha, 0f, duration)
            );
        }

        if (subtitleFade != null)
            yield return subtitleFade;

        if (gradientFade != null)
            yield return gradientFade;
    }

    private IEnumerator FadeCanvasGroup(
        CanvasGroup group,
        float from,
        float to,
        float duration
    )
    {
        if (group == null)
            yield break;

        group.alpha = from;

        if (duration <= 0f)
        {
            group.alpha = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.SmoothStep(
                from,
                to,
                Mathf.Clamp01(elapsed / duration)
            );
            yield return null;
        }

        group.alpha = to;
    }

    private Sprite MakeVerticalGradientSprite(
        int width,
        int height,
        Color top,
        Color bottom
    )
    {
        width = Mathf.Max(1, width);
        height = Mathf.Max(1, height);

        Texture2D texture = new Texture2D(
            width,
            height,
            TextureFormat.ARGB32,
            false
        );

        for (int y = 0; y < height; y++)
        {
            float normalizedY = (float)y / Mathf.Max(1, height - 1);
            Color color = Color.Lerp(bottom, top, normalizedY);

            for (int x = 0; x < width; x++)
                texture.SetPixel(x, y, color);
        }

        texture.wrapMode = TextureWrapMode.Clamp;
        texture.Apply();

        return Sprite.Create(
            texture,
            new Rect(0, 0, width, height),
            new Vector2(0.5f, 0.5f),
            100f
        );
    }

    private void ClearSubtitleContainer()
    {
        if (subtitleContainer == null)
            return;

        for (int i = subtitleContainer.childCount - 1; i >= 0; i--)
            Destroy(subtitleContainer.GetChild(i).gameObject);
    }

    // 기존 데이터와의 호환을 위해 남겨둔 이미지 이동/확대 기능이다.
    // Book 에셋 내부 Image를 직접 움직이면 페이지 컬 구조가 깨질 수 있으므로
    // 현재 책 페이지 재생 루틴에서는 자동으로 호출하지 않는다.
    private void SetupCutImagePan(int index)
    {
        if (cutImagePans == null || index < 0 || index >= cutImagePans.Count)
            return;

        CutImagePanEntry panData = cutImagePans[index];
        if (panData == null || panData.targetImage == null)
            return;

        if (panData.useImagePan)
            panData.targetImage.anchoredPosition = panData.startAnchoredPos;

        if (panData.useZoom)
            panData.targetImage.localScale = panData.startScale;
    }

    private void StartCutImagePan(int index)
    {
        if (cutImagePans == null || index < 0 || index >= cutImagePans.Count)
            return;

        CutImagePanEntry panData = cutImagePans[index];
        if (panData == null || panData.targetImage == null)
            return;

        if (!panData.useImagePan && !panData.useZoom)
            return;

        if (currentImagePanCoroutine != null)
            StopCoroutine(currentImagePanCoroutine);

        currentImagePanCoroutine = StartCoroutine(
            AnimateCutImagePan(
                panData.targetImage,
                panData.useImagePan,
                panData.startAnchoredPos,
                panData.endAnchoredPos,
                panData.useZoom,
                panData.startScale,
                panData.endScale,
                panData.moveDuration
            )
        );
    }

    private IEnumerator AnimateCutImagePan(
        RectTransform target,
        bool usePan,
        Vector2 startPosition,
        Vector2 endPosition,
        bool useZoom,
        Vector3 startScale,
        Vector3 endScale,
        float duration
    )
    {
        if (target == null)
            yield break;

        duration = Mathf.Max(0.01f, duration);

        if (usePan)
            target.anchoredPosition = startPosition;

        if (useZoom)
            target.localScale = startScale;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, elapsed / duration);

            if (usePan)
            {
                target.anchoredPosition = Vector2.Lerp(
                    startPosition,
                    endPosition,
                    progress
                );
            }

            if (useZoom)
                target.localScale = Vector3.Lerp(startScale, endScale, progress);

            yield return null;
        }

        if (usePan)
            target.anchoredPosition = endPosition;

        if (useZoom)
            target.localScale = endScale;

        currentImagePanCoroutine = null;
    }

    public void OnClickSkipButton()
    {
        if (SFXManager.Instance != null)
            SFXManager.Instance.PlayBtnClickSFX();

        if (isSkipping)
            return;

        isSkipping = true;

        StopAllCoroutines();
        currentImagePanCoroutine = null;

        SetPageTurnUI(false);

        if (skipButton != null)
            skipButton.SetActive(false);

        StartCoroutine(SkipCutSceneRoutine());
    }

    private IEnumerator SkipCutSceneRoutine()
    {
        yield return TransitionToVillage();
    }
}