using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{

    //public static GameManager Instance;

    //[Header("씬 전환 시 유지할 매니저")]
    //public HeldItemManager heldItemManager;
    //public StorageInventoryUIManager storageInventoryUIManager;
    //public DoGamUIManager doGamUIManager;

    //private void Awake()
    //{
    //    if (Instance == null)
    //    {
    //        Instance = this;
    //        DontDestroyOnLoad(gameObject);

    //        if (heldItemManager == null)
    //            heldItemManager = FindObjectOfType<HeldItemManager>();

    //        if (storageInventoryUIManager == null)
    //            storageInventoryUIManager = FindObjectOfType<StorageInventoryUIManager>();

    //        if (doGamUIManager == null)
    //            doGamUIManager = FindObjectOfType<DoGamUIManager>();

    //        Debug.Log("[GameManager] 매니저 자동 연결 완료");
    //    }
    //    else
    //    {
    //        Destroy(gameObject); // 중복 방지
    //    }
    //}

}
