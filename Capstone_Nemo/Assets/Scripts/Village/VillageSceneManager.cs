using UnityEngine;

public class VillageSceneManager : MonoBehaviour
{
    public static VillageSceneManager Instance;

    public bool IsStarRainDay { get; private set; }

    [Header("별빛 비")]
    [Tooltip("별빛 비 파티클의 최상위 오브젝트")]
    [SerializeField] private GameObject starRainObject;

    [Tooltip("하루에 별빛 비가 내릴 확률")]
    [Range(0f, 1f)]
    [SerializeField] private float starRainChance = 0.15f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
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

        // 날짜를 불러온 다음 오늘의 별빛 비 여부 적용
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
            Debug.LogWarning(
                "[VillageSceneManager] 별빛 비 오브젝트가 연결되지 않았습니다."
            );

            return;
        }

        if (TimeManager.Instance == null)
        {
            starRainObject.SetActive(false);

            Debug.LogWarning(
                "[VillageSceneManager] TimeManager를 찾을 수 없습니다."
            );

            return;
        }

        int currentDay =
            Mathf.Max(1, TimeManager.Instance.currentDay);

        IsStarRainDay =
    DetermineStarRainDay(serverName, currentDay);

        starRainObject.SetActive(IsStarRainDay);

        Debug.Log(
            $"[VillageSceneManager] {currentDay}일차 날씨: " +
            $"{(IsStarRainDay ? "별빛 비" : "맑음")}"
        );
    }

    private bool DetermineStarRainDay(
        string serverName,
        int currentDay
    )
    {
        // 모든 세이브 파일의 1일차는 무조건 맑음
        if (currentDay <= 1)
            return false;

        /*
         * 세이브 슬롯명과 일차로 고정된 난수를 만듦
         *
         * 따라서 같은 세이브의 같은 날짜에 마을을 다시 들어와도
         * 별빛 비 여부가 바뀌지 않는다.
         *
         * UnityEngine.Random의 전역 상태에도 영향을 주지 않는다.
         */
        unchecked
        {
            int seed = 17;

            if (!string.IsNullOrEmpty(serverName))
            {
                for (int i = 0; i < serverName.Length; i++)
                {
                    seed = seed * 31 + serverName[i];
                }
            }

            seed = seed * 31 + currentDay;

            System.Random dailyRandom =
                new System.Random(seed);

            return dailyRandom.NextDouble() <
                   starRainChance;
        }
    }

    void SetupServerNameAllManagers(string serverName)
    {
        StarDataManager.Instance?.SetServerName(serverName);
        PlayerLevelManager.Instance?.SetServerName(serverName);
        TreeLevelUnlocker.Instance?.SetServerName(serverName);
        StorageInventory.Instance?.SetServerName(serverName);
        TimeManager.Instance?.SetServerName(serverName);
    }
}