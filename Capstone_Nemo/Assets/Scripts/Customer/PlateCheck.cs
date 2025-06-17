using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlateCheck : MonoBehaviour
{
    private Customer targetCustomer;

    public void SetTargetCustomer(Customer customer)
    {
        targetCustomer = customer;
        Debug.Log($"[PlateCheck] �մ� ���� �Ϸ�: {customer.name}");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[PlateCheck] Ʈ���� �浹 �߻�: {other.name}");

        /*if (other.CompareTag("HeldDagwa"))
        {
            Debug.Log($"[PlateCheck] HeldDagwa ������");

            DagwaItem dagwa = other.GetComponent<DagwaItem>();
            if (dagwa == null)
            {
                Debug.LogError("[PlateCheck] DagwaItem�� �����ϴ�!");
                return;
            }

            if (targetCustomer == null)
            {
                Debug.LogError("[PlateCheck] targetCustomer�� null�Դϴ�!");
                return;
            }

            Debug.Log($"[PlateCheck] '{dagwa.dagwaName}' �� {targetCustomer.name}���� ����");
            targetCustomer.Serve(dagwa.dagwaName);
        }*/
    }

    public void TryServeDagwa()
    {
        if (targetCustomer == null)
        {
            Debug.LogWarning("[PlateCheck] �մ� ����");
            return;
        }

        ResultItemUI nearestDagwa = FindNearbyDagwa();
        if (nearestDagwa == null)
        {
            Debug.LogWarning("[PlateCheck] ��ó�� �ٰ� ����");
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
            Debug.Log($" {dagwaName} �� {targetCustomer.name}���� ���޵�");
        }
        else
        {
            Debug.LogWarning("�մ��� �Ҵ���� ����");
        }
    }
}
