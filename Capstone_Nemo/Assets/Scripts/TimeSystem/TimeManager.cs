using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Globalization;
using UnityEngine.SceneManagement;

[Serializable]
public class PlaytimeData
{
    public long seconds;          // 누적 플레이타임(초)
    public string lastPlayed;     // 마지막 접속 표시(로컬 시간 문자열)
}

public class TimeManager : MonoBehaviour
{
    public static event Action OnNewDayStarted;
    public static TimeManager Instance { get; private set; }

    public int hour = 9;
    public int minute = 0;
    public float realSecondsPerGameMinute = 0.25f;
    private float timer = 0f;

    public TMP_Text dayText;               // "1일차"
    //public Image clockProgressImage;       // 원형 이미지

    public Image clockHandImage;

    public int currentDay = 0;             // 일차
    private int totalGameMinutes = (26 - 9) * 60; // 하루 총 분(9시 ~ 26시 → 1020분)

    private string savePath;

    public bool isTimeFlow = true; // 시간 흐름 제어 변수

    private DateTime? _sessionStartUtc;
    private string _currentServerForPlay;
    private long _cachedPlaySeconds;

    private string PlaytimePath(string server)
        => Path.Combine(Application.persistentDataPath, $"playtime_{server}.json");


    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            // savePath는 반드시 서버명 세팅 후 지정!
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

    public void SetServerName(string serverName)
    {
        savePath = Path.Combine(Application.persistentDataPath, $"dayData_{serverName}.json");
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        dayText = GameObject.Find("DayText")?.GetComponent<TMP_Text>();
        //clockProgressImage = GameObject.Find("ClockProgress")?.GetComponent<Image>();
        clockHandImage = GameObject.Find("DayPanel_niddle")?.GetComponent<Image>();
        UpdateDayUI();
        UpdateClockProgressUI();

        var name = scene.name;
        bool shouldPause =
            name == "IntroScene" ||
            name == "SaveSelectScene" ||
            name == "StatementScene";
        isTimeFlow = !shouldPause;
    }
    void Start()
    {
        // 동적으로 씬에서 오브젝트를 찾아 연결
        if (dayText == null)
            dayText = GameObject.Find("DayText")?.GetComponent<TMP_Text>();

        if (clockHandImage == null)
            clockHandImage = GameObject.Find("DayPanel_niddle")?.GetComponent<Image>();

        UpdateDayUI();
        UpdateClockProgressUI();
    }

    void Update()
    {
        if (!isTimeFlow) return;

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

    public void LoadDay()
    {
        Debug.Log("[DayData] 실제 로드 경로: " + savePath);

        if (File.Exists(savePath))
        {
            Debug.Log("[DayData] 파일 있음");
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

        UpdateDayUI();
    }

    void UpdateClockProgressUI()
    {
        //if (clockProgressImage == null) return;
        //int minutesPassed = (hour - 9) * 60 + minute;
        //float progress = Mathf.Clamp01((float)minutesPassed / totalGameMinutes);
        //clockProgressImage.fillAmount = progress;

        if (clockHandImage == null) return;

        int minutesPassed = (hour - 9) * 60 + minute;
        float progress = Mathf.Clamp01((float)minutesPassed / totalGameMinutes);
        float angle = Mathf.Lerp(0, 360, progress);

        // 시계방향 회전(원하면 -angle)
        clockHandImage.rectTransform.localEulerAngles = new Vector3(0, 0, -angle);
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

        OnNewDayStarted?.Invoke();

        yield return new WaitForSeconds(1f);

        if (FadeManager.Instance != null)
            FadeManager.Instance.FadeToScene("StatementScene");
        else
            SceneManager.LoadScene("StatementScene");
    }

    public void SaveDayData()
    {
        if (string.IsNullOrEmpty(savePath))
        {
            Debug.LogWarning("[SaveDayData] 서버명 미지정 상태, 저장 skip!");
            return;
        }
        DayData data = new DayData
        {
            day = currentDay,
            hour = hour,
            minute = minute
        };
        File.WriteAllText(savePath, JsonUtility.ToJson(data));
        Debug.Log($"[SaveDayData] {currentDay}일차 {hour}:{minute} 저장 ({savePath})");
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded_PlaySession;
    }

    void OnDisable()
    {
        if (this == Instance)
            SaveDayData();
        SceneManager.sceneLoaded -= OnSceneLoaded_PlaySession;
    }

    // 외부에서 시간 흐름 On/Off
    public void SetTimeFlow(bool flow)
    {
        isTimeFlow = flow;
    }

    public void BeginSessionForSelectedSave()
    {
        var server = PlayerPrefs.GetString("SelectedSave", "");
        if (!string.IsNullOrEmpty(server)) BeginSession(server);
    }

    public void BeginSession(string serverName)
    {
        // 이전 세션 종료(혹시 열려있다면)
        EndAndPersistSession();

        _currentServerForPlay = serverName;
        _cachedPlaySeconds = 0;

        // 기존 누적 불러오기
        var path = PlaytimePath(serverName);
        if (File.Exists(path))
        {
            try
            {
                var data = JsonUtility.FromJson<PlaytimeData>(File.ReadAllText(path));
                if (data != null) _cachedPlaySeconds = data.seconds;
            }
            catch { /* 무시: 손상 시 0부터 */ }
        }
        _sessionStartUtc = DateTime.UtcNow;
    }

    public void EndAndPersistSession()
    {
        if (_sessionStartUtc == null || string.IsNullOrEmpty(_currentServerForPlay)) return;

        var elapsed = (long)Math.Max(0, (DateTime.UtcNow - _sessionStartUtc.Value).TotalSeconds);
        _cachedPlaySeconds += elapsed;

        var data = new PlaytimeData
        {
            seconds = _cachedPlaySeconds,
            lastPlayed = DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
        };
        try
        {
            File.WriteAllText(PlaytimePath(_currentServerForPlay), JsonUtility.ToJson(data, true));
        }
        catch { /* 디스크 에러 무시 */ }

        _sessionStartUtc = null;
    }

    private void OnSceneLoaded_PlaySession(Scene scene, LoadSceneMode mode)
    {
        var name = scene.name;

        // Intro/SaveSelect/Statement 씬에선 시간 멈춤 + 세션 종료
        bool nonPlayScene = name == "IntroScene" || name == "SaveSelectScene" || name == "StatementScene";
        isTimeFlow = !nonPlayScene;           // 시간 흐름 제어(이미 쓰고 있던 플래그)

        if (nonPlayScene)
        {
            EndAndPersistSession();           // 플레이 중이었다면 종료+저장
        }
        else
        {
            // 플레이 씬에 진입 → 현재 SelectedSave 기준으로 세션 시작
            BeginSessionForSelectedSave();
        }
    }

    void OnApplicationPause(bool pause)
    {
        if (pause) EndAndPersistSession();    // 일시정지 시 세션 저장
    }

    void OnApplicationQuit()
    {
        EndAndPersistSession();               // 종료 직전 세션 저장
        SaveDayData();
    }
}

[System.Serializable]
public class DayData
{
    public int day;
    public int hour;
    public int minute;
}
