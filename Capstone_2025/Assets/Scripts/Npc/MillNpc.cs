using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MillNpc : MonoBehaviour
{
    public GameObject MillPanel;
    
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
    }

    public bool IsMillOpen()
    {
        return MillPanel.activeSelf;
    }
}
