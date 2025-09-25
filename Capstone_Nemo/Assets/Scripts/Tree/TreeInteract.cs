using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeInteract : MonoBehaviour
{
    public static TreeInteract Instance;

    private bool isPlayerNear = false;
    public GameObject popupUI; // 팝업창 오브젝트
    //private PlayerManager playerManager; // 이동 관리 스크립트 참조

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
        // 필요하면 플레이어 이동 잠금 해제
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
