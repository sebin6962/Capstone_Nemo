using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
public class DayNightController : MonoBehaviour
{
    public TimeManager timeManager;
    public Light2D globalLight;   // 반드시 Global Light 2D 연결

    [Header("Night Time Range")]
    public int nightStartHour = 18;
    public int fullNightHour = 24;

    [Header("Global Light")]
    [Range(0f, 2f)] public float dayIntensity = 1f;
    [Range(0f, 2f)] public float nightIntensity = 0.6f;

    [Header("Street Lamp Lights")]
    public Light2D[] streetLampLights;   // 가로등 Spot Light 2D들 넣기

    [Tooltip("이 값부터 가로등이 켜지기 시작함 (0~1, 밤 진행도 기준)")]
    [Range(0f, 1f)] public float lampFadeStart = 0.35f;

    [Tooltip("이 값에서 가로등 밝기가 최대가 됨 (0~1, 밤 진행도 기준)")]
    [Range(0f, 1f)] public float lampFadeEnd = 0.55f;

    [Tooltip("true면 특정 시점에 바로 켜짐 / false면 서서히 켜짐")]
    public bool instantOn = false;

    private float[] lampBaseIntensities;

    [Header("Global Light Color")]
    public Color dayLightColor = Color.white;
    public Color nightLightColor = new Color32(0x38, 0x3D, 0x57, 0xFF);

    [Header("Window Sky Gradient Sprites")]
    public List<SpriteRenderer> windowSkyRenderers = new List<SpriteRenderer>();

    [Tooltip("창문 하늘에서 위쪽 색이 차지하는 비율")]
    [Range(0f, 1f)] public float windowSkyTopRatio = 0.7f;

    [Header("Window Sky Day Gradient")]
    public Color daySkyTopColor = new Color32(0x68, 0xC8, 0xFF, 0xFF);
    public Color daySkyBottomColor = new Color32(0xFF, 0xD6, 0x9A, 0xFF);

    [Header("Window Sky Night Gradient")]
    public Color nightSkyTopColor = new Color32(0x12, 0x18, 0x35, 0xFF);
    public Color nightSkyBottomColor = new Color32(0x3A, 0x2C, 0x5A, 0xFF);

    private MaterialPropertyBlock windowSkyPropertyBlock;

    void Start()
    {
        if (timeManager == null)
            timeManager = TimeManager.Instance ?? FindObjectOfType<TimeManager>();

        windowSkyPropertyBlock = new MaterialPropertyBlock();

        CacheLampBaseIntensities();
        UpdateLighting();
    }

    void Update()
    {
        if (timeManager == null)
            timeManager = TimeManager.Instance ?? FindObjectOfType<TimeManager>();

        if (timeManager == null || globalLight == null)
            return;

        if (!timeManager.isTimeFlow)
            return;

        UpdateLighting();
    }

    void CacheLampBaseIntensities()
    {
        if (streetLampLights == null)
        {
            lampBaseIntensities = new float[0];
            return;
        }

        lampBaseIntensities = new float[streetLampLights.Length];

        for (int i = 0; i < streetLampLights.Length; i++)
        {
            if (streetLampLights[i] == null) continue;
            lampBaseIntensities[i] = streetLampLights[i].intensity;
        }
    }

    void UpdateLighting()
    {
        int minutesPassed = (timeManager.hour - 9) * 60 + timeManager.minute;
        int startMinutes = (nightStartHour - 9) * 60;
        int fullNightMinutes = (fullNightHour - 9) * 60;

        float nightT = Mathf.Clamp01(
            Mathf.InverseLerp(startMinutes, fullNightMinutes, minutesPassed)
        );

        globalLight.intensity = Mathf.Lerp(dayIntensity, nightIntensity, nightT);

        UpdateStreetLamps(nightT);

        globalLight.color = Color.Lerp(dayLightColor, nightLightColor, nightT);

        UpdateWindowSkyGradients(nightT);
    }

    void UpdateWindowSkyGradients(float nightT)
    {
        if (windowSkyRenderers == null)
            return;

        if (windowSkyPropertyBlock == null)
            windowSkyPropertyBlock = new MaterialPropertyBlock();

        Color currentTopColor = Color.Lerp(daySkyTopColor, nightSkyTopColor, nightT);
        Color currentBottomColor = Color.Lerp(daySkyBottomColor, nightSkyBottomColor, nightT);

        for (int i = 0; i < windowSkyRenderers.Count; i++)
        {
            SpriteRenderer skyRenderer = windowSkyRenderers[i];

            if (skyRenderer == null)
                continue;

            skyRenderer.GetPropertyBlock(windowSkyPropertyBlock);

            windowSkyPropertyBlock.SetColor("_TopColor", currentTopColor);
            windowSkyPropertyBlock.SetColor("_BottomColor", currentBottomColor);
            windowSkyPropertyBlock.SetFloat("_TopRatio", windowSkyTopRatio);

            skyRenderer.SetPropertyBlock(windowSkyPropertyBlock);
        }
    }


    void UpdateStreetLamps(float nightT)
    {
        if (streetLampLights == null || lampBaseIntensities == null)
            return;

        float lampT;

        if (instantOn)
            lampT = (nightT >= lampFadeStart) ? 1f : 0f;
        else
            lampT = Mathf.Clamp01(Mathf.InverseLerp(lampFadeStart, lampFadeEnd, nightT));

        for (int i = 0; i < streetLampLights.Length; i++)
        {
            Light2D lamp = streetLampLights[i];
            if (lamp == null) continue;

            float baseIntensity = lampBaseIntensities[i];

            lamp.enabled = lampT > 0.001f;
            lamp.intensity = baseIntensity * lampT;
        }
    }
}
