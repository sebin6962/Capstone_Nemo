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
            Debug.Log("FadeManager�� �ڵ� �����Ǿ����ϴ�.");
        }
        else
        {
            Destroy(gameObject); // �ߺ� ����
        }
    }
}
