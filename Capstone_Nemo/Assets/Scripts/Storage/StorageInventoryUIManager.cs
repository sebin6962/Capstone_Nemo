using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StorageInventoryUIManager : MonoBehaviour
{
    public static StorageInventoryUIManager Instance;
    public GameObject panel;                     // â�� �г�
    public List<StorageInventorySlot> slots;     // �̸� ��ġ�� ���Ե�

    public void ToggleStorageUI()
    {
        // ���� �ڽ� �κ��丮 ���� ������ â�� ����/�ݱ� ����
        if (PlayerStoreBoxInventoryUIManager.Instance != null && PlayerStoreBoxInventoryUIManager.Instance.IsOpen())
            return;

        // �ڽ� �κ��丮 ���� ������ â�� ����/�ݱ� ����
        if (BoxInventoryManager.Instance != null && BoxInventoryManager.Instance.IsInventoryOpen())
            return;

        // ���� �г��� ���� ������ â�� ����/�ݱ� ����
        if (DoGamUIManager.Instance != null && DoGamUIManager.Instance.IsOpen())
            return;

        // UI ��ư �ܿ��� �� �� ���� ���ǹ� �߰�
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

        // â�� �� �� Ȯ�� ó��
        StorageAlertManager.Instance.OnStorageOpened();
    }

    public void UpdateSlots()
    {
        // ��� ���� �ʱ�ȭ
        foreach (var slot in slots)
            slot.ClearSlot();

        // â�� ������ ä���
        int i = 0;
        foreach (var pair in StorageInventory.Instance.GetAllItems())
        {
            if (i >= slots.Count) break;

            Sprite sprite = Resources.Load<Sprite>("Sprites/Ingredients/" + pair.Key);
            if (sprite == null)
            {
                Debug.LogWarning($"��������Ʈ �ε� ����: {pair.Key}");
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
