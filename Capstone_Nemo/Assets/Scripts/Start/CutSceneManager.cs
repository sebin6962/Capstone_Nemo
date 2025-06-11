using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CutSceneManager : MonoBehaviour
{
    public float cutSceneDuration = 2f; // �ƽ� ������ �ð�(��)

    void Start()
    {
        StartCoroutine(GoToVillageAfterDelay());
    }

    private IEnumerator GoToVillageAfterDelay()
    {
        yield return new WaitForSeconds(cutSceneDuration);

        // VillageScene���� ���̵� ��ȯ
        FadeManager.Instance.FadeToScene("VillageScene");
        PlayerPrefs.SetInt("StartTimeOnEnter", 1); // �ð� �帧 �÷��׵� �̰�����!
    }
}
