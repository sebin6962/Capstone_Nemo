using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class AuraFadeIn : MonoBehaviour
{
    [Header("광채 페이드 설정")]
    [SerializeField] private float fadeDelay = 0.1f;
    [SerializeField] private float fadeDuration = 1.2f;
    [SerializeField, Range(0f, 1f)] private float targetAlpha = 0.8f;

    private static readonly int AuraAlphaID =
        Shader.PropertyToID("_AuraAlpha");

    private Image auraImage;
    private Material auraMaterial;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        auraImage = GetComponent<Image>();

        if (auraImage == null)
        {
            Debug.LogError("[AuraFadeIn] Image 컴포넌트가 없습니다.", this);
            enabled = false;
            return;
        }

        if (auraImage.material == null)
        {
            Debug.LogError("[AuraFadeIn] 광채 Material이 연결되지 않았습니다.", this);
            enabled = false;
            return;
        }

        // 공유 머티리얼이 함께 변경되지 않도록 복사본 생성
        auraMaterial = new Material(auraImage.material);
        auraImage.material = auraMaterial;
    }

    private void OnEnable()
    {
        if (auraMaterial == null)
            return;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        // 패널이 켜진 첫 순간에는 광채가 보이지 않게 설정
        auraMaterial.SetFloat(AuraAlphaID, 0f);
        fadeCoroutine = StartCoroutine(FadeInRoutine());
    }

    private IEnumerator FadeInRoutine()
    {
        if (fadeDelay > 0f)
            yield return new WaitForSecondsRealtime(fadeDelay);

        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(elapsedTime / fadeDuration);

            // 처음과 끝이 부드럽게 변하도록 보간
            float smoothProgress =
                progress * progress * (3f - 2f * progress);

            float currentAlpha =
                Mathf.Lerp(0f, targetAlpha, smoothProgress);

            auraMaterial.SetFloat(AuraAlphaID, currentAlpha);

            yield return null;
        }

        auraMaterial.SetFloat(AuraAlphaID, targetAlpha);
        fadeCoroutine = null;
    }

    private void OnDisable()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        if (auraMaterial != null)
            auraMaterial.SetFloat(AuraAlphaID, 0f);
    }

    private void OnDestroy()
    {
        if (auraMaterial != null)
            Destroy(auraMaterial);
    }
}