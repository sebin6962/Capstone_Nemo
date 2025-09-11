using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CutSceneManager : MonoBehaviour
{
    [Header("컷 패널 (순서대로 3개)")]
    public List<GameObject> cutPanels = new List<GameObject>(); // 0,1,2 순서로 할당

    [Header("페이드용 검은 화면 Image (Canvas 최상단)")]
    public Image fadeImage; // 풀스크린 검정 이미지

    [Header("타이밍 설정")]
    [Tooltip("각 패널이 완전히 보이는 상태로 유지되는 시간(초)")]
    public float panelHoldSeconds = 3f;

    [Tooltip("페이드 인/아웃 시간(초) - 자연스럽고 천천히")]
    public float fadeSeconds = 1.5f;

    [Header("(이전 코드 호환) 컷신 총 길이 변수")]
    [Tooltip("원본 변수. 연출은 패널/페이드 시간 기반으로 진행되며, 전환 로직은 그대로 유지됩니다.")]
    public float cutSceneDuration = 3f; // 원본 필드 유지 (정보 표기용)

    private void Awake()
    {
        // 모든 컷을 비활성화
        foreach (var p in cutPanels)
        {
            if (p != null) p.SetActive(false);
        }

        // 검은 화면은 시작 시 완전히 켜둠(Alpha=1)
        if (fadeImage != null)
        {
            var c = fadeImage.color;
            c.a = 1f;
            fadeImage.color = c;
            fadeImage.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[CutSceneManager] fadeImage가 할당되지 않았습니다.");
        }
    }

    private void Start()
    {
        StartCoroutine(PlayCutAndTransition());
    }

    private IEnumerator PlayCutAndTransition()
    {
        // 안전장치
        if (fadeImage == null || cutPanels == null || cutPanels.Count == 0)
        {
            Debug.LogWarning("[CutSceneManager] 컷 패널 또는 페이드 이미지가 비어 있어 바로 전환합니다.");
            yield return null;
            yield return TransitionToVillage();
            yield break;
        }

        for (int i = 0; i < cutPanels.Count; i++)
        {
            // 현재 컷 활성화
            cutPanels[i].SetActive(true);

            // [검은 화면] -> 현재 컷 페이드 인
            yield return Fade(1f, 0f, fadeSeconds);

            // 컷 유지
            yield return new WaitForSeconds(panelHoldSeconds);

            // 마지막 컷이면: 페이드아웃/비활성화 없이 바로 전환
            if (i == cutPanels.Count - 1)
            {
                // 화면엔 마지막 컷이 그대로 보이는 상태에서,
                // FadeManager가 씬 전환 페이드를 담당
                yield return TransitionToVillage();
                yield break;
            }

            // 마지막이 아니면: 다음 컷을 위해 검은 화면으로 페이드 아웃
            yield return Fade(0f, 1f, fadeSeconds);

            // 현재 컷 비활성화
            cutPanels[i].SetActive(false);
        }
    }

    private IEnumerator TransitionToVillage()
    {
        // === 원본 코드 유지 구간 시작 ===
        // VillageSceneManager 인스턴스 정리
        if (VillageSceneManager.Instance != null)
        {
            Destroy(VillageSceneManager.Instance.gameObject);
            VillageSceneManager.Instance = null; // 원본과 동일
        }

        // 필요 시 ResetData (원본에 있던 조건 그대로)
        if (VillageSceneManager.Instance != null)
        {
            VillageSceneManager.Instance.ResetData();
        }

        // VillageScene으로 페이드 전환 (원본과 동일)
        SceneTransitionInfo.Instance.entranceID = "FromPlayerStore"; // :contentReference[oaicite:1]{index=1}
        FadeManager.Instance.FadeToScene("VillageScene");            // :contentReference[oaicite:2]{index=2}
        PlayerPrefs.SetInt("StartTimeOnEnter", 1);                   // :contentReference[oaicite:3]{index=3}
        // === 원본 코드 유지 구간 끝 ===

        yield return null;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float t = 0f;
        var color = fadeImage.color;

        // 시작값 보정
        color.a = from;
        fadeImage.color = color;

        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(from, to, t / duration);
            color.a = a;
            fadeImage.color = color;
            yield return null;
        }

        // 최종값 스냅
        color.a = to;
        fadeImage.color = color;
    }
}


