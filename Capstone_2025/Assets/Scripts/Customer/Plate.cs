using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Plate : MonoBehaviour
{
    [SerializeField] private Customer targetCustomer;

    public void SetTargetCustomer(Customer customer)
    {
        targetCustomer = customer;
    }

    public void ReceiveDagwa(GameObject dagwaObject)
    {
        if (targetCustomer == null || dagwaObject == null) return;

        DagwaItem item = dagwaObject.GetComponent<DagwaItem>();
        if (item == null)
        {
            Debug.LogWarning("DagwaItem ��ũ��Ʈ ����");
            return;
        }

        Debug.Log($"���޵� �ٰ�: {item.dagwaName}");
        targetCustomer.Serve(item.dagwaName);
        Destroy(dagwaObject);
    }
}
