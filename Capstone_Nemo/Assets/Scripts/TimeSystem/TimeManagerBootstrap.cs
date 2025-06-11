using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeManagerBootstrap : MonoBehaviour
{
    public GameObject timeManagerPrefab;

    void Awake()
    {
        // TimeManager�� ���� ���� ��쿡�� ����
        if (FindObjectOfType<TimeManager>() == null)
        {
            GameObject obj = Instantiate(timeManagerPrefab);
            DontDestroyOnLoad(obj);
            Debug.Log("TimeManager�� �ڵ� �����Ǿ����ϴ�.");
        }
        else
        {
            Destroy(gameObject); // �ߺ� ����
        }
    }
}
