using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class NextDayCutscene : MonoBehaviour
{
    [Header("컷 패널 (1개)")]
    [SerializeField] private GameObject cutPanel; // 여기에 CanvasGroup 꼭 붙여주세요

    [Header("페이드용 검은 화면 (전역 씬 전환)")]
    [SerializeField] private Image fadeImage;

    [Header("타이밍")]
    [SerializeField] private float holdSeconds = 3f;   // 컷 유지 시간
    [SerializeField] private float fadeSeconds = 1.5f; // 컷 페이드 인 시간

    [Header("시작 방식")]
    [SerializeField] private bool playOnStart = false;

    [Header("Subtitle Gradient")]
    [SerializeField] private Image gradientOverlay;          // 하단 검은 그라디언트 이미지
    [SerializeField] private float gradientHeight = 320f;
    [Range(0, 1f)][SerializeField] private float bottomAlpha = 0.65f;
    [Range(0, 1f)][SerializeField] private float topAlpha = 0f;
    [SerializeField] private float gradientFadeSeconds = 0.3f;
    [SerializeField] private float overlayFadeOutSeconds = 0.35f;

    [SerializeField] private GameObject subtitleTextObject;   // 그라디언트 위에 띄울 텍스트
    [SerializeField] private float textShowDelayAfterGradient = 0.1f;

    [SerializeField] private float textFadeInSeconds = 0.4f;
    [SerializeField] private float textFadeOutSeconds = 0.3f;

    private CanvasGroup gradientGroup;
    private CanvasGroup subtitleTextGroup;
    private CanvasGroup cutCanvasGroup;

    private void Awake()
    {
        if (cutPanel != null)
        {
            cutCanvasGroup = cutPanel.GetComponent<CanvasGroup>();
            if (cutCanvasGroup == null)
                cutCanvasGroup = cutPanel.AddComponent<CanvasGroup>();

            cutPanel.SetActive(false);
            cutCanvasGroup.alpha = 0f;
        }

        if (fadeImage != null)
        {
            var c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
            fadeImage.raycastTarget = false;
            fadeImage.gameObject.SetActive(false);
        }

        if (gradientOverlay != null)
        {
            var grt = gradientOverlay.rectTransform;
            grt.anchorMin = new Vector2(0f, 0f);
            grt.anchorMax = new Vector2(1f, 0f);
            grt.pivot = new Vector2(0.5f, 0f);
            grt.sizeDelta = new Vector2(grt.sizeDelta.x, gradientHeight);

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
                    new Color(0f, 0f, 0f, topAlpha),      // 위쪽(투명)
                    new Color(0f, 0f, 0f, bottomAlpha)    // 아래쪽(진한 검정)
                );
                gradientOverlay.type = Image.Type.Simple;
            }

            if (subtitleTextObject != null)
            {
                subtitleTextGroup = subtitleTextObject.GetComponent<CanvasGroup>();
                if (subtitleTextGroup == null)
                    subtitleTextGroup = subtitleTextObject.AddComponent<CanvasGroup>();

                subtitleTextGroup.alpha = 0f;
                subtitleTextObject.SetActive(false);
            }
        }
    }

    private void Start()
    {
        if (playOnStart) Play();
    }

    public void Play()
    {
        StopAllCoroutines();
        StartCoroutine(PlaySingleCutAndTransition());
    }

    private IEnumerator PlaySingleCutAndTransition()
    {
        if (cutPanel == null || cutCanvasGroup == null)
        {
            Debug.LogWarning("[NextDayCutscene] 컷 패널 세팅 누락");
            yield break;
        }

        // 컷 패널 켜고 투명 상태에서 시작
        cutPanel.SetActive(true);
        cutCanvasGroup.alpha = 0f;

        if (gradientOverlay != null && gradientGroup != null)
        {
            gradientOverlay.gameObject.SetActive(false);
            gradientGroup.alpha = 0f;
        }

        yield return FadeCanvasGroup(cutCanvasGroup, 0f, 1f, fadeSeconds);

        if (gradientOverlay != null && gradientGroup != null)
        {
            yield return new WaitForSeconds(0.2f);                     // 컷신 후 약간의 텀
            gradientOverlay.gameObject.SetActive(true);
            gradientGroup.alpha = 0f;

            yield return FadeCanvasGroup(gradientGroup, 0f, 1f, gradientFadeSeconds);

            if (subtitleTextObject != null)
            {
                if (textShowDelayAfterGradient > 0f)
                    yield return new WaitForSeconds(textShowDelayAfterGradient);

                subtitleTextObject.SetActive(true);
                subtitleTextGroup.alpha = 0f;

                // 서서히 0 → 1로
                yield return FadeCanvasGroup(subtitleTextGroup, 0f, 1f, textFadeInSeconds);
            }
        }

        yield return new WaitForSecondsRealtime(holdSeconds);

        if (subtitleTextObject != null && subtitleTextGroup != null)
        {
            yield return FadeCanvasGroup(subtitleTextGroup, subtitleTextGroup.alpha, 0f, textFadeOutSeconds);
            subtitleTextObject.SetActive(false);
        }

        if (gradientOverlay != null && gradientGroup != null)
        {
            yield return FadeCanvasGroup(gradientGroup, gradientGroup.alpha, 0f, overlayFadeOutSeconds);
            gradientOverlay.gameObject.SetActive(false);
        }

        yield return TransitionToVillage();
    }

    private IEnumerator TransitionToVillage()
    {
        if (VillageSceneManager.Instance != null)
        {
            Destroy(VillageSceneManager.Instance.gameObject);
            VillageSceneManager.Instance = null;
        }

        if (VillageSceneManager.Instance != null)
        {
            VillageSceneManager.Instance.ResetData();
        }

        SceneTransitionInfo.Instance.entranceID = "FromPlayerStore";
        FadeManager.Instance.FadeToScene("VillageScene");
        PlayerPrefs.SetInt("StartTimeOnEnter", 1);

        yield return null;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float seconds)
    {
        float t = 0f;
        cg.alpha = from;

        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / seconds));
            yield return null;
        }

        cg.alpha = to;
    }

    private Sprite MakeVerticalGradientSprite(int width, int height, Color top, Color bottom)
    {
        var tex = new Texture2D(width, height, TextureFormat.ARGB32, false);
        for (int y = 0; y < height; y++)
        {
            float t = (float)y / Mathf.Max(1, height - 1);
            Color c = Color.Lerp(bottom, top, t); // y=0(아래)=bottom, y=height-1(위)=top
            for (int x = 0; x < width; x++)
                tex.SetPixel(x, y, c);
        }
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.Apply();

        return Sprite.Create(
            tex,
            new Rect(0, 0, width, height),
            new Vector2(0.5f, 0.5f),
            100f
        );
    }
}




