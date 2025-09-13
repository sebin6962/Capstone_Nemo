using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class NextDayCutscene : MonoBehaviour
{
    [Header("단일 컷 패널")]
    [SerializeField] private GameObject cutPanel;        // 1개만 사용

    [Header("페이드용 검은 화면 (Canvas 최상단)")]
    [SerializeField] private Image fadeImage;            // 풀스크린 검정 Image

    [Header("타이밍")]
    [Tooltip("컷이 완전히 보이는 상태로 유지되는 시간(초)")]
    [SerializeField] private float holdSeconds = 3f;

    [Tooltip("페이드 인 시간(초)")]
    [SerializeField] private float fadeSeconds = 1.5f;

    [Header("시작 방식")]
    [Tooltip("씬 진입과 동시에 컷신을 재생할지 여부(보통은 버튼 클릭으로 Play 호출)")]
    [SerializeField] private bool playOnStart = false;

    private void Awake()
    {
        // 컷 패널은 처음엔 꺼둔다 (명세서 UI 먼저 보여야 하니까)
        if (cutPanel != null) cutPanel.SetActive(false);

        // 검은 화면: 처음엔 완전 투명 + 비활성 (UI 가리지 않도록)
        if (fadeImage != null)
        {
            var c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
            fadeImage.raycastTarget = false;
            fadeImage.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("[NextDayCutscene] fadeImage가 할당되지 않았습니다.");
        }
    }

    private void Start()
    {
        if (playOnStart) Play();
    }

    /// <summary>버튼 OnClick에서 호출</summary>
    public void Play()
    {
        StopAllCoroutines();
        StartCoroutine(PlaySingleCutAndTransition());
    }

    private IEnumerator PlaySingleCutAndTransition()
    {
        if (fadeImage == null || cutPanel == null)
        {
            Debug.LogWarning("[NextDayCutscene] 세팅 누락. 바로 전환합니다.");
            yield return null;
            yield return TransitionToVillage();
            yield break;
        }

        // 1) 검은 화면을 켜고 알파 1(검)로 시작
        fadeImage.gameObject.SetActive(true);
        SetFadeAlpha(1f);

        // 2) 단일 컷 패널 활성화
        cutPanel.SetActive(true);

        // 3) 검 → 투 페이드인 (컷 보이게)
        yield return Fade(1.5f, 0f, fadeSeconds);

        // 4) 컷 유지
        yield return new WaitForSecondsRealtime(holdSeconds);

        // ※ CutSceneManager와 동일하게 마지막에 페이드아웃(투→검) 없음
        //    => 화면에 컷이 그대로 보이는 상태에서 전역 페이드(씬 전환)가 덮음
        yield return TransitionToVillage();
    }

    private IEnumerator TransitionToVillage()
    {
        // === CutSceneManager의 전환 로직을 그대로 사용 ===
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

        // 다음 씬 전환을 위해 현재 페이드 이미지/컷은 건드리지 않음
        // (전역 FadeManager가 검→다음 씬→페이드인까지 처리)
        yield return null;
    }

    private IEnumerator Fade(float from, float to, float seconds)
    {
        float t = 0f;
        var color = fadeImage.color;
        color.a = from;
        fadeImage.color = color;

        while (t < seconds)
        {
            t += Time.unscaledDeltaTime; // timeScale=0에서도 부드럽게
            float a = Mathf.Lerp(from, to, Mathf.Clamp01(t / seconds));
            color.a = a;
            fadeImage.color = color;
            yield return null;
        }

        color.a = to;
        fadeImage.color = color;

        // 투명으로 끝나면 클릭 방해하지 않도록 꺼둔다(선택)
        if (to <= 0f) fadeImage.gameObject.SetActive(false);
    }

    private void SetFadeAlpha(float a)
    {
        var c = fadeImage.color;
        c.a = a;
        fadeImage.color = c;
    }
}



