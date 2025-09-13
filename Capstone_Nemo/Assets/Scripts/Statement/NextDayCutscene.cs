using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class NextDayCutscene : MonoBehaviour
{
    [Header("Single cut panel")]
    [SerializeField] private GameObject cutPanel;     // 단 하나의 패널
    [Header("Fade")]
    [SerializeField] private Image fadeImage;         // 전체화면 검은 Image
    [SerializeField] private float fadeSeconds = 0.6f;
    [SerializeField] private float holdSeconds = 1.5f; // 컷 유지 시간
    [SerializeField] private bool fadeOutAtEnd = true; // 끝에 검게 닫을지
    [Header("Finish")]
    public UnityEvent onFinished;                     // 외부에서 다음 씬 전환 연결 가능
    [SerializeField] private string nextSceneName;    // 빈 값이면 씬 전환 안 함

    private void Awake()
    {
        if (cutPanel != null) cutPanel.SetActive(false);

        if (fadeImage != null)
        {
            // 시작은 투명 + 클릭 막지 않음 + 비활성
            var c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
            fadeImage.raycastTarget = false;
            fadeImage.gameObject.SetActive(false);
        }
    }

    public void Play()
    {
        StopAllCoroutines();
        StartCoroutine(RunSingleCut());
    }

    private IEnumerator RunSingleCut()
    {
        if (fadeImage == null || cutPanel == null)
        {
            Debug.LogWarning("[NextDayCutscene] 세팅 누락. 바로 종료.");
            Finish();
            yield break;
        }

        // 1) 검은 화면 켜고 알파 1(검정)에서 시작
        fadeImage.gameObject.SetActive(true);
        SetFadeAlpha(1f);

        // 2) 컷 패널 활성화
        cutPanel.SetActive(true);

        // 3) 검정 → 투명 페이드인 (컷 보이게)
        yield return Fade(1f, 0f, fadeSeconds);

        // 4) 컷 유지
        yield return new WaitForSeconds(holdSeconds);

        //// 5) 필요하면 투명 → 검정으로 닫고 컷 끄기
        //if (fadeOutAtEnd)
        //    yield return Fade(0f, 1f, fadeSeconds);

        //cutPanel.SetActive(false);

        // 6) 종료 처리
        Finish();
    }

    private void Finish()
    {
        // 다음 사용을 위해 페이드 숨김(다음 화면 가리면 안 됨)
        if (fadeImage != null)
        {
            SetFadeAlpha(0f);
            fadeImage.gameObject.SetActive(false);
        }

        onFinished?.Invoke();

        //if (!string.IsNullOrEmpty(nextSceneName))
        //{
        //    // FadeManager가 있다면 여기서 호출하도록 onFinished에 배선하면 됨
        //    SceneManager.LoadScene(nextSceneName);
        //}
    }

    private IEnumerator Fade(float from, float to, float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(from, to, t / seconds);
            SetFadeAlpha(a);
            yield return null;
        }
        SetFadeAlpha(to);
    }

    private void SetFadeAlpha(float a)
    {
        var c = fadeImage.color;
        c.a = a;
        fadeImage.color = c;
    }
}


