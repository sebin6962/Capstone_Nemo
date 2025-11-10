using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class IntroSceneManager : MonoBehaviour
{
    [Header("UI References")]
    public CanvasGroup logoUI;
    public CanvasGroup clickTextUI;
    public TextMeshProUGUI clickText;

    [Header("Timing Settings")]
    public float delayBeforeLogo = 2f;
    public float logoFadeDuration = 1f;
    public float delayBeforeText = 2f;
    public float textFadeDuration = 1f;

    [Header("Blink Settings")]
    public float blinkSpeed = 1.5f;
    private bool blinking = false;

    private bool clicked = false;
    private bool canClick = false;

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
        // 초기 상태: 전부 숨김
        logoUI.alpha = 0;
        logoUI.gameObject.SetActive(true);
        clickTextUI.alpha = 0;
        clickTextUI.gameObject.SetActive(false);

        StartCoroutine(FlowSequence());
    }

    void Update()
    {
        if (canClick && !clicked && Input.GetMouseButtonDown(0))
        {
            clicked = true;
            //if (SFXManager.Instance != null)
            //    SFXManager.Instance.PlayIntroClickSFX();
            FadeManager.Instance.FadeToScene("SaveSelectScene");
        }

        // 우주선
        if (moveShip && spaceshipUI != null)
        {
            Vector2 ap = spaceshipUI.anchoredPosition;

            // x: 등속 이동 (픽셀/초)
            ap.x += shipPxPerSec * Time.deltaTime;

            // y: 사인 웨이브
            float t = Time.time - moveStartTime;
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

    IEnumerator FlowSequence()
    {
        // 1 로고 등장
        yield return new WaitForSeconds(delayBeforeLogo);
        yield return StartCoroutine(FadeCanvasGroup(logoUI, 0, 1, logoFadeDuration));

        if (gradientGroup != null)
            yield return StartCoroutine(FadeCanvasGroup(gradientGroup, 0, 1, gradientFadeSeconds));

        // 2 텍스트 등장
        yield return new WaitForSeconds(delayBeforeText);
        clickTextUI.gameObject.SetActive(true);
        yield return StartCoroutine(FadeCanvasGroup(clickTextUI, 0, 1, textFadeDuration));

        // 4 우주선 이동 시작
        if (startShipAfterText) SetupAndStartSpaceship();
        canClick = true;

        // 3 깜빡임 시작
        blinking = true;
        StartCoroutine(BlinkText());

        
    }

    IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        float t = 0f;
        cg.alpha = from;
        while (t < duration)
        {
            t += Time.deltaTime;
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
            float alpha = (Mathf.Sin(Time.time * blinkSpeed * Mathf.PI) + 1f) / 2f; // 0~1 반복
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
        moveStartTime = Time.time;
        moveShip = true;
    }
}
