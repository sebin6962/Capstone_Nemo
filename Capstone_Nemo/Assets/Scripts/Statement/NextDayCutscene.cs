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

        // 컷 패널을 페이드 인 (0 → 1)
        yield return FadeCanvasGroup(cutCanvasGroup, 0f, 1f, fadeSeconds);

        // 컷 유지
        yield return new WaitForSecondsRealtime(holdSeconds);

        // 전역 씬 전환 실행
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




