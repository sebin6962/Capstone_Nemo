using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class NextDayCutscene : MonoBehaviour
{
    [Header("컷신 패널(CanvasGroup)")]
    public CanvasGroup panel;          // 초기엔 panel.gameObject.SetActive(false), alpha=0

    [Header("타이밍(초)")]
    public float fadeIn = 0.5f;
    public float hold = 3.0f;
    public float fadeOut = 0.5f;

    [Header("끝나고 패널 비활성화")]
    public bool deactivateAfter = true;

    [Header("끝난 뒤 실행(원래 씬 전환 메서드 연결)")]
    public UnityEvent onFinished;

    public void Play()
    {
        StopAllCoroutines();
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        if (panel == null) yield break;

        panel.gameObject.SetActive(true);
        panel.interactable = false;
        panel.blocksRaycasts = false;
        panel.alpha = 0f;

        // 페이드 인
        float t = 0f;
        while (t < fadeIn)
        {
            t += Time.deltaTime;
            panel.alpha = Mathf.Lerp(0f, 1f, t / fadeIn);
            yield return null;
        }
        panel.alpha = 1f;

        // 유지
        yield return new WaitForSeconds(hold);

        // 페이드 아웃
        t = 0f;
        while (t < fadeOut)
        {
            t += Time.deltaTime;
            panel.alpha = Mathf.Lerp(1f, 0f, t / fadeOut);
            yield return null;
        }
        panel.alpha = 0f;

        if (deactivateAfter)
            panel.gameObject.SetActive(false);

        // 씬 전환 메서드 호출
        onFinished?.Invoke();
    }
}
