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
            FadeManager.Instance.FadeToScene("SaveSelectScene");
        }
    }

    IEnumerator FlowSequence()
    {
        // 1? 로고 등장
        yield return new WaitForSeconds(delayBeforeLogo);
        yield return StartCoroutine(FadeCanvasGroup(logoUI, 0, 1, logoFadeDuration));

        if (gradientGroup != null)
            yield return StartCoroutine(FadeCanvasGroup(gradientGroup, 0, 1, gradientFadeSeconds));

        // 2? 텍스트 등장
        yield return new WaitForSeconds(delayBeforeText);
        clickTextUI.gameObject.SetActive(true);
        yield return StartCoroutine(FadeCanvasGroup(clickTextUI, 0, 1, textFadeDuration));

        // 3? 깜빡임 시작
        blinking = true;
        StartCoroutine(BlinkText());

        canClick = true;
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
}
