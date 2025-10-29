using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class SimpleYellowGlow : MonoBehaviour
{
    [Header("Color")]
    [SerializeField] Color glowColor = new Color(1f, 0.9f, 0.3f, 1f); // 노란색(알파는 아래 범위로 제어)

    [Header("Alpha Flicker")]
    [SerializeField, Range(0f, 1f)] float minAlpha = 0.45f;
    [SerializeField, Range(0f, 1f)] float maxAlpha = 0.7f;
    [SerializeField, Tooltip("초당 사이클 속도")] float frequency = 0.5f;

    [Header("Desync")]
    [SerializeField, Tooltip("오브젝트마다 위상 랜덤")] bool desync = true;

    [Header("Smoothing")]
    [SerializeField, Tooltip("프레임 간 보간(0=즉시, 1=아주 느림)")]
    [Range(0f, 1f)] float lerpFactor = 0.2f;

    SpriteRenderer sr;
    float phase;
    float currentAlpha;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (desync) phase = Random.value * Mathf.PI * 2f;

        // 초기 알파를 중간값으로
        float mid = (minAlpha + maxAlpha) * 0.5f;
        currentAlpha = Mathf.Clamp01(mid);

        // 초기 색 적용
        var c = glowColor;
        c.a = currentAlpha;
        sr.color = c;
    }

    void Update()
    {
        float t = Time.time * frequency + phase;

        // 0~1 사인 파형
        float s = Mathf.Sin(t) * 0.5f + 0.5f;

        // 목표 알파 (자연스럽게: 중간값 기준으로 살짝만 흔들리게 범위를 좁게 설정해두는 걸 추천)
        float targetAlpha = Mathf.Lerp(minAlpha, maxAlpha, s);

        // 부드럽게 보간 (프레임 독립적 보간)
        float k = 1f - Mathf.Pow(1f - lerpFactor, Time.deltaTime * 60f);
        currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, k);

        // 적용
        var c = glowColor;
        c.a = currentAlpha;
        sr.color = c;
    }
}

