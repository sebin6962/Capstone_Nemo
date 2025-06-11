using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WarningUIManager : MonoBehaviour
{
    public static WarningUIManager Instance;

    public CanvasGroup warningGroup; // ← Image/Text가 포함된 패널에 CanvasGroup 추가
    public float fadeDuration = 0.5f;
    public float displayDuration = 2f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void ShowWarning()
    {
        StopAllCoroutines();
        StartCoroutine(ShowWarningRoutine());
    }

    private IEnumerator ShowWarningRoutine()
    {
        // 초기 상태
        warningGroup.gameObject.SetActive(true);
        yield return StartCoroutine(FadeCanvasGroup(warningGroup, 0f, 1f, fadeDuration));

        // 유지 시간
        yield return new WaitForSeconds(displayDuration);

        // 사라지기
        yield return StartCoroutine(FadeCanvasGroup(warningGroup, 1f, 0f, fadeDuration));
        warningGroup.gameObject.SetActive(false);
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        float elapsed = 0f;
        group.alpha = from;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        group.alpha = to;
    }
}
