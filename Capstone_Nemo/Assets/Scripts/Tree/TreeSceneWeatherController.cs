using UnityEngine;

public class TreeSceneWeatherController : MonoBehaviour
{
    [Header("별빛 비")]
    [Tooltip("TreeScene에서 사용할 별빛 비 파티클의 최상위 오브젝트")]
    [SerializeField] private GameObject starRainObject;

    public bool IsStarRainDay { get; private set; }

    private void Start()
    {
        ApplyWeather();
    }

    private void ApplyWeather()
    {
        string serverName =
            PlayerPrefs.GetString(
                "SelectedSave",
                ""
            );

        if (string.IsNullOrEmpty(serverName))
        {
            IsStarRainDay = false;

            SetStarRainActive(false);

            Debug.LogWarning(
                "[TreeSceneWeatherController] " +
                "선택된 세이브 슬롯을 찾을 수 없습니다."
            );

            return;
        }

        if (TimeManager.Instance == null)
        {
            IsStarRainDay = false;

            SetStarRainActive(false);

            Debug.LogWarning(
                "[TreeSceneWeatherController] " +
                "TimeManager를 찾을 수 없습니다."
            );

            return;
        }

        // 현재 선택된 세이브 슬롯 지정
        TimeManager.Instance.SetServerName(
            serverName
        );

        // 현재 날짜 불러오기
        TimeManager.Instance.LoadDay();

        int currentDay =
            Mathf.Max(
                1,
                TimeManager.Instance.currentDay
            );

        // VillageScene과 동일한 공용 계산 사용
        IsStarRainDay =
            StarRainWeather.IsStarRainDay(
                serverName,
                currentDay
            );

        SetStarRainActive(IsStarRainDay);

        Debug.Log(
            $"[TreeSceneWeatherController] " +
            $"{currentDay}일차 날씨: " +
            $"{(IsStarRainDay ? "별빛 비" : "맑음")}"
        );
    }

    private void SetStarRainActive(bool active)
    {
        if (starRainObject != null)
        {
            starRainObject.SetActive(active);
        }
        else
        {
            Debug.LogWarning(
                "[TreeSceneWeatherController] " +
                "별빛 비 오브젝트가 연결되지 않았습니다."
            );
        }

        // 별빛 비 환경음도 동일하게 적용
        AmbientSoundManager.Instance?.SetStarRainActive(
            active
        );
    }
}