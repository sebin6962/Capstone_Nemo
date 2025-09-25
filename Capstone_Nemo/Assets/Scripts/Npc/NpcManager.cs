using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NpcManager : MonoBehaviour
{
    public GameObject shopPanel;
    public ShopManager shopManager;
    public NpcTrigger trigger;

    public string dataPath;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && trigger.isPlayerNearNpc && !IsShopOpen())
        {
            Debug.Log("E키 눌림 - 상점 토글 시도");
            shopManager.LoadShopData(dataPath);
            shopManager.OpenShop();
        }
    }

    /*private void OpenShop()
    {
        shopPanel.SetActive(true);
        OpenShop();
    }*/

    public bool IsShopOpen()
    {
        return shopPanel.activeSelf;
    }
}
