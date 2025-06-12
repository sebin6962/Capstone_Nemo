using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StorageInventoryUIManager : MonoBehaviour
{
    public static StorageInventoryUIManager Instance;
    public GameObject panel;                     // 창고 패널
    public List<StorageInventorySlot> slots;     // 미리 배치된 슬롯들

    public void ToggleStorageUI()
    {
        // 가게 박스 인벤토리 열려 있으면 창고 열기/닫기 막기
        if (PlayerStoreBoxInventoryUIManager.Instance != null && PlayerStoreBoxInventoryUIManager.Instance.IsOpen())
            return;

        // 박스 인벤토리 열려 있으면 창고 열기/닫기 막기
        if (BoxInventoryManager.Instance != null && BoxInventoryManager.Instance.IsInventoryOpen())
            return;

        // 도감 패널이 열려 있으면 창고 열기/닫기 막기
        if (DoGamUIManager.Instance != null && DoGamUIManager.Instance.IsOpen())
            return;

        // UI 버튼 외에는 열 수 없게 조건문 추가
        if (!EventSystem.current.currentSelectedGameObject ||
            EventSystem.current.currentSelectedGameObject.GetComponent<Button>() == null)
            return;

        if (panel.activeSelf)
        {
            panel.SetActive(false);
        }
        else
        {
            UpdateSlots();
            panel.SetActive(true);
        }

        // 창고 열 때 확인 처리
        StorageAlertManager.Instance.OnStorageOpened();
    }

    public void UpdateSlots()
    {
        // 모든 슬롯 초기화
        foreach (var slot in slots)
            slot.ClearSlot();

        // 창고 데이터 채우기
        int i = 0;
        foreach (var pair in StorageInventory.Instance.GetAllItems())
        {
            if (i >= slots.Count) break;

            Sprite sprite = Resources.Load<Sprite>("Sprites/Ingredients/" + pair.Key);
            if (sprite == null)
            {
                Debug.LogWarning($"스프라이트 로드 실패: {pair.Key}");
                continue;
            }

            slots[i].SetItem(pair.Key, sprite, pair.Value);
            i++;
        }
    }

    //private void OnDisable()
    //{
    //    if (InventoryTooltipManager.Instance != null)
    //        InventoryTooltipManager.Instance.Hide();
    //}

    public bool IsOpen()
    {
        return panel != null && panel.activeSelf;
    }
}
