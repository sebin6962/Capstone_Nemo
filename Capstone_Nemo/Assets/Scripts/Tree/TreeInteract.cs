using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeInteract : MonoBehaviour
{
    public static TreeInteract Instance;

    private bool isPlayerNear = false;
    public GameObject popupUI; // �˾�â ������Ʈ
    //private PlayerManager playerManager; // �̵� ���� ��ũ��Ʈ ����

    void Update()
    {
        if (isPlayerNear && Input.GetKeyDown(KeyCode.E))
        {
            TogglePopup();
        }
    }
    private void TogglePopup()
    {
        bool isActive = popupUI.activeSelf;
        popupUI.SetActive(!isActive);
    }
    public void ClosePopup()
    {
        popupUI.SetActive(false);
        // �ʿ��ϸ� �÷��̾� �̵� ��� ����
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = true;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = false;
        }
    }
}
