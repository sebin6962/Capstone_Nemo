using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MillManager : MonoBehaviour
{
    public GameObject MillPanel;
    public Transform inventoryPanelParent;
    public GameObject SlotPrefab;
    public SelectedSlot selectedSlot;
    public Sprite[] testIcons;
    public Button confirmButton;

    private MillItemData selectedItem = null;
    private List<MillItemData> Inventory;

    [SerializeField] private Image ResultEffectImage;
    [SerializeField] private float displayDuration = 0.7f;

    void Start()
    {
        /*Inventory = new List<MillItemData>
        {
            new MillItemData(*//*"쌀",*//* testIcons[0], 3),
            new MillItemData(*//*"찹쌀",*//* testIcons[1], 5),
            new MillItemData(*//*"단호박",*//* testIcons[2], 2)
        };*/

        Inventory = new List<MillItemData>();

        var storageItems = StorageInventory.Instance.GetAllItems();

        foreach (var pair in storageItems)
        {
            string itemName = pair.Key;
            int itemCount = pair.Value;

            //가루 변환 가능한 아이템 필터링
            if (!IsMillable(itemName)) continue;

            Sprite icon = Resources.Load<Sprite>("Sprites/Ingredients/" + itemName);
            if (icon == null)
            {
                Debug.LogWarning("[MillManager] 아이템 스프라이트 없음: " + itemName);
                continue;
            }

            Inventory.Add(new MillItemData(itemName, icon, itemCount));
        }

        confirmButton.onClick.AddListener(Confirm);
        confirmButton.interactable = false;
        OpenMill();
    }
    public void OpenMill()
    {
        gameObject.SetActive(true);

        foreach (Transform child in inventoryPanelParent)
            Destroy(child.gameObject);

        foreach (var item in Inventory)
        {
            var obj = Instantiate(SlotPrefab, inventoryPanelParent);
            obj.GetComponent<MillInventory>().Setup(item, this);
        }

        selectedItem = null;
        selectedSlot.Clear();

    }

    private bool IsMillable(string itemName)
    {
        //가루로 만들 수 있는 재료
        return itemName == "Danhobak" || itemName == "Baeknyeoncho" || itemName == "Mugwort" || itemName == "cinnamon";
    }


    public void SelectItem(MillItemData item)
    {
        if (ReferenceEquals(selectedItem, item))
        {
            item.itemQuantity += 1;
            selectedItem = null;
            selectedSlot.Clear();
            UpdateInventoryUI();
            confirmButton.interactable = false;
            return;
        }

        if (item.itemQuantity <= 0)
            return;

        if (selectedItem != null)
            selectedItem.itemQuantity += 1;

        item.itemQuantity -= 1;
        selectedItem = item;
        selectedSlot.Set(item);
        UpdateInventoryUI();
        confirmButton.interactable = true;
    }

    private void UpdateInventoryUI()
    {
        foreach (Transform child in inventoryPanelParent)
        {
            var slot = child.GetComponent<MillInventory>();
            slot?.UpdateQuantityText();
        }
    }



    public void Confirm()
    {
        if (selectedItem == null)
            return;

        string sourceName = selectedItem.itemName;
        if (!MillDB.GrindResult.TryGetValue(sourceName, out string resultName))
        {
            return;
        }

        StorageInventory.Instance.AddItem(sourceName, -1);
        StorageInventory.Instance.AddItem(resultName, 1);
        StorageInventory.Instance.SaveStorage();
        Debug.Log($"{sourceName} → {resultName}로 변환");

        Sprite resultSprite = Resources.Load<Sprite>("Sprites/Ingredients/" + resultName);
        if (resultSprite != null)
            ShowResultEffect(resultSprite);

        else
            Debug.LogWarning($"[MillManager] 스프라이트 로드 실패: {resultName}");

        selectedItem = null;
        selectedSlot.Clear();
        UpdateInventoryUI();
        confirmButton.interactable = false;

    }

    public void ShowResultEffect(Sprite sprite)
    {
        ResultEffectImage.sprite = sprite;
        ResultEffectImage.gameObject.SetActive(true);
        StartCoroutine(HideResultEffectAfterDelay());
    }

    IEnumerator HideResultEffectAfterDelay()
    {
        yield return new WaitForSeconds(displayDuration);
        ResultEffectImage.gameObject.SetActive(false);
    }

    public void CloseMill()
    {
        if (selectedItem != null)
        {
            selectedItem.itemQuantity += 1;
            selectedSlot.Clear();
            selectedItem = null;
            UpdateInventoryUI();
        }
        MillPanel.SetActive(false);
    }
}
