using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WateringCanAnchor : MonoBehaviour
{
    public GameObject wateringCanPrefab; // Prefab 연결
    private GameObject placedCan;
    private bool isPlayerNearby = false;

    void Start()
    {
        if (wateringCanPrefab != null)
        {
            placedCan = Instantiate(wateringCanPrefab, transform.position, Quaternion.identity);
            Vector3 fixedPos = placedCan.transform.position;
            fixedPos.z = 0f;
            placedCan.transform.position = fixedPos;

            
        }
    }

    void Update()
    {
        //if (isPlayerNearby && Input.GetKeyDown(KeyCode.E))
        //{
        //    if (!HeldItemManager.Instance.IsHoldingItem())
        //    {
        //        GameObject canInstance = Instantiate(wateringCanPrefab); // 복제본 생성
        //        BoxInventoryManager.Instance.HoldItem(canInstance);
        //        Destroy(placedCan);
        //        placedCan = null;
        //        Debug.Log("물뿌리개를 들었습니다.");
        //    }
        //}

        //if (isPlayerNearby && Input.GetKeyDown(KeyCode.Space))
        //{
        //    if (HeldItemManager.Instance.IsHoldingItem() &&
        //BoxInventoryManager.Instance.IsHoldingWateringCan())
        //    {
        //        RestoreWateringCan();
        //        BoxInventoryManager.Instance.RemoveHeldItem();
        //        Debug.Log("물뿌리개를 다시 놓았습니다.");
        //    }
        //    else
        //    {
        //        Debug.Log("물뿌리개를 들고 있지 않음");
        //    }
        //}

        if (isPlayerNearby && Input.GetKeyDown(KeyCode.E))
        {
            //// 1. 아무것도 안 들고 있을 때 → 물뿌리개 들기
            //if (!HeldItemManager.Instance.IsHoldingItem())
            //{
            //    GameObject canInstance = Instantiate(wateringCanPrefab); // 복제본 생성
            //    BoxInventoryManager.Instance.HoldItem(canInstance);
            //    Destroy(placedCan);
            //    placedCan = null;
            //    Debug.Log("물뿌리개를 들었습니다.");
            //}
            //// 2. 물뿌리개를 들고 있을 때 → 내려놓기
            //else if (BoxInventoryManager.Instance.IsHoldingWateringCan())
            //{
            //    RestoreWateringCan();
            //    BoxInventoryManager.Instance.RemoveHeldItem();
            //    Debug.Log("물뿌리개를 다시 놓았습니다.");
            //}
            //// 3. 그 외(다른 아이템을 들고 있을 때 등)
            //else
            //{
            //    Debug.Log("물뿌리개 상호작용 불가(다른 아이템을 들고 있음)");
            //}

            // 1. 아무것도 안 들고 있으면 -> 물뿌리개 들기
            if (!HeldItemManager.Instance.IsHoldingItem())
            {
                GameObject canInstance = Instantiate(wateringCanPrefab);
                BoxInventoryManager.Instance.HoldItem(canInstance);
                Destroy(placedCan);
                placedCan = null;
                Debug.Log("물뿌리개를 들었습니다.");

                SFXManager.Instance.PlayBbyongSFX();
                return; // << 반드시 return해서 아래 상호작용 중복 방지!
            }
            // 2. 물뿌리개를 들고 있으면 -> 내려놓기
            else if (BoxInventoryManager.Instance.IsHoldingWateringCan())
            {
                RestoreWateringCan();
                BoxInventoryManager.Instance.RemoveHeldItem();
                Debug.Log("물뿌리개를 다시 놓았습니다.");

                SFXManager.Instance.PlayBbyongSFX();
                return; // << 반드시 return!
            }
            // 3. 그 외(다른 아이템 들고 있을 때)
            else
            {
                Debug.Log("물뿌리개 상호작용 불가(다른 아이템 들고 있음)");
                return;
            }
        }
    }

    public void RestoreWateringCan()
    {
        if (placedCan == null && wateringCanPrefab != null)
        {
            placedCan = Instantiate(wateringCanPrefab, transform.position, Quaternion.identity);
            // Z 위치 고정
            Vector3 fixedPos = placedCan.transform.position;
            fixedPos.z = 0f;
            placedCan.transform.position = fixedPos;

            Debug.Log("물뿌리개 생성 위치: " + transform.position);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            isPlayerNearby = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            isPlayerNearby = false;
    }
}
