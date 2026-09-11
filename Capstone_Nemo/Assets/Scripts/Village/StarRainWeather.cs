using UnityEngine;

public static class StarRainWeather
{
    // 하루에 별빛 비가 내릴 확률
    private const float StarRainChance = 0.15f;

    /// <summary>
    /// 특정 세이브 슬롯의 특정 날짜가 별빛 비가 오는 날인지 반환합니다.
    /// 같은 세이브 + 같은 날짜라면 항상 같은 결과가 나옵니다.
    /// </summary>
    public static bool IsStarRainDay(string saveName, int currentDay)
    {
        // 모든 세이브 슬롯의 1일차는 무조건 맑음
        if (currentDay <= 1)
            return false;

        unchecked
        {
            int seed = 17;

            // 세이브 슬롯 이름을 seed에 포함
            if (!string.IsNullOrEmpty(saveName))
            {
                for (int i = 0; i < saveName.Length; i++)
                {
                    seed = seed * 31 + saveName[i];
                }
            }

            // 현재 날짜를 seed에 포함
            seed = seed * 31 + currentDay;

            // UnityEngine.Random을 사용하지 않고
            // 이 날짜만을 위한 고정 난수 생성
            System.Random dailyRandom =
                new System.Random(seed);

            return dailyRandom.NextDouble() < StarRainChance;
        }
    }
}