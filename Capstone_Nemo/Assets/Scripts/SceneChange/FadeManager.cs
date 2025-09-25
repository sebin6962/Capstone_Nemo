
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class FadeManager : MonoBehaviour
{
    public static FadeManager Instance;

    [Header("UI 이미지 (검은 패널)")]
    public Image fadeImage;

    [Header("페이드 시간 (초)")]
    public float fadeDuration = 1f;

    private bool isFading = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject); // 중복 방지
        }
    }

    /// <summary>
    /// 외부에서 호출하여 씬을 페이드 전환
    /// </summary>
    public void FadeToScene(string sceneName, float delay = 0f)
    {
        if (isFading) return;
        StartCoroutine(FadeAndSwitchScenes(sceneName, delay));
    }

    /// <summary>
    /// 페이드 → 씬 전환 → 페이드 인 코루틴
    /// </summary>
    private IEnumerator FadeAndSwitchScenes(string sceneName, float delay)
    {
        isFading = true;

        yield return StartCoroutine(FadeOut());

        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        TimeManager.Instance?.SaveDayData();
        SceneManager.LoadScene(sceneName);
        yield return null; // 씬 완전히 로드될 때까지 1프레임 대기

        yield return StartCoroutine(FadeIn());

        isFading = false;
    }

    /// <summary>
    /// 화면 어둡게 (페이드 아웃)
    /// </summary>
    public IEnumerator FadeOut()
    {
        fadeImage.gameObject.SetActive(true);
        fadeImage.color = new Color(0, 0, 0, 0);
        float t = 0;
        Color color = fadeImage.color;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            color.a = Mathf.Lerp(0, 1, t / fadeDuration);
            fadeImage.color = color;
            yield return null;
        }

        color.a = 1f;
        fadeImage.color = color;
    }

    /// <summary>
    /// 화면 밝게 (페이드 인)
    /// </summary>
    public IEnumerator FadeIn()
    {
        fadeImage.color = new Color(0, 0, 0, 1);
        float t = 0;
        Color color = fadeImage.color;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            color.a = Mathf.Lerp(1, 0, t / fadeDuration);
            fadeImage.color = color;
            yield return null;
        }

        color.a = 0f;
        fadeImage.color = color;
        fadeImage.gameObject.SetActive(false); // 검은 화면 꺼주기
    }
}
