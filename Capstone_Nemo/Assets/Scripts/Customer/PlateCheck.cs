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
    public void SendDagwaToCustomer(string dagwaName)
    {
        if (targetCustomer != null)
        {
            targetCustomer.Serve(dagwaName);
            Debug.Log($" {dagwaName} �� {targetCustomer.name}���� ���޵�");
        }
        else
        {
            Debug.LogWarning("���� �Ҵ���� �ʾҽ��ϴ�!");
        }
    }
}
