using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MillNpcManager : MonoBehaviour
{
    public GameObject MillPanel;
    public MillManager millManager;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && NpcTrigger.isPlayerNearNpc && !IsMillOpen())
        {
            Debug.Log("E키 눌림 - 방앗간 토글 시도");
            OpenMill();
        }
    }

    private void OpenMill()
    {
        MillPanel.SetActive(true);
        millManager.OpenMill();
    }

    public bool IsMillOpen()
    {
        return MillPanel.activeSelf;
    }
}
