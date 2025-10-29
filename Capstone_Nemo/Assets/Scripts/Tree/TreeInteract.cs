using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeInteract : MonoBehaviour
{
    public static TreeInteract Instance;

    private bool isPlayerNear = false;
    public GameObject popupUI; // 팝업창 오브젝트
    //private PlayerManager playerManager; // 이동 관리 스크립트 참조

    private void Awake()
    {
        Instance = this; // 싱글턴 인스턴스 지정
    }

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

        if (!isActive && TreeLevelUnlocker.Instance != null)
        {
            TreeLevelUnlocker.Instance.ApplyPanelSprite(); // 팝업 오픈 시 최신 스프라이트 반영
        }
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

    public bool IsOpen()
    {
        return popupUI != null && popupUI.activeSelf;
    }
}
