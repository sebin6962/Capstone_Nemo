using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StatementManager : MonoBehaviour
{
    public void OnNextDayButtonClicked()
    {
        PlayerPrefs.SetFloat("SpawnX", -16f);
        PlayerPrefs.SetFloat("SpawnY", 5f);
        PlayerPrefs.SetFloat("SpawnZ", 0f);

        // ������ �÷��� ����
        PlayerPrefs.SetInt("NextDayFlag", 1);

        FadeManager.Instance.FadeToScene("VillageScene", 0.5f);

        //// �Ϸ� ���� �� ����!
        //if (TimeManager.Instance != null)
        //{
        //    TimeManager.Instance.currentDay++;
        //    TimeManager.Instance.hour = 9;
        //    TimeManager.Instance.minute = 0;
        //    TimeManager.Instance.SaveDayData();
        //}
    }
}
