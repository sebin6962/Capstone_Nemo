using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NpcManager : MonoBehaviour
{
    public GameObject shopPanel;
    public ShopManager shopManager;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && NpcTrigger.isPlayerNearNpc && !IsShopOpen())
        {
            Debug.Log("EŰ ���� - ���� ��� �õ�");
            OpenShop();
        }
    }

    private void OpenShop()
    {
        shopPanel.SetActive(true);
        shopManager.OpenShop();
    }

    public bool IsShopOpen()
    {
        return shopPanel.activeSelf;
    }
}
