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

    public void TryServeDagwa()
    {
        if (targetCustomer == null)
        {
            Debug.LogWarning("[PlateCheck] 손님 없음");
            return;
        }

        ResultItemUI nearestDagwa = FindNearbyDagwa();
        if (nearestDagwa == null)
        {
            Debug.LogWarning("[PlateCheck] 근처에 다과 없음");
            return;
        }

        targetCustomer.Serve(nearestDagwa.GetItemName());
    }

    private ResultItemUI FindNearbyDagwa()
    {
        ResultItemUI[] allDagwa = FindObjectsOfType<ResultItemUI>();

        ResultItemUI closest = null;
        float minDist = float.MaxValue;

        foreach (var dagwa in allDagwa)
        {
            float dist = Vector3.Distance(transform.position, dagwa.transform.position);
            if (dist < 1.0f && dist < minDist)
            {
                closest = dagwa;
                minDist = dist;
            }
        }

        return closest;
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
            Debug.LogWarning("손님이 할당되지 않음");
        }
    }
}
