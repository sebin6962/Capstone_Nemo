using UnityEngine;

public class VillageSceneManager : MonoBehaviour
{
    public static VillageSceneManager Instance;

    public bool IsStarRainDay { get; private set; }

    [Header("별빛 비")]
    [Tooltip("별빛 비 파티클의 최상위 오브젝트")]
    [SerializeField] private GameObject starRainObject;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            // VillageSceneManager는 씬마다 존재하므로
            // DontDestroyOnLoad는 사용하지 않음
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        ResetData();
    }

    public void ResetData()
    {
        string serverName =
            PlayerPrefs.GetString("SelectedSave", "");

        SetupServerNameAllManagers(serverName);

        StarDataManager.Instance?.SetServerName(serverName);
        StarDataManager.Instance?.LoadStarData();

        PlayerLevelManager.Instance?.SetServerName(serverName);
        PlayerLevelManager.Instance?.Load();

        TreeLevelUnlocker.Instance?.SetServerName(serverName);
        TreeLevelUnlocker.Instance?.LoadUnlockData();

        StorageInventory.Instance?.SetServerName(serverName);
        StorageInventory.Instance?.LoadStorage();

        TimeManager.Instance?.SetServerName(serverName);
        TimeManager.Instance?.LoadDay();

        // 날짜를 불러온 뒤 오늘의 별빛 비 여부 적용
        ApplyDailyStarRain(serverName);

        PlayerPrefs.SetString("SelectedSave", serverName);

        if (PlayerPrefs.GetInt("StartTimeOnEnter", 0) == 1)
        {
            PlayerPrefs.SetInt("StartTimeOnEnter", 0);

            TimeManager.Instance?.SetTimeFlow(true);
        }

        TimeManager.Instance?.BeginSessionForSelectedSave();
    }

    private void ApplyDailyStarRain(string serverName)
    {
        if (starRainObject == null)
        {
            IsStarRainDay = false;

            AmbientSoundManager.Instance?.SetStarRainActive(false);

            Debug.LogWarning(
                "[VillageSceneManager] 별빛 비 오브젝트가 연결되지 않았습니다."
            );

            return;
        }

        if (TimeManager.Instance == null)
        {
            IsStarRainDay = false;

            starRainObject.SetActive(false);

            AmbientSoundManager.Instance?.SetStarRainActive(false);

            Debug.LogWarning(
                "[VillageSceneManager] TimeManager를 찾을 수 없습니다."
            );

            return;
        }

        int currentDay =
            Mathf.Max(
                1,
                TimeManager.Instance.currentDay
            );

        // 공용 날씨 시스템을 사용해 오늘의 날씨 판정
        IsStarRainDay =
            StarRainWeather.IsStarRainDay(
                serverName,
                currentDay
            );

        starRainObject.SetActive(IsStarRainDay);

        // 환경음도 날씨와 동일하게 적용
        AmbientSoundManager.Instance?.SetStarRainActive(
            IsStarRainDay
        );

        Debug.Log(
            $"[VillageSceneManager] " +
            $"{currentDay}일차 날씨: " +
            $"{(IsStarRainDay ? "별빛 비" : "맑음")}"
        );
    }

    private void SetupServerNameAllManagers(string serverName)
    {
        StarDataManager.Instance?.SetServerName(serverName);
        PlayerLevelManager.Instance?.SetServerName(serverName);
        TreeLevelUnlocker.Instance?.SetServerName(serverName);
        StorageInventory.Instance?.SetServerName(serverName);
        TimeManager.Instance?.SetServerName(serverName);
    }
}