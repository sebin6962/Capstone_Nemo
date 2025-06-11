using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadeManagerBootstrap : MonoBehaviour
{
    public GameObject fadeManagerPrefab;

    void Awake()
    {
        if (FadeManager.Instance == null)
        {
            GameObject obj = Instantiate(fadeManagerPrefab);
            DontDestroyOnLoad(obj);
            Debug.Log("FadeManager가 자동 생성되었습니다.");
        }
        else
        {
            Destroy(gameObject); // 중복 방지
        }
    }
}
