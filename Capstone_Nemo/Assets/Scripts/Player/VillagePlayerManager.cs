using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VillagePlayerManager : MonoBehaviour
{
    private GameObject currentItem;

    public FarmManager farmManager;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (BoxInventoryManager.Instance.IsInventoryOpen() &&
                HeldItemManager.Instance.IsHoldingItem())
            {
                BoxInventoryManager.Instance.TryAutoStoreHeldItem();
            }
        }

        if (currentItem != null && Input.GetKeyDown(KeyCode.E))
        {
            //if (currentItem.name == "wateringCan")
            //    return;

            if (currentItem.name.Contains("wateringCan") || ToolData.Instance.IsWateringCan(currentItem))
                return;

            if (HeldItemManager.Instance.IsHoldingItem())
            {
                Debug.Log("이미 아이템이나 도구를 들고 있어서 새로운 것을 들 수 없습니다.");
                return;
            }

            if (ToolData.Instance.IsTool(currentItem.name))
                Debug.Log("도구 들기: " + currentItem.name);
            else
                Debug.Log("아이템 들기: " + currentItem.name);

            BoxInventoryManager.Instance.HoldItem(currentItem);
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (BoxInventoryManager.Instance.IsHoldingWateringCan() &&
                TryGetClickedFarmTile(out var pos1, out _))
            {
                farmManager.WaterSoil(pos1);
                Debug.Log("물 뿌리기 완료");
            }
            else if (HeldItemManager.Instance.IsHoldingItem() &&
         TryGetClickedFarmTile(out var pos2, out _) &&
         !BoxInventoryManager.Instance.IsHoldingWateringCan())
            {
                string seedItemName = HeldItemManager.Instance.GetHeldItemName();

                if (!string.IsNullOrEmpty(seedItemName))
                {
                    CropData cropData = CropDataManager.Instance.GetCropDataByItemName(seedItemName);

                    if (cropData != null)
                    {
                        farmManager.PlantSeed(pos2, cropData);
                        Debug.Log($"씨앗 {seedItemName} 심기 완료");
                    }
                    else
                    {
                        Debug.LogWarning($"CropData를 찾을 수 없습니다: {seedItemName}");
                    }
                }
                else
                {
                    Debug.LogWarning("현재 들고 있는 씨앗 이름이 null 또는 빈 문자열입니다.");
                }

            }
        }
    }

    private bool TryGetClickedFarmTile(out Vector3 worldPos, out Vector3Int clickedCell)
    {
        worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        worldPos.z = 0f;

        clickedCell = farmManager.fieldTilemap.WorldToCell(worldPos);
        Vector3Int playerCell = farmManager.fieldTilemap.WorldToCell(transform.position);

        int dx = Mathf.Abs(clickedCell.x - playerCell.x);
        int dy = Mathf.Abs(clickedCell.y - playerCell.y);

        return dx <= 1 && dy <= 1 && farmManager.IsFarmTile(worldPos);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Item"))
        {
            Debug.Log("접촉한 아이템: " + other.name);
            currentItem = other.gameObject;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject == currentItem)
        {
            currentItem = null;
        }
    }
}
