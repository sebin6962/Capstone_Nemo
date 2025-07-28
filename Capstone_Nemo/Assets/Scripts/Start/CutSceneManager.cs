using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CutSceneManager : MonoBehaviour
{
    public float cutSceneDuration = 2f; // 컷신 보여줄 시간(초)

    void Start()
    {
        StartCoroutine(GoToVillageAfterDelay());
    }

    private IEnumerator GoToVillageAfterDelay()
    {
        yield return new WaitForSeconds(cutSceneDuration);

        if (VillageSceneManager.Instance != null)
        {
            Destroy(VillageSceneManager.Instance.gameObject);
            VillageSceneManager.Instance = null;
        }

        if (VillageSceneManager.Instance != null)
        {
            VillageSceneManager.Instance.ResetData();
        }

        // VillageScene으로 페이드 전환
        SceneTransitionInfo.Instance.entranceID = "FromPlayerStore";
        FadeManager.Instance.FadeToScene("VillageScene");
        PlayerPrefs.SetInt("StartTimeOnEnter", 1); // 시간 흐름 플래그도 이곳에서
    }
}
