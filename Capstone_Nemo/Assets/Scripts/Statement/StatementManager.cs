using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StatementManager : MonoBehaviour
{
    [SerializeField] private NextDayCutscene cutscene;

    [Header("패널")]
    [SerializeField] private GameObject statementPanel;       // 명세서 패널
    [SerializeField] private LevelUpRevealPanel levelUpPanel; // 레벨업 패널
    void Awake()
    {
        // 혹시 TimeManager 이벤트 순서가 씬 전환 뒤로 밀렸어도 안전
        UnlockManager.Instance?.ApplyScheduledUnlocksForNewDay();
        
    }

    public void OnNextDayButtonClicked()
    {
        PlayerPrefs.SetFloat("SpawnX", -16f);
        PlayerPrefs.SetFloat("SpawnY", 5f);
        PlayerPrefs.SetFloat("SpawnZ", 0f);

        // 다음날 플래그 저장
        PlayerPrefs.SetInt("NextDayFlag", 1);

        var um = UnlockManager.Instance;
        // 씬 전환은 컷신 종료 후에
        //cutscene.onFinished.RemoveAllListeners();
        //cutscene.onFinished.AddListener(() =>
        //{
        //    if (FadeManager.Instance != null)
        //        FadeManager.Instance.FadeToScene("VillageScene", 0.5f);
        //    else
        //        SceneManager.LoadScene("VillageScene");
        //});

        // 전날 레벨업 여부 검사 (다음 날 적용 예약이 있으면 true)
        if (um != null && um.HasLevelUpRevealToShow())  // ok
        {
            int newLevel = um.GetLevelUpRevealLevel();        // ★ 표시용 레벨
            var finishKeys = um.GetLevelUpRevealFinishKeys();   // ★ 표시용 키들

            if (statementPanel != null) statementPanel.SetActive(false);

            if (levelUpPanel != null)
            {
                levelUpPanel.Show(newLevel, finishKeys, () => {
                    um.MarkLevelUpRevealShown();
                    cutscene.Play();
                });
            }
            else
            {
                Debug.LogWarning("[StatementManager] levelUpPanel 미지정 → 바로 컷신 진행");
                cutscene.Play();
            }
        }
        else
        {
            cutscene.Play();
        }
    }

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
