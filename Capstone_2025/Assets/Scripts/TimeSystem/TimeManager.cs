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

    public TMP_Text dayText;               // "1����"
    public Image clockProgressImage;       // ���� �̹���

    public int currentDay = 1;             // ����
    private int totalGameMinutes = (26 - 9) * 60; // �Ϸ� �� ��(9�� ~ 26�� �� 1020��)

    private string savePath;

    public bool isTimeFlow = true; // �ð� �帧 ���� ����

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
            Destroy(gameObject); // Ȥ�ö� �ߺ� ����
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
        // �������� ������ ������Ʈ�� ã�� ����
        if (dayText == null)
            dayText = GameObject.Find("DayText")?.GetComponent<TMP_Text>();

        if (clockProgressImage == null)
            clockProgressImage = GameObject.Find("ClockProgress")?.GetComponent<Image>();

        UpdateDayUI();
        UpdateClockProgressUI();
    }

    void Update()
    {
        // ���� ��(StatementScene)������ �ð� ���� X
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
            Debug.Log($"[LoadDay] ���Ͽ��� �ҷ���: {currentDay}���� {hour}:{minute} ({savePath})");
        }
        else
        {
            currentDay = 1;
            hour = 9;
            minute = 0;
            Debug.Log("[LoadDay] ���� ����. 1������ ����");
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
        dayText.text = $"{currentDay}����";
    }

    IEnumerator EndOfDayRoutine()
    {
        currentDay++;           // ��¥ ���� ����!
        hour = 9;           // ��¥ �ѱ� �� �ð� �ʱ�ȭ!
        minute = 0;
        SaveDayData();          // ������ ��¥ ����!

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
        Debug.Log($"[SaveDayData] {currentDay}���� {hour}:{minute} ���� ({savePath})");
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

    // �ܺο��� �ð� �帧 On/Off
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
