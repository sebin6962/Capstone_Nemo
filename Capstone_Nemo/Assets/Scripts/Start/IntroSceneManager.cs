using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;

public class IntroSceneManager : MonoBehaviour
{
    // 게임 중 일시정지 메뉴에서 IntroScene으로 돌아올 때만 사용한다.
    // 씬이 로드되면 한 번 소비되어 일반 타이틀 진입에는 영향을 주지 않는다.
    private static bool openSaveSelectOnNextLoad;

    public static void RequestOpenSaveSelectOnNextLoad()
    {
        openSaveSelectOnNextLoad = true;
    }

    private static bool ConsumeOpenSaveSelectRequest()
    {
        if (!openSaveSelectOnNextLoad)
            return false;

        openSaveSelectOnNextLoad = false;
        return true;
    }

    [Header("UI References")]
    public CanvasGroup logoUI;
    public CanvasGroup clickTextUI;
    public TextMeshProUGUI clickText;

    [Header("Timing Settings")]
    public float delayBeforeLogo = 2f;
    public float logoFadeDuration = 1f;
    public float textFadeDuration = 1f;

    [Header("Blink Settings")]
    public float blinkSpeed = 1.5f;
    private bool blinking = false;

    private bool clicked = false;
    private bool canClick = false;

    // 타이틀 화면 전체 클릭용 손 커서 등록 상태
    private bool introHandCursorRegistered = false;

    [Header("Gradient Overlay")]
    public Image gradientOverlay;               // 하단 그라데이션용 이미지
    public float gradientHeight = 300f;         // 패널 높이(px)
    [Range(0, 1)] public float bottomAlpha = 0.65f;
    [Range(0, 1)] public float topAlpha = 0f;
    public float gradientFadeSeconds = 0.4f;
    private CanvasGroup gradientGroup;

    [Header("Spaceship")]
    public RectTransform spaceshipUI;       // 우주선 UI 이미지의 RectTransform
    public float shipPxPerSec = 120f;       // 초당 이동 속도 (UI 픽셀/초)
    public float offscreenMarginPx = 30f;   // 왼쪽 화면 밖 시작 여유(px)
    public bool startShipAfterText = true;

    [Header("Spaceship Wave")]
    public float waveAmplitudePx = 18f;     // 위아래 진폭(px)
    public float waveFrequency = 0.6f;      // 1초당 사이클 수(Hz)
    public float wavePhaseDegrees = 0f;     // 시작 위상(도)

    [Header("Spaceship Sprites (Cycle)")]
    public UnityEngine.UI.Image spaceshipImage; // 우주선 UI Image 컴포넌트
    public Sprite[] shipCycle = new Sprite[4];

    private bool moveShip = false;
    private float baseAnchoredY;            // 기준 y (현재 배치한 anchored Y)
    private float moveStartTime;            // 이동 시작 시각
    private int shipSpriteIdx = 0;

    private bool textStarted = false;     // 텍스트 페이드인 시작 여부
    private bool shipStarted = false;     // 우주선 이동 시작 여부
    private Coroutine flowCoroutine;      // 메인 시퀀스 코루틴 핸들

    private bool allowSkipToText = false;

    [Header("Logo Drop Bounce")]
    public RectTransform logoRect;              // 로고 UI의 RectTransform
    public float logoStartOffsetY = 350f;       // 시작 위치: 최종 위치보다 위로 얼마나 올릴지
    public float logoDropDuration = 1.1f;       // 위에서 내려오는 시간
    public float logoImpactOffsetY = 28f;       // 배경에 닿는 느낌: 최종 위치보다 아래로 내려가는 정도
    public float logoBounceOvershootY = 14f;    // 튕겨 올라가는 정도
    public float logoBounceUpDuration = 0.22f;  // 튕겨 올라가는 시간
    public float logoSettleDuration = 0.18f;    // 제자리로 돌아오는 시간

    [Header("Shooting Stars")]
    [SerializeField] private ShootingStarSpawner shootingStarSpawner;

    [Header("Save Select Transition")]
    [SerializeField] private GameObject titleRoot;
    [SerializeField] private Image moonImage;

    [SerializeField] private GameObject saveSelectRoot;
    [SerializeField] private CanvasGroup saveSelectCanvasGroup;

    [SerializeField]
    private Color saveSelectMoonColor =
        new Color(0.45f, 0.45f, 0.45f, 1f);

    [SerializeField] private float titleFadeOutDuration = 0.35f;
    [SerializeField] private float moonColorDuration = 0.5f;

    // 타이틀 페이드가 진행되는 도중 세이브 UI를 시작할 시간
    [SerializeField] private float saveSelectStartDelay = 0.15f;

    private bool openingSaveSelect;

    [Header("Save Select Animation")]
    [SerializeField]
    private SaveSelectOpenAnimator saveSelectOpenAnimator;

    [Header("Background Effect Fade")]
    [SerializeField] private CanvasGroup spaceshipCanvasGroup;
    [SerializeField] private CanvasGroup shootingStarCanvasGroup;
    [SerializeField] private float backgroundEffectFadeDuration = 0.35f;

    void Awake()
    {
        // 하단 그라데이션 세팅
        if (gradientOverlay != null)
        {
            var rt = gradientOverlay.rectTransform;
            // 앵커를 하단에 고정 (왼쪽 아래 ~ 오른쪽 아래)
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);

            // Pivot을 하단에 고정 (0.5, 0)
            rt.pivot = new Vector2(0.5f, 0f);

            // anchoredPosition을 (0, 0)으로 설정해 화면 하단 경계선과 일치시킴
            rt.anchoredPosition = Vector2.zero;

            // 패널 높이 지정 (위로 gradientHeight만큼 올라오게)
            rt.sizeDelta = new Vector2(0, gradientHeight);

            // 기존 내용 유지
            gradientGroup = gradientOverlay.GetComponent<CanvasGroup>();
            if (!gradientGroup) gradientGroup = gradientOverlay.gameObject.AddComponent<CanvasGroup>();
            gradientGroup.alpha = 0f;
            gradientOverlay.raycastTarget = false;

            if (gradientOverlay.sprite == null)
            {
                gradientOverlay.sprite = MakeVerticalGradientSprite(
                    4, Mathf.RoundToInt(gradientHeight),
                    new Color(0, 0, 0, topAlpha),
                    new Color(0, 0, 0, bottomAlpha)
                );
                gradientOverlay.type = Image.Type.Simple;
            }
        }
    }
    void Start()
    {
        bool openSaveSelectImmediately =
            ConsumeOpenSaveSelectRequest();

        if (titleRoot != null)
            titleRoot.SetActive(true);

        // 초기 상태: 전부 숨김
        logoUI.alpha = 0;
        logoUI.gameObject.SetActive(true);

        if (logoRect == null && logoUI != null)
            logoRect = logoUI.GetComponent<RectTransform>();

        clickTextUI.alpha = 0;
        clickTextUI.gameObject.SetActive(false);

        if (saveSelectOpenAnimator != null)
            saveSelectOpenAnimator.PrepareHidden();

        if (saveSelectRoot != null)
            saveSelectRoot.SetActive(false);

        if (saveSelectCanvasGroup != null)
        {
            saveSelectCanvasGroup.alpha = 1f;
            saveSelectCanvasGroup.interactable = false;
            saveSelectCanvasGroup.blocksRaycasts = false;
        }

        if (spaceshipCanvasGroup != null)
            spaceshipCanvasGroup.alpha = 1f;

        if (shootingStarCanvasGroup != null)
            shootingStarCanvasGroup.alpha = 1f;

        if (openSaveSelectImmediately)
        {
            PrepareReturnedSaveSelectState();
            StartCoroutine(OpenSaveSelectAfterSceneReturn());
            return;
        }

        flowCoroutine = StartCoroutine(FlowSequence());
    }

    /// <summary>
    /// 플레이 중 메뉴에서 IntroScene으로 복귀했을 때 인트로를 건너뛰고
    /// 세이브 선택 화면이 열리기 직전 상태로 맞춘다.
    /// </summary>
    private void PrepareReturnedSaveSelectState()
    {
        clicked = true;
        canClick = false;
        blinking = false;
        openingSaveSelect = true;
        textStarted = true;
        shipStarted = true;
        moveShip = false;

        UnregisterIntroHandCursor();

        if (logoUI != null)
            logoUI.alpha = 0f;

        if (clickTextUI != null)
        {
            clickTextUI.alpha = 0f;
            clickTextUI.gameObject.SetActive(false);
        }

        if (titleRoot != null)
            titleRoot.SetActive(false);

        if (gradientGroup != null)
            gradientGroup.alpha = 1f;

        if (moonImage != null)
        {
            Color moonColor = saveSelectMoonColor;
            moonColor.a = moonImage.color.a;
            moonImage.color = moonColor;
        }

        if (spaceshipCanvasGroup != null)
        {
            spaceshipCanvasGroup.alpha = 0f;
            spaceshipCanvasGroup.gameObject.SetActive(false);
        }

        if (shootingStarSpawner != null)
            shootingStarSpawner.StopSpawning(true);

        if (shootingStarCanvasGroup != null)
        {
            shootingStarCanvasGroup.alpha = 0f;
            shootingStarCanvasGroup.gameObject.SetActive(false);
        }

        if (saveSelectOpenAnimator != null)
            saveSelectOpenAnimator.PrepareHidden();

        if (saveSelectRoot != null)
            saveSelectRoot.SetActive(true);

        if (saveSelectCanvasGroup != null)
        {
            saveSelectCanvasGroup.alpha = 1f;
            saveSelectCanvasGroup.interactable = false;
            saveSelectCanvasGroup.blocksRaycasts = false;
        }
    }

    /// <summary>
    /// SaveSelectRoot의 OnEnable 초기화가 끝난 다음 프레임부터
    /// 탭, 패널, 슬롯 등장 모션만 재생한다.
    /// </summary>
    private IEnumerator OpenSaveSelectAfterSceneReturn()
    {
        yield return null;

        if (saveSelectOpenAnimator != null)
        {
            saveSelectOpenAnimator.PrepareHidden();
            yield return StartCoroutine(
                saveSelectOpenAnimator.PlayOpen()
            );
        }

        if (saveSelectCanvasGroup != null)
        {
            saveSelectCanvasGroup.alpha = 1f;
            saveSelectCanvasGroup.interactable = true;
            saveSelectCanvasGroup.blocksRaycasts = true;
        }
    }

    void Update()
    {
        // 마우스 왼쪽 클릭 체크
        if (!clicked && Input.GetMouseButtonDown(0))
        {
            // 안내 문구가 활성화된 뒤에는 첫 클릭으로 바로 씬 전환
            if (canClick && !openingSaveSelect)
            {
                clicked = true;
                blinking = false;
                openingSaveSelect = true;

                // 타이틀 전체 클릭용 손 커서 해제
                UnregisterIntroHandCursor();

                if (SFXManager.Instance != null)
                    SFXManager.Instance.PlayIntroClickSFX();

                StartCoroutine(OpenSaveSelectUI());
            }
            // 로고 연출이 끝난 뒤 클릭하면 남은 대기 시간을 건너뜀
            else if (!textStarted && allowSkipToText)
            {
                if (SFXManager.Instance != null)
                    SFXManager.Instance.PlayIntroClickSFX();

                ForceStartTextStage();
            }
        }



        // 우주선
        if (moveShip && spaceshipUI != null)
        {
            Vector2 ap = spaceshipUI.anchoredPosition;

            // x: 등속 이동 (픽셀/초)
            ap.x += shipPxPerSec * Time.unscaledDeltaTime;

            // y: 사인 웨이브
            float t = Time.unscaledTime - moveStartTime;
            float phaseRad = wavePhaseDegrees * Mathf.Deg2Rad;
            ap.y = baseAnchoredY + waveAmplitudePx * Mathf.Sin(2f * Mathf.PI * waveFrequency * t + phaseRad);

            spaceshipUI.anchoredPosition = ap;

            Canvas rootCanvas = spaceshipUI.GetComponentInParent<Canvas>();
            RectTransform canvasRT = rootCanvas != null ? rootCanvas.GetComponent<RectTransform>() : null;
            float canvasWidth = (canvasRT != null) ? canvasRT.rect.width : Screen.width;

            float halfShipW = spaceshipUI.rect.width * 0.5f;
            float rightOutX = canvasWidth * 0.5f + halfShipW + offscreenMarginPx;

            if (ap.x > rightOutX)
            {
                // 다시 왼쪽 화면 밖에서 출발 + 스프라이트 다음 것으로 교체
                RestartSpaceshipFromLeft();
            }
        }
    }

    private IEnumerator OpenSaveSelectUI()
    {
        canClick = false;

        // 세 연출을 동시에 시작
        StartCoroutine(FadeOutBackgroundEffects());
        StartCoroutine(FadeOutTitleElements());
        StartCoroutine(TransitionMoonColor());

        // 타이틀이 완전히 사라질 때까지 기다리지 않고
        // 페이드가 진행되는 도중 세이브 UI 등장 시작
        yield return WaitRealtime(saveSelectStartDelay);

        // 비활성 상태에서 먼저 숨김값 적용
        if (saveSelectOpenAnimator != null)
            saveSelectOpenAnimator.PrepareHidden();

        if (saveSelectRoot != null)
            saveSelectRoot.SetActive(true);

        // OnEnable에서 UI 상태가 변경될 가능성이 있으므로
        // 활성화 직후 같은 프레임에 다시 숨김값 적용
        if (saveSelectOpenAnimator != null)
            saveSelectOpenAnimator.PrepareHidden();

        if (saveSelectCanvasGroup != null)
        {
            saveSelectCanvasGroup.alpha = 1f;

            // 버튼이 Disabled 색상으로 변하지 않게 유지
            saveSelectCanvasGroup.interactable = true;

            // 연출 중 마우스 입력만 차단
            saveSelectCanvasGroup.blocksRaycasts = false;
        }

        if (saveSelectOpenAnimator != null)
        {
            yield return StartCoroutine(
                saveSelectOpenAnimator.PlayOpen()
            );
        }

        if (saveSelectCanvasGroup != null)
        {
            saveSelectCanvasGroup.interactable = true;
            saveSelectCanvasGroup.blocksRaycasts = true;
        }
    }

    private IEnumerator FadeOutTitleElements()
    {
        float logoStartAlpha =
            logoUI != null ? logoUI.alpha : 0f;

        float textStartAlpha =
            clickTextUI != null ? clickTextUI.alpha : 0f;

        float elapsed = 0f;

        while (elapsed < titleFadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = titleFadeOutDuration <= 0f
                ? 1f
                : Mathf.Clamp01(
                    elapsed / titleFadeOutDuration
                );

            float eased =
                progress * progress * (3f - 2f * progress);

            if (logoUI != null)
            {
                logoUI.alpha =
                    Mathf.Lerp(logoStartAlpha, 0f, eased);
            }

            if (clickTextUI != null)
            {
                clickTextUI.alpha =
                    Mathf.Lerp(textStartAlpha, 0f, eased);
            }

            yield return null;
        }

        if (logoUI != null)
            logoUI.alpha = 0f;

        if (clickTextUI != null)
            clickTextUI.alpha = 0f;

        if (titleRoot != null)
            titleRoot.SetActive(false);
    }

    private IEnumerator TransitionMoonColor()
    {
        if (moonImage == null)
            yield break;

        Color startColor = moonImage.color;
        Color targetColor = saveSelectMoonColor;

        targetColor.a = startColor.a;

        float elapsed = 0f;

        while (elapsed < moonColorDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = moonColorDuration <= 0f
                ? 1f
                : Mathf.Clamp01(
                    elapsed / moonColorDuration
                );

            float eased =
                progress * progress * (3f - 2f * progress);

            moonImage.color =
                Color.Lerp(startColor, targetColor, eased);

            yield return null;
        }

        moonImage.color = targetColor;
    }

    private IEnumerator WaitRealtime(float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private IEnumerator FadeOutBackgroundEffects()
    {
        // 새로운 별똥별만 더 이상 생성하지 않음
        // 현재 날아가던 별똥별은 잠시 계속 움직임
        if (shootingStarSpawner != null)
            shootingStarSpawner.StopSpawning(false);

        float shipStartAlpha =
            spaceshipCanvasGroup != null
                ? spaceshipCanvasGroup.alpha
                : 1f;

        float starStartAlpha =
            shootingStarCanvasGroup != null
                ? shootingStarCanvasGroup.alpha
                : 1f;

        float elapsed = 0f;

        while (elapsed < backgroundEffectFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = backgroundEffectFadeDuration <= 0f
                ? 1f
                : Mathf.Clamp01(
                    elapsed / backgroundEffectFadeDuration
                );

            // 처음에는 천천히, 마지막에 부드럽게 사라짐
            float eased =
                progress * progress * (3f - 2f * progress);

            if (spaceshipCanvasGroup != null)
            {
                spaceshipCanvasGroup.alpha =
                    Mathf.Lerp(shipStartAlpha, 0f, eased);
            }

            if (shootingStarCanvasGroup != null)
            {
                shootingStarCanvasGroup.alpha =
                    Mathf.Lerp(starStartAlpha, 0f, eased);
            }

            yield return null;
        }

        if (spaceshipCanvasGroup != null)
            spaceshipCanvasGroup.alpha = 0f;

        if (shootingStarCanvasGroup != null)
            shootingStarCanvasGroup.alpha = 0f;

        // 완전히 투명해진 뒤 실제 재생 정지
        moveShip = false;

        if (shootingStarSpawner != null)
            shootingStarSpawner.StopSpawning(true);

        if (spaceshipUI != null)
            spaceshipUI.gameObject.SetActive(false);

        if (shootingStarCanvasGroup != null)
            shootingStarCanvasGroup.gameObject.SetActive(false);
    }

    private IEnumerator FlowSequence()
    {
        yield return new WaitForSecondsRealtime(delayBeforeLogo);

        // 타이틀 낙하 → 튕김 → 원위치 도착까지 대기
        yield return StartCoroutine(DropBounceLogo());

        // 타이틀 도착 직후 배경 연출 시작
        StartShipOnce();

        if (shootingStarSpawner != null)
            shootingStarSpawner.StartSpawning();

        // 그라데이션은 안내 문구와 동시에 나타나게 함
        if (gradientGroup != null)
        {
            StartCoroutine(
                FadeCanvasGroup(
                    gradientGroup,
                    0f,
                    1f,
                    gradientFadeSeconds
                )
            );
        }

        // 별도의 클릭이나 추가 대기 없이 즉시 안내 문구 시작
        yield return StartCoroutine(StartTextStage());

        flowCoroutine = null;
    }

    // 텍스트 등장 + 깜빡임 + 클릭 허용까지 한 번에 처리
    private IEnumerator StartTextStage()
    {
        if (textStarted)
            yield break;

        textStarted = true;

        clickTextUI.gameObject.SetActive(true);

        // 문구가 켜지는 순간부터 클릭 허용
        canClick = true;

        // 화면 어디를 클릭해도 되는 상태이므로 손 커서 표시
        RegisterIntroHandCursor();

        yield return StartCoroutine(
            FadeCanvasGroup(
                clickTextUI,
                0,
                1,
                textFadeDuration
            )
        );

        yield return StartCoroutine(FadeCanvasGroup(clickTextUI, 0, 1, textFadeDuration));

        blinking = true;
        StartCoroutine(BlinkText());
    }

    private void StartShipOnce()
    {
        if (shipStarted) return;
        shipStarted = true;

        SetupAndStartSpaceship();   // 내부에서 RestartSpaceshipFromLeft() 호출
    }

    private void ForceStartTextStage()
    {
        if (textStarted) return;   // 이미 시작했으면 무시

        // 메인 흐름 중단
        if (flowCoroutine != null)
        {
            StopCoroutine(flowCoroutine);
            flowCoroutine = null;
        }

        // 로고/그라데이션이 아직 진행 중이어도 상관없이 텍스트 바로 시작
        StartCoroutine(StartTextStage());

        // 우주선도 아직 안 시작됐으면 여기서 한 번 더 보장
        StartShipOnce();
    }

    IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null)
            yield break;

        if (duration <= 0f)
        {
            cg.alpha = to;
            yield break;
        }

        float t = 0f;
        cg.alpha = from;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        cg.alpha = to;
    }

    IEnumerator BlinkText()
    {
        Text text = clickTextUI.GetComponent<Text>();
        Image img = clickTextUI.GetComponent<Image>();
        while (blinking && clickText != null)
        {
            float alpha = (Mathf.Sin(Time.unscaledTime * blinkSpeed * Mathf.PI) + 1f) / 2f; // 0~1 반복
            Color c = clickText.color;
            c.a = alpha;
            clickText.color = c;
            yield return null;
        }
    }

    private Sprite MakeVerticalGradientSprite(int width, int height, Color top, Color bottom)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.ARGB32, false);
        for (int y = 0; y < height; y++)
        {
            float t = (float)y / Mathf.Max(1, height - 1);
            Color c = Color.Lerp(bottom, top, t);
            for (int x = 0; x < width; x++) tex.SetPixel(x, y, c);
        }
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
    }

    // 화면 바깥 '왼쪽 상단'에서 시작하도록 세팅 후 x축 이동 시작
    private void SetupAndStartSpaceship()
    {
        //if (spaceshipUI == null) return;

        //// 현재 배치 y를 기준으로 웨이브
        //baseAnchoredY = spaceshipUI.anchoredPosition.y;

        //// 캔버스 너비를 얻어 '왼쪽 화면 밖' x 계산
        //Canvas rootCanvas = spaceshipUI.GetComponentInParent<Canvas>();
        //RectTransform canvasRT = rootCanvas != null ? rootCanvas.GetComponent<RectTransform>() : null;
        //float canvasWidth = (canvasRT != null) ? canvasRT.rect.width : Screen.width;

        //float halfShipW = spaceshipUI.rect.width * 0.5f;
        //float startX = -canvasWidth * 0.5f - halfShipW - offscreenMarginPx;

        //// 시작 위치 세팅 (x만 왼쪽 화면 밖, y는 현재 값 유지)
        //spaceshipUI.anchoredPosition = new Vector2(startX, baseAnchoredY);

        //moveStartTime = Time.time;
        //moveShip = true;

        RestartSpaceshipFromLeft();
    }

    private void RestartSpaceshipFromLeft()
    {
        if (spaceshipUI == null) return;

        // 1) 스프라이트 순환 적용 (재시작마다 교체)
        if (spaceshipImage != null && shipCycle != null && shipCycle.Length > 0)
        {
            spaceshipImage.sprite = shipCycle[shipSpriteIdx % shipCycle.Length];
            shipSpriteIdx = (shipSpriteIdx + 1) % shipCycle.Length;
        }

        // 2) 기준 Y는 현재 배치된 anchored Y(처음 한 번은 배치값, 이후에도 유지)
        baseAnchoredY = spaceshipUI.anchoredPosition.y;

        // 3) 캔버스 너비를 기준으로 '왼쪽 화면 밖' 시작 X 계산
        Canvas rootCanvas = spaceshipUI.GetComponentInParent<Canvas>();
        RectTransform canvasRT = rootCanvas != null ? rootCanvas.GetComponent<RectTransform>() : null;
        float canvasWidth = (canvasRT != null) ? canvasRT.rect.width : Screen.width;

        float halfShipW = spaceshipUI.rect.width * 0.5f;
        float startX = -canvasWidth * 0.5f - halfShipW - offscreenMarginPx;

        // 4) 시작 위치 세팅
        spaceshipUI.anchoredPosition = new Vector2(startX, baseAnchoredY);

        // 5) 웨이브 기준 시간 초기화 후 이동 시작
        moveStartTime = Time.unscaledTime;
        moveShip = true;
    }

    IEnumerator DropBounceLogo()
    {
        if (logoUI == null)
            yield break;

        if (logoRect == null)
            logoRect = logoUI.GetComponent<RectTransform>();

        if (logoRect == null)
        {
            logoUI.alpha = 1f;
            yield break;
        }

        Vector2 finalPos = logoRect.anchoredPosition;

        Vector2 startPos = finalPos + new Vector2(0f, logoStartOffsetY);
        Vector2 impactPos = finalPos + new Vector2(0f, -logoImpactOffsetY);
        Vector2 bouncePos = finalPos + new Vector2(0f, logoBounceOvershootY);

        // 페이드 없이 바로 보이게
        logoUI.alpha = 1f;
        logoRect.anchoredPosition = startPos;

        float t = 0f;

        // 1) 위에서 내려옴
        while (t < logoDropDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / logoDropDuration);

            float eased = EaseInQuad(p);
            logoRect.anchoredPosition = Vector2.Lerp(startPos, impactPos, eased);

            yield return null;
        }

        logoRect.anchoredPosition = impactPos;

        // 2) 닿은 뒤 부드럽게 위로 '통' 튐
        t = 0f;
        while (t < logoBounceUpDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / logoBounceUpDuration);

            float eased = EaseOutCubic(p);
            logoRect.anchoredPosition = Vector2.Lerp(impactPos, bouncePos, eased);

            yield return null;
        }

        logoRect.anchoredPosition = bouncePos;

        // 3) 다시 제자리로 부드럽게 안착
        t = 0f;
        while (t < logoSettleDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / logoSettleDuration);

            float eased = EaseInOutSine(p);
            logoRect.anchoredPosition = Vector2.Lerp(bouncePos, finalPos, eased);

            yield return null;
        }

        logoRect.anchoredPosition = finalPos;
    }

    float EaseOutCubic(float x)
    {
        return 1f - Mathf.Pow(1f - x, 3f);
    }

    float EaseInOutSine(float x)
    {
        return -(Mathf.Cos(Mathf.PI * x) - 1f) / 2f;
    }

    float EaseInCubic(float x)
    {
        return x * x * x;
    }

    float EaseInQuad(float x)
    {
        return x * x;
    }

    private void RegisterIntroHandCursor()
    {
        if (introHandCursorRegistered)
            return;

        if (GameCursorManager.Instance == null)
            return;

        introHandCursorRegistered = true;
        GameCursorManager.Instance.EnterHandCursor(this);
    }

    private void UnregisterIntroHandCursor()
    {
        if (!introHandCursorRegistered)
            return;

        introHandCursorRegistered = false;

        if (GameCursorManager.Instance != null)
        {
            GameCursorManager.Instance.ExitHandCursor(this);
        }
    }

    private void OnDisable()
    {
        UnregisterIntroHandCursor();
    }

    private void OnDestroy()
    {
        UnregisterIntroHandCursor();
    }
}
