using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlateCheck : MonoBehaviour
{
    private Customer targetCustomer;

    public void SetTargetCustomer(Customer customer)
    {
        targetCustomer = customer;
        Debug.Log($"[PlateCheck] 손님 연동 완료: {customer.name}");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[PlateCheck] 트리거 충돌 발생: {other.name}");

        /*if (other.CompareTag("HeldDagwa"))
        {
            Debug.Log($"[PlateCheck] HeldDagwa 감지됨");

            DagwaItem dagwa = other.GetComponent<DagwaItem>();
            if (dagwa == null)
            {
                Debug.LogError("[PlateCheck] DagwaItem이 없습니다!");
                return;
            }

            if (targetCustomer == null)
            {
                Debug.LogError("[PlateCheck] targetCustomer가 null입니다!");
                return;
            }

            Debug.Log($"[PlateCheck] '{dagwa.dagwaName}' → {targetCustomer.name}에게 전달");
            targetCustomer.Serve(dagwa.dagwaName);
        }*/
    }
    public void SendDagwaToCustomer(string dagwaName)
    {
        if (targetCustomer != null)
        {
            targetCustomer.Serve(dagwaName);
            Debug.Log($" {dagwaName} → {targetCustomer.name}에게 전달됨");
        }
        else
        {
            Debug.LogWarning("고객이 할당되지 않았습니다!");
        }
    }
}
