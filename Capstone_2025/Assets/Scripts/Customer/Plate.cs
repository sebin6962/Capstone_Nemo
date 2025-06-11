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
            Debug.LogWarning("DagwaItem 스크립트 없음");
            return;
        }

        Debug.Log($"전달된 다과: {item.dagwaName}");
        targetCustomer.Serve(item.dagwaName);
        Destroy(dagwaObject);
    }
}
