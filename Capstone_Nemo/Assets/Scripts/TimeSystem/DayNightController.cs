using UnityEngine;
using UnityEngine.Rendering.Universal;

public class DayNightController : MonoBehaviour
{
    public TimeManager timeManager;
    public Light2D globalLight;   // 반드시 Global Light 2D 연결

    [Header("Night Time Range")]
    public int nightStartHour = 18;
    public int fullNightHour = 24;

    [Header("Global Light")]
    [Range(0f, 2f)] public float dayIntensity = 1f;
    [Range(0f, 2f)] public float nightIntensity = 0.22f;

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

    void Start()
    {
        if (timeManager == null)
            timeManager = TimeManager.Instance ?? FindObjectOfType<TimeManager>();

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
