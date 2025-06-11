using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResultItem : MonoBehaviour
{
    public string itemName;

    public void Collect()
    {
        StorageInventory.Instance.AddItem(itemName, 1);
        Debug.Log($"[��� ������] {itemName} ȹ���!");
        Destroy(gameObject);
    }
}
