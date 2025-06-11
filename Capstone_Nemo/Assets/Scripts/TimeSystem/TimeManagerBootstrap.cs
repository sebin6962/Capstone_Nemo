using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeManagerBootstrap : MonoBehaviour
{
    public GameObject timeManagerPrefab;

    void Awake()
    {
        // TimeManager가 씬에 없을 경우에만 생성
        if (FindObjectOfType<TimeManager>() == null)
        {
            GameObject obj = Instantiate(timeManagerPrefab);
            DontDestroyOnLoad(obj);
            Debug.Log("TimeManager가 자동 생성되었습니다.");
        }
        else
        {
            Destroy(gameObject); // 중복 방지
        }
    }
}
