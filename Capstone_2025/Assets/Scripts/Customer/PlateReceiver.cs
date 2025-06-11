using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlateReceiver : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("HeldDagwa"))
        {
            Debug.Log("다과가 접시에 놓였습니다!");

            // 들고 있는 상태 해제
            HeldItemManager.Instance.HideHeldItem();

            // 다과 위치 조정
            other.transform.position = transform.position + new Vector3(0, 0.3f, 0);
            other.transform.SetParent(transform); // 접시에 고정

            // 접시가 손님과 연동되어 있다면 전달
            PlateCheck plate = GetComponent<PlateCheck>();
            if (plate != null)
            {
                DagwaItem dagwa = other.GetComponent<DagwaItem>();
                if (dagwa != null)
                {
                    plate.SendDagwaToCustomer(dagwa.dagwaName);
                }
            }
        }
    }
}
