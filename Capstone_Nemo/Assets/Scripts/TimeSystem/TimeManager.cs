using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Globalization;
using UnityEngine.SceneManagement;

public class TimeManager : MonoBehaviour
{
    public static event Action OnNewDayStarted;
    public static TimeManager Instance { get; private set; }

    public int hour = 9;
    public int minute = 0;
    public float realSecondsPerGameMinute = 0.9f;
    private float timer = 0f;

    public TMP_Text dayText;               // "1일차"
    //public Image clockProgressImage;       // 원형 이미지

    public Image clockHandImage;

    public int currentDay = 0;             // 일차
    private int totalGameMinutes = (26 - 9) * 60; // 하루 총 분(9시 ~ 26시 → 1020분)

    private string currentServer;

    public bool isTimeFlow = true; // 시간 흐름 제어 변수

    private DateTime? _sessionStartUtc;
    private string _currentServerForPlay;
    private long _cachedPlaySeconds;

    public GameObject dayEndPanel;      // 곧 하루가 끝남 팝업 패널
    public CanvasGroup dayEndGroup;     
    private Coroutine dayEndCo;         // 중복 실행 방지용 코루틴
    private bool dayEndWarningShown = false;  // 오늘 하루에 한 번만 뜨게

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
        currentServer = serverName;
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

        WireDayEndPanelInScene();
    }
    void Start()
    {
        // 동적으로 씬에서 오브젝트를 찾아 연결
        if (dayText == null)
            dayText = GameObject.Find("DayText")?.GetComponent<TMP_Text>();

        if (clockHandImage == null)
            clockHandImage = GameObject.Find("DayPanel_niddle")?.GetComponent<Image>();

        WireDayEndPanelInScene();

        UpdateDayUI();
        UpdateClockProgressUI();
    }

    void WireDayEndPanelInScene()
    {
        if (dayEndPanel != null && dayEndGroup != null) return;

        // 비활성 포함해서 전부 스캔
        var groups = FindObjectsOfType<CanvasGroup>(true);
        foreach (var cg in groups)
        {
            if (cg.gameObject.name == "DayEndWarningPanel")
            {
                // 프리팹 에셋이 아닌, 씬에 실제 배치된 객체만 채택
                if (!cg.gameObject.scene.IsValid()) continue;

                dayEndPanel = cg.gameObject;
                dayEndGroup = cg;

                // 초기 상태 정리
                dayEndGroup.alpha = 0f;
                dayEndPanel.SetActive(false);
                Debug.Log("[TimeManager] DayEndWarningPanel auto-wired.");
                break;
            }
        }
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

            // '하루 종료 1분 전' 체크
            CheckDayEndWarning();

            UpdateClockProgressUI();
        }
    }

    private void CheckDayEndWarning()
    {
        // 하루 동안 지난 시간(분)
        int minutesPassed = (hour - 9) * 60 + minute;

        int remainingMinutes = totalGameMinutes - minutesPassed;

        // 남은 시간이 1분이고, 아직 경고를 안 띄웠다면
        if (remainingMinutes == 60 && !dayEndWarningShown)
        {
            SFXManager.Instance.PlayDayOffSFX();
            ShowDayEndWarning();
            dayEndWarningShown = true;
        }
    }

    public void ShowDayEndWarning()
    {
        if (dayEndCo != null)
            StopCoroutine(dayEndCo);

        dayEndCo = StartCoroutine(DayEndWarningRoutine());
    }

    private IEnumerator DayEndWarningRoutine()
    {
        if (dayEndPanel == null || dayEndGroup == null)
            yield break;

        dayEndPanel.SetActive(true);

        float duration = 0.5f;
        float t = 0f;

        // 페이드 인
        while (t < duration)
        {
            t += Time.deltaTime;
            dayEndGroup.alpha = Mathf.Lerp(0f, 1f, t / duration);
            yield return null;
        }
        dayEndGroup.alpha = 1f;

        // 화면에 유지
        yield return new WaitForSeconds(2f);

        // 페이드 아웃
        t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            dayEndGroup.alpha = Mathf.Lerp(1f, 0f, t / duration);
            yield return null;
        }
        dayEndGroup.alpha = 0f;

        dayEndPanel.SetActive(false);
        dayEndCo = null;
    }

    public void LoadDay()
    {
        if (string.IsNullOrWhiteSpace(currentServer))
        {
            Debug.LogWarning(
                "[TimeManager] 서버명이 설정되지 않아 " +
                "날짜·시간을 불러올 수 없습니다."
            );

            return;
        }

        if (!SaveService.EnsureLoaded(currentServer))
        {
            currentDay = 1;
            hour = 9;
            minute = 0;

            UpdateDayUI();
            UpdateClockProgressUI();

            return;
        }

        SaveData saveData =
            SaveService.CurrentData;

        if (saveData == null ||
            saveData.worldTimeData == null)
        {
            currentDay = 1;
            hour = 9;
            minute = 0;

            Debug.LogWarning(
                "[TimeManager] 날짜·시간 데이터가 없어 " +
                "기본값을 사용합니다."
            );
        }
        else
        {
            WorldTimeSaveData timeData =
                saveData.worldTimeData;

            currentDay = Mathf.Max(1, timeData.day);
            hour = Mathf.Clamp(timeData.hour, 0, 26);
            minute = Mathf.Clamp(timeData.minute, 0, 59);

            Debug.Log(
                $"[TimeManager] 통합 세이브에서 불러옴: " +
                $"{currentDay}일차 {hour:D2}:{minute:D2}"
            );
        }

        // 이전 세이브에서 남아 있던 분 계산값 제거
        timer = 0f;

        UpdateDayUI();
        UpdateClockProgressUI();

        dayEndWarningShown = false;
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
        //float angle = Mathf.Lerp(0, 360, progress);
        float zAngle = Mathf.Lerp(-90f, -360f, progress);

        // 시계방향 회전(원하면 -angle)
        clockHandImage.rectTransform.localEulerAngles = new Vector3(0, 0, zAngle);
    }

    void UpdateDayUI()
    {
        if (dayText == null) return;
        dayText.text = $"{currentDay}일차";
    }

    IEnumerator EndOfDayRoutine()
    {
        isTimeFlow = false;

        if (NPCDialogueUIManager.Instance != null && NPCDialogueUIManager.Instance.IsOpen())
            NPCDialogueUIManager.Instance.CloseDialogue();

        if (DialogueFocusManager.Instance != null)
            DialogueFocusManager.Instance.EndFocusImmediate();

        yield return null;

        currentDay++;           // 날짜 먼저 증가
        hour = 9;           // 날짜 넘길 때 시간 초기화
        minute = 0;

        //날 넘어갈때 손님도 초기화
        if (CustomerSaveManager.Instance != null)
        {
            CustomerSaveManager.Instance.ClearForNewDay();
        }

        // 다음 날로 넘어갈 때 플래그 리셋
        dayEndWarningShown = false;

        SaveDayData();          // 증가한 날짜 저장

        OnNewDayStarted?.Invoke();

        yield return new WaitForSeconds(1f);

        if (FadeManager.Instance != null)
            FadeManager.Instance.FadeToScene("StatementScene");
        else
            SceneManager.LoadScene("StatementScene");
    }

    public void SaveDayData()
    {
        if (string.IsNullOrWhiteSpace(currentServer))
        {
            Debug.LogWarning(
                "[TimeManager] 서버명이 설정되지 않아 " +
                "날짜·시간 저장을 건너뜁니다."
            );

            return;
        }

        if (!SaveService.EnsureLoaded(currentServer))
        {
            Debug.LogError(
                "[TimeManager] 현재 세이브를 준비할 수 없어 " +
                "날짜·시간을 저장하지 못했습니다: " +
                currentServer
            );

            return;
        }

        SaveService.CurrentData.worldTimeData =
            new WorldTimeSaveData
            {
                day = Mathf.Max(1, currentDay),
                hour = Mathf.Clamp(hour, 0, 26),
                minute = Mathf.Clamp(minute, 0, 59)
            };

        SaveService.CurrentData
            .worldTimeMigrationCompleted = true;

        SaveService.SaveCurrent();

        Debug.Log(
            $"[TimeManager] {currentDay}일차 " +
            $"{hour:D2}:{minute:D2} 통합 저장 완료"
        );
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
        EndAndPersistSession();

        _currentServerForPlay = serverName;
        _cachedPlaySeconds = 0;

        if (SaveService.EnsureLoaded(serverName))
        {
            PlaytimeSaveData playtimeData =
                SaveService.CurrentData.playtimeData;

            if (playtimeData != null)
            {
                _cachedPlaySeconds = Math.Max(
                    0,
                    playtimeData.seconds
                );
            }
        }

        _sessionStartUtc = DateTime.UtcNow;
    }

    public void EndAndPersistSession()
    {
        if (_sessionStartUtc == null ||
            string.IsNullOrWhiteSpace(
                _currentServerForPlay
            ))
        {
            return;
        }

        long elapsed = (long)Math.Max(
            0,
            (
                DateTime.UtcNow -
                _sessionStartUtc.Value
            ).TotalSeconds
        );

        _cachedPlaySeconds += elapsed;

        if (SaveService.EnsureLoaded(
            _currentServerForPlay
        ))
        {
            SaveService.CurrentData.playtimeData =
                new PlaytimeSaveData
                {
                    seconds = _cachedPlaySeconds,
                    lastPlayed = DateTime.Now.ToString(
                        "yyyy-MM-dd HH:mm",
                        CultureInfo.InvariantCulture
                    )
                };

            SaveService.CurrentData
                .playtimeMigrationCompleted = true;

            SaveService.SaveCurrent();
        }
        else
        {
            Debug.LogError(
                "[TimeManager] 현재 세이브를 준비할 수 없어 " +
                "플레이 시간을 저장하지 못했습니다: " +
                _currentServerForPlay
            );
        }

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

    public string GetCurrentTimeTooltipText()
    {
        int displayHour = hour;

        // 24, 25시는 다음날 오전 0시, 1시처럼 보이도록 처리
        if (displayHour >= 24)
            displayHour -= 24;

        string period = displayHour < 12 ? "오전" : "오후";

        int hour12 = displayHour % 12;
        if (hour12 == 0)
            hour12 = 12;

        return $"{period} {hour12}시";
    }
}

//[System.Serializable]
//public class DayData
//{
//    public int day;
//    public int hour;
//    public int minute;
//}
