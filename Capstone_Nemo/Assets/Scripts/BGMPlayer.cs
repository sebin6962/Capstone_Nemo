using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BGMPlayer : MonoBehaviour
{
    private static BGMPlayer instance;

    void Awake()
    {
        // �̹� �����ϴ� BGMPlayer�� ������ ���� ������ �� �ı�
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject); // �� ��ȯ �� �ı����� �ʵ��� ��
    }
    //BGM ��ü �Լ�(��: Ư�� ������ ���� �ٲٱ�)�� ���Ŀ� �߰�
}
