using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MillShopNpc : MonoBehaviour
{
    public GameObject ShopPanel;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && NpcTrigger.isPlayerNearNpc && !IsMillOpen())
        {
            Debug.Log("EŰ ���� - ��Ѱ� ��� �õ�");
            OpenShop();
        }
    }

    private void OpenShop()
    {
        ShopPanel.SetActive(true);
    }

    public bool IsMillOpen()
    {
        return ShopPanel.activeSelf;
    }
}
