using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    public static PlayerInteract Instance;
    public KeyCode interactKey = KeyCode.E;

    private bool isNearMaker = false;
    public MakerInfo currentMaker;

    private StorageInventory nearbyStorage;

    private TableInfo nearbyTable;   // Ź�� ������(Trigger/Collision���� �Ҵ�)
    //private bool requestCrafting = false;
    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        // EŰ
    if (Input.GetKeyDown(interactKey))
    {
        // 1. ����(â��) �κ��丮�� ���� �ְ�, �÷��̾ ���ڿ� ������� �� EŰ �� UI �ݱ�
        if (nearbyStorage != null && PlayerStoreBoxInventoryUIManager.Instance.IsOpen())
        {
            PlayerStoreBoxInventoryUIManager.Instance.CloseUI();
            Debug.Log("[E] ���� �κ��丮 ����");
            return;
        }

        // 2. ����(â��)�� ��� �ְ�, UI�� ���� ���� �� �� EŰ�� ����
        if (nearbyStorage != null)
        {
            PlayerStoreBoxInventoryUIManager.Instance.OpenUI(nearbyStorage);
            Debug.Log("[E] ���� �κ��丮 ����");
            return;
        }

        // 3. ���۱� ��ó�� ���� ��
        if (isNearMaker && currentMaker != null)
        {
            // (1) ���۱⿡ �ϼ��� ������� �ִ� ���
            if (currentMaker.currentResultObject != null)
            {
                if (!HeldItemManager.Instance.IsHoldingItem())
                {
                    var sr = currentMaker.currentResultObject.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        Sprite resultSprite = sr.sprite;
                        string resultName = resultSprite.name;
                        HeldItemManager.Instance.ShowHeldItem(resultSprite, resultName);
                        Destroy(currentMaker.currentResultObject);
                        currentMaker.currentResultObject = null;
                        Debug.Log($"[E] ����� {resultName} ���� ����");

                        SFXManager.Instance.PlayBbyongSFX();
                    }
                }
                else
                {
                    Debug.Log("�̹� ��� �ִ� �������� �ֽ��ϴ�! ����� ���� �Ұ�.");
                }
                return;
            }

                // (2) ���۱⿡ ������� ����, �÷��̾ �������� ��� �ִٸ� �� ��� ����(�ִ� 4��)
                if (HeldItemManager.Instance.IsHoldingItem())
                {
                    if (currentMaker.inputItemNames.Count >= 4)
                    {
                        Debug.Log("���۱� ��� ������ ���� á���ϴ�! (�ִ� 4��)");
                        return;
                    }
                    string heldItemName = HeldItemManager.Instance.GetHeldItemName();
                    Sprite heldItemSprite = HeldItemManager.Instance.GetHeldItemSprite();

                    //�ϼ�ǰ ���۴뿡 �ö��� ����
                    if (heldItemName.EndsWith("finish"))
                    {
                        Debug.Log($"[CraftingTable] �ϼ��� ������({heldItemName})�� ���۴�� ��ȣ�ۿ����� ����");
                        return;
                    }

                    currentMaker.inputItemNames.Add(heldItemName);
                    currentMaker.inputItemSprites.Add(heldItemSprite);

                    // ��� ���� �� ����UI�� ������ �ڵ� ����(Ŭ��) & ��ġ ���� & Ȱ��ȭ
                    currentMaker.ActivateSlotUI();
                    if (currentMaker.slotUIManager != null)
                        currentMaker.slotUIManager.UpdateSlots(currentMaker.inputItemSprites);

                    HeldItemManager.Instance.HideHeldItem();
                    Debug.Log($"[E] {heldItemName} ���۱⿡ ����, �� {currentMaker.inputItemNames.Count}/4");
                    SFXManager.Instance.PlayBbyongSFX();
                    return;
                }
        }

            // 4. Ź�ڿ� ������ ����
            if (nearbyTable != null && HeldItemManager.Instance.IsHoldingItem())
            {
                if (nearbyTable.currentPlacedObject != null)
                {
                    Debug.Log("Ź�� ���� �̹� �������� �ֽ��ϴ�!");
                    return;
                }

                Sprite heldSprite = HeldItemManager.Instance.GetHeldItemSprite();
                string heldName = HeldItemManager.Instance.GetHeldItemName();

                // Spot ��ġ�� ������ ������Ʈ ����
                GameObject tableItemObj = new GameObject("TableItem");
                SpriteRenderer sr = tableItemObj.AddComponent<SpriteRenderer>();
                sr.sprite = heldSprite;

                // Sorting Layer/Order�� Ź�ں��� ���� ����
                sr.sortingLayerName = "Obj";   // ���ϴ� Sorting Layer �̸�
                sr.sortingOrder = 100;              // Ź�� SpriteRenderer���� �� ū ��

                // ������ ũ�� ���� (��: 0.6��� ���̱�)
                tableItemObj.transform.localScale = new Vector3(0.6f, 0.6f, 1f);

                // Spot ��ġ�� ��ġ
                tableItemObj.transform.position = nearbyTable.itemSpot.position;

                nearbyTable.currentPlacedObject = tableItemObj;

                HeldItemManager.Instance.HideHeldItem();

                SFXManager.Instance.PlayBbyongSFX();
                Debug.Log($"[E] {heldName}��(��) Ź�� ���� ����");
                return;
            }

            // 4-1.Ź�ڿ��� ������ ȸ��(��� ���� ���� ���¿��� EŰ)
            if (nearbyTable != null && !HeldItemManager.Instance.IsHoldingItem())
            {
                if (nearbyTable.currentPlacedObject != null)
                {
                    SpriteRenderer sr = nearbyTable.currentPlacedObject.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        Sprite tableSprite = sr.sprite;
                        string tableName = tableSprite.name;

                        HeldItemManager.Instance.ShowHeldItem(tableSprite, tableName);

                        Destroy(nearbyTable.currentPlacedObject);
                        nearbyTable.currentPlacedObject = null;

                        SFXManager.Instance.PlayBbyongSFX();
                        Debug.Log($"[E] Ź�ڿ��� {tableName}��(��) ����");
                    }
                    return;
                }
            }
        }

        // SpaceŰ: ���� �õ� (���۱� ��ó������)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // 1. ����(â��) �κ��丮�� ���� �ְ�, �÷��̾ �������� ��� �ִٸ�
            if (PlayerStoreBoxInventoryUIManager.Instance.IsOpen() &&
                HeldItemManager.Instance.IsHoldingItem())
            {
                string heldItemName = HeldItemManager.Instance.GetHeldItemName();

                StorageInventory.Instance.AddItem(heldItemName, 1);
                StorageInventory.Instance.SaveStorage();

                HeldItemManager.Instance.HideHeldItem();

                PlayerStoreBoxInventoryUIManager.Instance.UpdateSlots();
                SFXManager.Instance.PlayBbyongSFX();
                Debug.Log($"[Space] {heldItemName} 1���� ���ڿ� ������");
                return;
            }

            // 2. ���۱� ��ó���� ��ᰡ 1�� �̻� ���� ��쿡�� ���� �õ�
            if (isNearMaker && currentMaker != null && currentMaker.inputItemNames.Count > 0)
            {
                var recipeSet = new HashSet<string>(currentMaker.inputItemNames);
                Sprite resultSprite = CraftingRecipeManager.Instance.GetResultSprite(currentMaker.makerId, recipeSet);

                if (resultSprite != null)
                {
                    // ���� ���� �� ���� UI ��Ȱ��ȭ (���� ���۱� ����)
                    currentMaker.DeactivateSlotUI();

                    Debug.Log("[Space] ���� ����, ���: " + resultSprite.name);

                    // ����� + ��� ����
                    StartCoroutine(currentMaker.ShowProgressAndSpawnItem(resultSprite));

                    // ��ǲ �κ��丮, ���� UI �ʱ�ȭ
                    currentMaker.inputItemNames.Clear();
                    currentMaker.inputItemSprites.Clear();
                    if (currentMaker.slotUIManager != null)
                        currentMaker.slotUIManager.ClearSlots();
                }
                else
                {
                    Debug.LogWarning("[Space] ���� ����: ������ ��Ī ����");
                }
                return;
            }
        }
    }

    //private IEnumerator DelayedCraftingRoutine()
    //{
    //    yield return null; // 1������ ���

    //    if (currentMaker != null)
    //    {
    //        Debug.Log($"[������ ���� �õ�] makerId: {currentMaker.makerId}");

    //        // 1. �÷��̾ ��� �ִ� ������ �̸��� ������
    //        string heldItemName = HeldItemManager.Instance.GetHeldItemName();

    //        // 2. ������ ���� �غ� (�׻� �� ���� �����۸�)
    //        var recipeSet = new HashSet<string>();
    //        if (!string.IsNullOrEmpty(heldItemName))
    //            recipeSet.Add(heldItemName);

    //        // 3. ���� ���� ����
    //        Sprite resultSprite = CraftingRecipeManager.Instance.GetResultSprite(currentMaker.makerId, recipeSet);

    //        if (resultSprite != null)
    //        {
    //            // ���� ����!
    //            Debug.Log("[���� ����] ���: " + resultSprite.name);

    //            // ���� ������ �Ҹ�
    //            HeldItemManager.Instance.HideHeldItem();

    //            // ��� ������Ʈ ���� 
    //            StartCoroutine(currentMaker.ShowProgressAndSpawnItem(resultSprite));
    //        }
    //        else
    //        {
    //            Debug.LogWarning("[���� ����] ������ ����/��Ī ����");
    //            // ���� ������ ����
    //        }
    //    }
    //    else
    //    {
    //        Debug.LogWarning("[������ ���� ����] currentMaker�� null�Դϴ�");
    //    }
    //}

    public bool IsNearMaker()
    {
        return isNearMaker;
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        var maker = other.GetComponent<MakerInfo>();
        if (maker != null)
        {
            currentMaker = maker;
            isNearMaker = true;
            Debug.Log($"����: {currentMaker.makerId}");
        }

        if (other.CompareTag("StorageBox")) // �� Tag ���� �ʿ�
        {
            nearbyStorage = other.GetComponent<StorageInventory>();
        }

        if (other.CompareTag("Table"))
        {
            nearbyTable = other.GetComponent<TableInfo>();
            Debug.Log($"���̺� ����");
        }
            
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var maker = other.GetComponent<MakerInfo>();
        if (maker != null && currentMaker == maker)
        {
            isNearMaker = false;
            currentMaker = null;
            Debug.Log($"��Ż: {maker.makerId}");
        }

        if (other.CompareTag("StorageBox"))
        {
            if (nearbyStorage == other.GetComponent<StorageInventory>())
                nearbyStorage = null;
        }

        if (other.CompareTag("Table") && other.GetComponent<TableInfo>() == nearbyTable)
            nearbyTable = null;
    }
}
