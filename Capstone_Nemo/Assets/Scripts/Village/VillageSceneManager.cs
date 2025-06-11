using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VillageSceneManager : MonoBehaviour
{
    void Start()
    {
        if (PlayerPrefs.GetInt("StartTimeOnEnter", 0) == 1)
        {
            PlayerPrefs.SetInt("StartTimeOnEnter", 0); // �÷��� ����
            // �ð� �帧 ����!
            TimeManager.Instance?.SetTimeFlow(true);
        }
    }
}
