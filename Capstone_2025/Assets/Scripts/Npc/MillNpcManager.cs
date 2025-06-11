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
            Debug.Log("EŰ ���� - ��Ѱ� ��� �õ�");
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
