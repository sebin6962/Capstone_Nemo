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

    [SerializeField] private Image gradientOverlay;          // 하단 그라디언트 이미지
    [SerializeField] private float gradientFadeSeconds = 0.3f;
    private CanvasGroup gradientGroup;

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
            gradientGroup = gradientOverlay.GetComponent<CanvasGroup>();
            if (gradientGroup == null)
                gradientGroup = gradientOverlay.gameObject.AddComponent<CanvasGroup>();

            gradientOverlay.gameObject.SetActive(false);
            gradientGroup.alpha = 0f;
            gradientOverlay.raycastTarget = false;
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
            gradientOverlay.gameObject.SetActive(true);
            gradientGroup.alpha = 0f;
            StartCoroutine(FadeCanvasGroup(gradientGroup, 0f, 1f, gradientFadeSeconds));
        }

        yield return FadeCanvasGroup(cutCanvasGroup, 0f, 1f, fadeSeconds);

        yield return new WaitForSecondsRealtime(holdSeconds);

        if (gradientOverlay != null && gradientGroup != null)
        {
            yield return FadeCanvasGroup(gradientGroup, gradientGroup.alpha, 0f, gradientFadeSeconds);
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
}




