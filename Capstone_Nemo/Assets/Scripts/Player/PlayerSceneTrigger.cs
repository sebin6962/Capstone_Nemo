using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class PlayerSceneTrigger : MonoBehaviour
{
    private string targetScene = "";
    private bool isInTrigger = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("PlayerStore"))
        {
            targetScene = "PlayerStoreScene"; // �ʿ� �� �̸� Ŀ���͸���¡
            isInTrigger = true;
        }

        if (other.CompareTag("TreeEntry"))
        {
            targetScene = "TreeScene"; // �ʿ� �� �̸� Ŀ���͸���¡
            isInTrigger = true;
        }

        if (other.CompareTag("Market"))
        {
            targetScene = "MarketScene"; // �̵��� �� �̸�
            isInTrigger = true;
        }

        if (other.CompareTag("MillStore"))
        {
            targetScene = "MillScene"; // �̵��� �� �̸�
            isInTrigger = true;
        }

        if (other.CompareTag("Village"))
        {
            targetScene = "VillageScene"; // �̵��� �� �̸�
            isInTrigger = true;
        }

    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("PlayerStore"))
        {
            targetScene = "";
            isInTrigger = false;
        }

        if (other.CompareTag("TreeEntry"))
        {
            targetScene = "";
            isInTrigger = false;
        }

        if (other.CompareTag("Market"))
        {
            targetScene = "";
            isInTrigger = false;
        }

        if (other.CompareTag("MillStore"))
        {
            targetScene = "";
            isInTrigger = false;
        }

        if (other.CompareTag("Village"))
        {
            
            targetScene = "";
            isInTrigger = false;
        }
    }

    private void Update()
    {
        if (isInTrigger && (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.S)))
        {
            Debug.Log("��ȣ�ۿ�: WŰ ���� �� �� ��ȯ ��");
            FadeManager.Instance.FadeToScene(targetScene, 0.5f); // 0.5�� �� �� ��ȯ
        }
    }
}
