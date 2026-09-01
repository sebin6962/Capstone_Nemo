using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

public class DayNightController : MonoBehaviour
{
    public TimeManager timeManager;
    public Light2D globalLight;

    [Header("Night Time Range")]
    public int nightStartHour = 18;
    public int fullNightHour = 24;

    [Header("Global Light")]
    [Range(0f, 2f)]
    public float dayIntensity = 1f;

    [Range(0f, 2f)]
    public float nightIntensity = 0.6f;

    [Header("Street Lamp Lights")]
    public Light2D[] streetLampLights;

    [Tooltip("이 값부터 가로등이 켜지기 시작함")]
    [Range(0f, 1f)]
    public float lampFadeStart = 0.35f;

    [Tooltip("이 값에서 가로등 밝기가 최대가 됨")]
    [Range(0f, 1f)]
    public float lampFadeEnd = 0.55f;

    [Tooltip("true면 바로 켜짐 / false면 서서히 켜짐")]
    public bool instantOn = false;

    private float[] lampBaseIntensities;

    [Header("Normal Global Light Color")]
    public Color dayLightColor = Color.white;

    public Color nightLightColor =
        new Color32(0x38, 0x3D, 0x57, 0xFF);

    [Header("Star Rain Global Light Color")]
    [Tooltip("별빛 비가 내리는 날의 낮 조명 색")]
    public Color starRainDayLightColor =
        new Color32(0xB8, 0xC8, 0xE8, 0xFF);

    [Tooltip("별빛 비가 내리는 날의 밤 조명 색")]
    public Color starRainNightLightColor =
        new Color32(0x27, 0x2C, 0x4A, 0xFF);

    [Header("Window Sky Gradient Sprites")]
    public List<SpriteRenderer> windowSkyRenderers =
        new List<SpriteRenderer>();

    [Tooltip("창문 하늘에서 위쪽 색이 차지하는 비율")]
    [Range(0f, 1f)]
    public float windowSkyTopRatio = 0.7f;

    [Header("Window Sky Day Gradient")]
    public Color daySkyTopColor =
        new Color32(0x68, 0xC8, 0xFF, 0xFF);

    public Color daySkyBottomColor =
        new Color32(0xFF, 0xD6, 0x9A, 0xFF);

    [Header("Window Sky Night Gradient")]
    public Color nightSkyTopColor =
        new Color32(0x12, 0x18, 0x35, 0xFF);

    public Color nightSkyBottomColor =
        new Color32(0x3A, 0x2C, 0x5A, 0xFF);

    [Header("Star Rain Window Sky Gradient")]
    [Tooltip("별빛 비가 내리는 날의 낮 하늘 위쪽 색")]
    public Color starRainDaySkyTopColor =
        new Color32(0x72, 0x84, 0xB5, 0xFF);

    [Tooltip("별빛 비가 내리는 날의 낮 하늘 아래쪽 색")]
    public Color starRainDaySkyBottomColor =
        new Color32(0xC3, 0xAE, 0xD2, 0xFF);

    [Tooltip("별빛 비가 내리는 날의 밤 하늘 위쪽 색")]
    public Color starRainNightSkyTopColor =
        new Color32(0x0C, 0x12, 0x30, 0xFF);

    [Tooltip("별빛 비가 내리는 날의 밤 하늘 아래쪽 색")]
    public Color starRainNightSkyBottomColor =
        new Color32(0x36, 0x2A, 0x62, 0xFF);

    private MaterialPropertyBlock windowSkyPropertyBlock;

    void Start()
    {
        FindTimeManager();

        windowSkyPropertyBlock =
            new MaterialPropertyBlock();

        CacheLampBaseIntensities();
        UpdateLighting();
    }

    void Update()
    {
        if (timeManager == null)
            FindTimeManager();

        if (timeManager == null || globalLight == null)
            return;

        if (!timeManager.isTimeFlow)
            return;

        UpdateLighting();
    }

    void FindTimeManager()
    {
        timeManager =
            TimeManager.Instance ??
            FindObjectOfType<TimeManager>();
    }

    bool IsStarRainDay()
    {
        return VillageSceneManager.Instance != null &&
               VillageSceneManager.Instance.IsStarRainDay;
    }

    void CacheLampBaseIntensities()
    {
        if (streetLampLights == null)
        {
            lampBaseIntensities = new float[0];
            return;
        }

        lampBaseIntensities =
            new float[streetLampLights.Length];

        for (int i = 0;
             i < streetLampLights.Length;
             i++)
        {
            if (streetLampLights[i] == null)
                continue;

            lampBaseIntensities[i] =
                streetLampLights[i].intensity;
        }
    }

    void UpdateLighting()
    {
        if (timeManager == null || globalLight == null)
            return;

        int minutesPassed =
            (timeManager.hour - 9) * 60 +
            timeManager.minute;

        int startMinutes =
            (nightStartHour - 9) * 60;

        int fullNightMinutes =
            (fullNightHour - 9) * 60;

        float nightT = Mathf.Clamp01(
            Mathf.InverseLerp(
                startMinutes,
                fullNightMinutes,
                minutesPassed
            )
        );

        bool starRain = IsStarRainDay();

        globalLight.intensity =
            Mathf.Lerp(
                dayIntensity,
                nightIntensity,
                nightT
            );

        Color targetDayLightColor =
            starRain
                ? starRainDayLightColor
                : dayLightColor;

        Color targetNightLightColor =
            starRain
                ? starRainNightLightColor
                : nightLightColor;

        globalLight.color =
            Color.Lerp(
                targetDayLightColor,
                targetNightLightColor,
                nightT
            );

        UpdateStreetLamps(nightT);
        UpdateWindowSkyGradients(nightT, starRain);
    }

    void UpdateWindowSkyGradients(
        float nightT,
        bool starRain
    )
    {
        if (windowSkyRenderers == null)
            return;

        if (windowSkyPropertyBlock == null)
        {
            windowSkyPropertyBlock =
                new MaterialPropertyBlock();
        }

        Color targetDayTop =
            starRain
                ? starRainDaySkyTopColor
                : daySkyTopColor;

        Color targetDayBottom =
            starRain
                ? starRainDaySkyBottomColor
                : daySkyBottomColor;

        Color targetNightTop =
            starRain
                ? starRainNightSkyTopColor
                : nightSkyTopColor;

        Color targetNightBottom =
            starRain
                ? starRainNightSkyBottomColor
                : nightSkyBottomColor;

        Color currentTopColor =
            Color.Lerp(
                targetDayTop,
                targetNightTop,
                nightT
            );

        Color currentBottomColor =
            Color.Lerp(
                targetDayBottom,
                targetNightBottom,
                nightT
            );

        for (int i = 0;
             i < windowSkyRenderers.Count;
             i++)
        {
            SpriteRenderer skyRenderer =
                windowSkyRenderers[i];

            if (skyRenderer == null)
                continue;

            skyRenderer.GetPropertyBlock(
                windowSkyPropertyBlock
            );

            windowSkyPropertyBlock.SetColor(
                "_TopColor",
                currentTopColor
            );

            windowSkyPropertyBlock.SetColor(
                "_BottomColor",
                currentBottomColor
            );

            windowSkyPropertyBlock.SetFloat(
                "_TopRatio",
                windowSkyTopRatio
            );

            skyRenderer.SetPropertyBlock(
                windowSkyPropertyBlock
            );
        }
    }

    void UpdateStreetLamps(float nightT)
    {
        if (streetLampLights == null ||
            lampBaseIntensities == null)
        {
            return;
        }

        float lampT;

        if (instantOn)
        {
            lampT =
                nightT >= lampFadeStart
                    ? 1f
                    : 0f;
        }
        else
        {
            lampT = Mathf.Clamp01(
                Mathf.InverseLerp(
                    lampFadeStart,
                    lampFadeEnd,
                    nightT
                )
            );
        }

        for (int i = 0;
             i < streetLampLights.Length;
             i++)
        {
            Light2D lamp =
                streetLampLights[i];

            if (lamp == null)
                continue;

            float baseIntensity =
                lampBaseIntensities[i];

            lamp.enabled = lampT > 0.001f;
            lamp.intensity =
                baseIntensity * lampT;
        }
    }
}