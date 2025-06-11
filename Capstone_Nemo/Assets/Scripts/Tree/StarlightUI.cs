using UnityEngine;
using TMPro;
using System.Collections;

public class StarlightUI : MonoBehaviour
{
    public TMP_Text starlightText;

    private int displayedStarlight = 0;           // 화면에 보여지는 별빛 값
    private Coroutine animateRoutine = null;      // 애니메이션 중복 방지

    void Start()
    {
        if (StarDataManager.Instance != null && StarDataManager.Instance.playerData != null && starlightText != null)
        {
            displayedStarlight = StarDataManager.Instance.playerData.starlight;
            starlightText.text = displayedStarlight.ToString("N0");
        }
    }

    void OnEnable()
    {
        if (StarDataManager.Instance != null && StarDataManager.Instance.playerData != null && starlightText != null)
        {
            displayedStarlight = StarDataManager.Instance.playerData.starlight;
            starlightText.text = displayedStarlight.ToString("N0");
        }
    }


    public void UpdateStarlightUI()
    {
        if (StarDataManager.Instance == null || StarDataManager.Instance.playerData == null || starlightText == null)
            return;

        int targetStarlight = StarDataManager.Instance.playerData.starlight;
        if (animateRoutine != null)
            StopCoroutine(animateRoutine);

        animateRoutine = StartCoroutine(AnimateStarlight(displayedStarlight, targetStarlight));
    }

    IEnumerator AnimateStarlight(int from, int to)
    {
        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            int value = (int)Mathf.Lerp(from, to, t);
            if (starlightText != null)
                starlightText.text = value.ToString("N0");
            yield return null;
        }

        displayedStarlight = to;
        if (starlightText != null)
            starlightText.text = displayedStarlight.ToString("N0");
        animateRoutine = null;
    }
}
