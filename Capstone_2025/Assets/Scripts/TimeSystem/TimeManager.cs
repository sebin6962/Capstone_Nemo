using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    public int hour = 9;
    public int minute = 0;
    public float realSecondsPerGameMinute = 0.25f;
    private float timer = 0f;

    public TMP_Text dayText;               // "1일차"
    public Image clockProgressImage;       // 원형 이미지

    public int currentDay = 1;             // 일차
    private int totalGameMinutes = (26 - 9) * 60; // 하루 총 분(9시 ~ 26시 → 1020분)

    private string savePath;

    public bool isTimeFlow = true; // 시간 흐름 제어 변수

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            savePath = Path.Combine(Application.persistentDataPath, "dayData.json");
            LoadDay();
        }
        else
        {
            Destroy(gameObject); // 혹시라도 중복 방지
        }
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        dayText = GameObject.Find("DayText")?.GetComponent<TMP_Text>();
        clockProgressImage = GameObject.Find("ClockProgress")?.GetComponent<Image>();
        UpdateDayUI();
        UpdateClockProgressUI();
    }
    void Start()
    {
        // 동적으로 씬에서 오브젝트를 찾아 연결
        if (dayText == null)
            dayText = GameObject.Find("DayText")?.GetComponent<TMP_Text>();

        if (clockProgressImage == null)
            clockProgressImage = GameObject.Find("ClockProgress")?.GetComponent<Image>();

        UpdateDayUI();
        UpdateClockProgressUI();
    }

    void Update()
    {
        // 명세서 씬(StatementScene)에서는 시간 진행 X
        if (SceneManager.GetActiveScene().name == "StatementScene")
            return;

        timer += Time.deltaTime;
        if (timer >= realSecondsPerGameMinute)
        {
            timer = 0f;
            minute += 1;
            if (minute >= 60)
            {
                minute = 0;
                hour += 1;

                if (hour >= 26)
                {
                    StartCoroutine(EndOfDayRoutine());
                }
            }
            UpdateClockProgressUI();
        }
    }

    void LoadDay()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            DayData data = JsonUtility.FromJson<DayData>(json);
            currentDay = data.day;
            hour = data.hour;
            minute = data.minute;
            Debug.Log($"[LoadDay] 파일에서 불러옴: {currentDay}일차 {hour}:{minute} ({savePath})");
        }
        else
        {
            currentDay = 1;
            hour = 9;
            minute = 0;
            Debug.Log("[LoadDay] 파일 없음. 1일차로 리셋");
        }
    }

    void UpdateClockProgressUI()
    {
        if (clockProgressImage == null) return;
        int minutesPassed = (hour - 9) * 60 + minute;
        float progress = Mathf.Clamp01((float)minutesPassed / totalGameMinutes);
        clockProgressImage.fillAmount = progress;
    }

    void UpdateDayUI()
    {
        if (dayText == null) return;
        dayText.text = $"{currentDay}일차";
    }

    IEnumerator EndOfDayRoutine()
    {
        currentDay++;           // 날짜 먼저 증가!
        hour = 9;           // 날짜 넘길 때 시간 초기화!
        minute = 0;
        SaveDayData();          // 증가한 날짜 저장!

        yield return new WaitForSeconds(1f);

        if (FadeManager.Instance != null)
            FadeManager.Instance.FadeToScene("StatementScene");
        else
            SceneManager.LoadScene("StatementScene");
    }

    public void SaveDayData()
    {
        DayData data = new DayData
        {
            day = currentDay,
            hour = hour,
            minute = minute
        };
        File.WriteAllText(savePath, JsonUtility.ToJson(data));
        Debug.Log($"[SaveDayData] {currentDay}일차 {hour}:{minute} 저장 ({savePath})");
    }

    void OnApplicationQuit()
    {
        SaveDayData();
    }

    void OnDisable()
    {
        if (this == Instance)
            SaveDayData();
    }

    // 외부에서 시간 흐름 On/Off
    public void SetTimeFlow(bool flow)
    {
        isTimeFlow = flow;
    }
}

[System.Serializable]
public class DayData
{
    public int day;
    public int hour;
    public int minute;
}
