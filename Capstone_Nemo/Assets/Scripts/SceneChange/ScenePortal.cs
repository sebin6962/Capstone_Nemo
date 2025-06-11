using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScenePortal : MonoBehaviour
{
    [Header("�� ���� ���� ��ȯ�� �� �̸�")]
    public string targetScene;

    [Header("���� ���� ���� ���� �̸�")]
    public string entranceID;

    private bool isInTrigger = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            isInTrigger = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            isInTrigger = false;
    }

    private void Update()
    {
        if (isInTrigger && (Input.GetKeyDown(KeyCode.E)))
        {
            SceneTransitionInfo.Instance.entranceID = entranceID;
            Debug.Log($"[Portal] Scene change to {targetScene}, entranceID set to {entranceID}");
            FadeManager.Instance.FadeToScene(targetScene, 0.5f);
        }
    }
}
