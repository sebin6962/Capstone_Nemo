using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StatementManager : MonoBehaviour
{
    [SerializeField] private NextDayCutscene cutscene;
    public void OnNextDayButtonClicked()
    {
        PlayerPrefs.SetFloat("SpawnX", -16f);
        PlayerPrefs.SetFloat("SpawnY", 5f);
        PlayerPrefs.SetFloat("SpawnZ", 0f);

        // 다음날 플래그 저장
        PlayerPrefs.SetInt("NextDayFlag", 1);

        // 씬 전환은 컷신 종료 후에
        cutscene.onFinished.RemoveAllListeners();
        cutscene.onFinished.AddListener(() =>
        {
            if (FadeManager.Instance != null)
                FadeManager.Instance.FadeToScene("VillageScene", 0.5f);
            else
                SceneManager.LoadScene("VillageScene");
        });

        cutscene.Play();

        //FadeManager.Instance.FadeToScene("VillageScene", 0.5f);

        //// 하루 증가 및 저장!
        //if (TimeManager.Instance != null)
        //{
        //    TimeManager.Instance.currentDay++;
        //    TimeManager.Instance.hour = 9;
        //    TimeManager.Instance.minute = 0;
        //    TimeManager.Instance.SaveDayData();
        //}
    }
}
