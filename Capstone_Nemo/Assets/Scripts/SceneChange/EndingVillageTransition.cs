using UnityEngine;

public class EndingVillageTransition : MonoBehaviour
{
    private const string VillageEndingKey = "PlayVillageEnding";

    private bool isTransitioning;

    // Timeline Signal에서 호출
    public void GoToVillageEnding()
    {
        if (isTransitioning)
            return;

        isTransitioning = true;

        // 일반적인 VillageScene 진입과 엔딩 진입을 구분
        PlayerPrefs.SetInt(VillageEndingKey, 1);
        PlayerPrefs.Save();

        if (FadeManager.Instance != null)
        {
            FadeManager.Instance.FadeToScene("VillageScene");
        }
        else
        {
            Debug.LogError("[EndingVillageTransition] FadeManager.Instance가 없습니다.");
        }
    }
}