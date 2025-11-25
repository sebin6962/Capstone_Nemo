using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class BoxInventoryManager : MonoBehaviour
{
    public static BoxInventoryManager Instance;

    public GameObject inventoryPanel;
    public List<InventorySlot> slots;

    private GameObject heldItem;
    private Sprite heldSprite;
    private string heldItemName;

    private string savePath;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        savePath = Path.Combine(Application.persistentDataPath, "boxInventory.json");
        LoadInventory();

        // 씨앗 무한 슬롯 세팅
        SetupInfiniteSeedSlots();
    }

    void Update()
    {
        if (BoxTrigger.isPlayerNearBox && Input.GetKeyDown(KeyCode.E))
        {
            ToggleInventory();
            SFXManager.Instance.PlayBoxOpenSFX();

            //village2 튜토리얼 진행 트리거 2
                if (TutorialManager.Instance && TutorialManager.Instance.IsCurrentStep(VillageSecondStep.OpenStorage))
                {
                    TutorialManager.Instance.GoToNextVillageSecondStep();
                }
        }
    }

    private void ToggleInventory()
    {
        if ((DoGamUIManager.Instance != null && DoGamUIManager.Instance.panel.activeSelf))
            return;

        if (StorageInventoryUIManager.Instance != null && StorageInventoryUIManager.Instance.IsOpen())
            return;

        bool isActive = inventoryPanel.activeSelf;
        inventoryPanel.SetActive(!isActive);

        if (!inventoryPanel.activeSelf && InventoryTooltipManager.Instance != null)
            InventoryTooltipManager.Instance.Hide();
    }

    public bool IsInventoryOpen() => inventoryPanel.activeSelf;

    public bool IsHoldingTool(string toolName)
    {
        return heldItemName != null &&
               ToolData.Instance != null &&
               ToolData.Instance.IsTool(heldItemName) &&
               heldItemName == toolName;
    }

    public bool IsHoldingWateringCan()
    {
        if (!HeldItemManager.Instance.IsHoldingItem()) return false;
        string name = HeldItemManager.Instance.GetHeldItemName();
        if (string.IsNullOrEmpty(name)) return false;
        return name == "wateringCan" && ToolData.Instance != null && ToolData.Instance.IsTool(name);
    }

    public void RemoveHeldItem()
    {
        heldItem = null;
        heldSprite = null;
        heldItemName = null;
        HeldItemManager.Instance.HideHeldItem();
    }

    public void HoldItem(GameObject item)
    {
        heldItem = item;

        var spriteRenderer = item.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            heldSprite = spriteRenderer.sprite;
            heldItemName = item.name.Replace("(Clone)", "").Trim();
            HeldItemManager.Instance.ShowHeldItem(heldSprite, heldItemName);
        }
        Destroy(item);
    }

    public void PlaceHeldItemInSlot(InventorySlot clickedSlot = null)
    {
        if (heldSprite == null) return;

        if (ToolData.Instance.IsTool(heldItemName))
        {
            Debug.Log("도구는 상자에 저장할 수 없습니다: " + heldItemName);
            return;
        }

        foreach (var slot in slots)
        {
            if (slot.HasItem() && slot.GetItemName() == heldItemName)
            {
                // 무한 슬롯은 제외
                if (slot.IsInfiniteSeedSlot()) continue;

                int newCount = slot.GetItemCount() + 1;
                slot.SetItem(slot.GetSprite(), heldItemName, newCount);
                RemoveHeldItem();
                SaveInventory();
                return;
            }
        }

        foreach (var slot in slots)
        {
            if (!slot.HasItem() && !slot.IsInfiniteSeedSlot())
            {
                slot.SetItem(heldSprite, heldItemName, 1);
                RemoveHeldItem();
                SaveInventory();
                return;
            }
        }

        Debug.Log("인벤토리에 빈 슬롯이 없습니다.");
    }

    public void PickUpFromSlot(InventorySlot slot)
    {
        if (!slot.HasItem()) return;

        heldSprite = slot.GetSprite();
        heldItemName = slot.GetItemName();
        heldItem = null;

        slot.ClearSlot();
        HeldItemManager.Instance.ShowHeldItem(heldSprite, heldItemName);
        SFXManager.Instance.PlayBbyongSFX();
        SaveInventory();
    }

    public void SaveInventory()
    {
        var data = new InventorySaveData();
        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            // 무한 슬롯은 저장 제외
            if (slot.IsInfiniteSeedSlot()) continue;

            data.items.Add(new InventorySlotData
            {
                itemName = slot.HasItem() ? slot.GetItemName() : "",
                count = slot.HasItem() ? slot.GetItemCount() : 0
            });
        }

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(savePath, json);
    }

    public void LoadInventory()
    {
        if (!File.Exists(savePath)) return;

        string json = File.ReadAllText(savePath);
        var data = JsonUtility.FromJson<InventorySaveData>(json);

        for (int i = 0; i < slots.Count && i < data.items.Count; i++)
        {
            var item = data.items[i];
            if (!string.IsNullOrEmpty(item.itemName) && item.count > 0)
            {
                Sprite sprite = Resources.Load<Sprite>("Sprites/SeedBags/" + item.itemName);
                slots[i].SetItem(sprite, item.itemName, item.count);
            }
            else
            {
                slots[i].ClearSlot();
            }
        }
    }

    public Sprite GetHeldSprite() => heldSprite;

    public void HoldItemFromSlot(Sprite sprite, string itemName)
    {
        heldSprite = sprite;
        heldItemName = itemName;
        heldItem = null;
        HeldItemManager.Instance.ShowHeldItem(heldSprite, heldItemName);
        SFXManager.Instance.PlayBbyongSFX();
    }

    public void TryAutoStoreHeldItem()
    {
        if (heldSprite == null || string.IsNullOrEmpty(heldItemName)) return;
        if (ToolData.Instance.IsTool(heldItemName)) return;

        foreach (var slot in slots)
        {
            if (slot.HasItem() && slot.GetItemName() == heldItemName && !slot.IsInfiniteSeedSlot())
            {
                slot.SetItem(slot.GetSprite(), heldItemName, slot.GetItemCount() + 1);
                RemoveHeldItem();
                SaveInventory();
                return;
            }
        }

        foreach (var slot in slots)
        {
            if (!slot.HasItem() && !slot.IsInfiniteSeedSlot())
            {
                slot.SetItem(heldSprite, heldItemName, 1);
                RemoveHeldItem();
                SaveInventory();
                return;
            }
        }
    }

    // 무한 씨앗 슬롯 세팅
    private void SetupInfiniteSeedSlots()
    {
        // 고정 순서: 쌀 모종, 쑥 씨앗, 단호박, 백년초
        string[] seedNames = { "Rice_seedBag", "Mugwort_seedBag", "Danhobak_seedBag", "Baeknyeoncho_seedBag" };

        for (int i = 0; i < 4 && i < slots.Count; i++)
        {
            Sprite sprite = Resources.Load<Sprite>("Sprites/SeedBags/" + seedNames[i]);
            if (sprite != null)
                slots[i].SetItem(sprite, seedNames[i], -1); // -1 = 무한
        }
    }

    [System.Serializable]
    public class InventorySlotData
    {
        public string itemName;
        public int count;
    }

    [System.Serializable]
    public class InventorySaveData
    {
        public List<InventorySlotData> items = new();
    }
}

