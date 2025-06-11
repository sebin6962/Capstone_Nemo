using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStart : MonoBehaviour
{
    public float waitBeforeCut = 1f;     // ���� ���� �� �ƾ� ������ ��� (��)
    public float waitBeforeVillage = 2f; // �ƾ� �� ���� ������ ��� (��)

    public void OnClickStart()
    {
        StartCoroutine(GameStartFlow());
        TimeManager.Instance?.SetTimeFlow(false); // �ð� ����
    }

    private IEnumerator GameStartFlow()
    {
        // 1. ���� ���� ��ư ���� �� ��� ���
        yield return new WaitForSeconds(waitBeforeCut);

        // 2. cutScene���� ��ȯ (���̵�)
        FadeManager.Instance.FadeToScene("CutScene");
        // Fade ��ȯ�� ���������� �� ��ȯ �� ���̵��α��� �ϹǷ�, 1������ ���
        yield return new WaitForSeconds(FadeManager.Instance.fadeDuration + 0.1f);

        // 3. cutScene���� ��� ���
        yield return new WaitForSeconds(waitBeforeVillage);

        // 4. VillageScene���� ��ȯ (���̵�)
        // VillageScene ���� �� �ð� �帧 ������ ���� �÷��� ����
        //PlayerPrefs.SetInt("StartTimeOnEnter", 1);
        //FadeManager.Instance.FadeToScene("VillageScene");
        // ���� ���̵� Ÿ�� ��ŭ ���
        yield return new WaitForSeconds(FadeManager.Instance.fadeDuration + 0.1f);
    }
}
