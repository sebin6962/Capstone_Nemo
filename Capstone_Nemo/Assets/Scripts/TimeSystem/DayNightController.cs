using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DayNightController : MonoBehaviour
{
    public CanvasGroup nightOverlay;   
    public TimeManager timeManager;    

    // 몇 시부터 어두워질지 / 완전 밤인지
    public int nightStartHour = 18;
    public int nightEndHour = 24;

    void Awake()
    {
        nightOverlay = GetComponent<CanvasGroup>();
    }

    void Start()
    {
        timeManager = FindObjectOfType<TimeManager>();
    }

    void Update()
    {
        if (nightOverlay == null || timeManager == null)
            return;

        if (!timeManager.isTimeFlow)
            return;

        int minutesPassed = (timeManager.hour - 9) * 60 + timeManager.minute;
        int startMinutes = (nightStartHour - 9) * 60;
        int endMinutes = (nightEndHour - 9) * 60;


        float t = Mathf.InverseLerp(startMinutes, endMinutes, minutesPassed);
        nightOverlay.alpha = t;  
    }
}
