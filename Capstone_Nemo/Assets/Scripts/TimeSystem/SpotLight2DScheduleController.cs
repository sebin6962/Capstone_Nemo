using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class SpotLight2DScheduleController : MonoBehaviour
{
    [Serializable]
    public class SpotLightSchedule
    {
        [Header("관리 이름")]
        public string lightName;

        [Header("연결할 Spot Light 2D")]
        public Light2D spotLight;

        [Header("켜지는 게임 시간")]
        [Range(0, 26)] public int onHour = 18;
        [Range(0, 59)] public int onMinute = 0;

        [Header("꺼지는 시간 사용")]
        public bool useOffTime = true;

        [Header("꺼지는 게임 시간")]
        [Range(0, 26)] public int offHour = 26;
        [Range(0, 59)] public int offMinute = 0;

        [Header("켜졌을 때 밝기")]
        public float onIntensity = 1f;

        [Header("오브젝트 자체를 켜고 끌지 여부")]
        public bool useGameObjectActive = false;
    }

    [Header("시간에 따라 제어할 Spot Light 2D 목록")]
    public List<SpotLightSchedule> spotLights = new List<SpotLightSchedule>();

    [Header("TimeManager가 없을 때 조명 끄기")]
    public bool turnOffWhenTimeManagerMissing = true;

    private void Start()
    {
        ApplyLightState();
    }

    private void Update()
    {
        ApplyLightState();
    }

    private void ApplyLightState()
    {
        if (TimeManager.Instance == null)
        {
            if (turnOffWhenTimeManagerMissing)
                SetAllLights(false);

            return;
        }

        int currentTime = ToMinutes(TimeManager.Instance.hour, TimeManager.Instance.minute);

        foreach (var data in spotLights)
        {
            if (data == null || data.spotLight == null)
                continue;

            int onTime = ToMinutes(data.onHour, data.onMinute);
            bool shouldTurnOn;

            if (data.useOffTime)
            {
                int offTime = ToMinutes(data.offHour, data.offMinute);

                // 켜지는 시간과 꺼지는 시간이 같으면 꺼진 상태로 처리
                if (onTime == offTime)
                {
                    shouldTurnOn = false;
                }
                // 예: 18:00 ~ 26:00
                else if (onTime < offTime)
                {
                    shouldTurnOn = currentTime >= onTime && currentTime < offTime;
                }
                // 예: 23:00 ~ 02:00 처럼 날짜를 넘기는 경우
                else
                {
                    shouldTurnOn = currentTime >= onTime || currentTime < offTime;
                }
            }
            else
            {
                // 꺼지는 시간을 사용하지 않으면, 켜지는 시간 이후 계속 켜짐
                shouldTurnOn = currentTime >= onTime;
            }

            SetLight(data, shouldTurnOn);
        }
    }

    private void SetLight(SpotLightSchedule data, bool isOn)
    {
        if (data.spotLight == null)
            return;

        if (data.useGameObjectActive)
        {
            data.spotLight.gameObject.SetActive(isOn);
        }
        else
        {
            data.spotLight.enabled = isOn;
        }

        if (isOn)
            data.spotLight.intensity = data.onIntensity;
    }

    private void SetAllLights(bool isOn)
    {
        foreach (var data in spotLights)
        {
            if (data == null || data.spotLight == null)
                continue;

            SetLight(data, isOn);
        }
    }

    private int ToMinutes(int hour, int minute)
    {
        return hour * 60 + minute;
    }
}