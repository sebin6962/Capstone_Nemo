using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoxObject : MonoBehaviour
{
    [HideInInspector] public StorageInventory storage;

    void Awake()
    {
        // 자동 연결
        storage = StorageInventory.Instance;
    }

}
