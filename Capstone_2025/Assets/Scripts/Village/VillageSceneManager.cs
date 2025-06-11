using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VillageSceneManager : MonoBehaviour
{
    void Start()
    {
        if (PlayerPrefs.GetInt("StartTimeOnEnter", 0) == 1)
        {
            PlayerPrefs.SetInt("StartTimeOnEnter", 0); // 플래그 리셋
            // 시간 흐름 시작!
            TimeManager.Instance?.SetTimeFlow(true);
        }
    }
}
