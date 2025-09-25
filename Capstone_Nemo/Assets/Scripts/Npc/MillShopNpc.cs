using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MillShopNpc : MonoBehaviour
{
    public GameObject ShopPanel;
    public NpcTrigger trigger;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && trigger.isPlayerNearNpc && !IsMillOpen())
        {
            Debug.Log("E키 눌림 - 방앗간 토글 시도");
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
