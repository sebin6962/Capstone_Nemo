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

    private TableInfo nearbyTable;   // 탁자 감지용(Trigger/Collision에서 할당)
    //private bool requestCrafting = false;
    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        // E키
    if (Input.GetKeyDown(interactKey))
    {
        // 1. 상자(창고) 인벤토리가 열려 있고, 플레이어가 상자와 닿아있을 때 E키 → UI 닫기
        if (nearbyStorage != null && PlayerStoreBoxInventoryUIManager.Instance.IsOpen())
        {
            PlayerStoreBoxInventoryUIManager.Instance.CloseUI();
            Debug.Log("[E] 상자 인벤토리 닫힘");
            return;
        }

        // 2. 상자(창고)에 닿아 있고, UI가 닫혀 있을 때 → E키로 열기
        if (nearbyStorage != null)
        {
            PlayerStoreBoxInventoryUIManager.Instance.OpenUI(nearbyStorage);
            Debug.Log("[E] 상자 인벤토리 열기");
            return;
        }

        // 3. 제작기 근처에 있을 때
        if (isNearMaker && currentMaker != null)
        {
            // (1) 제작기에 완성된 결과물이 있는 경우
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
                        Debug.Log($"[E] 결과물 {resultName} 소지 시작");

                        SFXManager.Instance.PlayBbyongSFX();
                    }
                }
                else
                {
                    Debug.Log("이미 들고 있는 아이템이 있습니다! 결과물 소지 불가.");
                }
                return;
            }

                // (2) 제작기에 결과물이 없고, 플레이어가 아이템을 들고 있다면 → 재료 투입(최대 4개)
                if (HeldItemManager.Instance.IsHoldingItem())
                {
                    if (currentMaker.inputItemNames.Count >= 4)
                    {
                        Debug.Log("제작기 재료 슬롯이 가득 찼습니다! (최대 4개)");
                        return;
                    }
                    string heldItemName = HeldItemManager.Instance.GetHeldItemName();
                    Sprite heldItemSprite = HeldItemManager.Instance.GetHeldItemSprite();

                    //완성품 제작대에 올라가지 않음
                    if (heldItemName.EndsWith("finish"))
                    {
                        Debug.Log($"[CraftingTable] 완성된 아이템({heldItemName})은 제작대와 상호작용하지 않음");
                        return;
                    }

                    currentMaker.inputItemNames.Add(heldItemName);
                    currentMaker.inputItemSprites.Add(heldItemSprite);

                    // 재료 넣을 때 슬롯UI가 없으면 자동 생성(클론) & 위치 지정 & 활성화
                    currentMaker.ActivateSlotUI();
                    if (currentMaker.slotUIManager != null)
                        currentMaker.slotUIManager.UpdateSlots(currentMaker.inputItemSprites);

                    HeldItemManager.Instance.HideHeldItem();
                    Debug.Log($"[E] {heldItemName} 제작기에 투입, 총 {currentMaker.inputItemNames.Count}/4");
                    SFXManager.Instance.PlayBbyongSFX();
                    return;
                }
        }

            // 4. 탁자에 아이템 놓기
            if (nearbyTable != null && HeldItemManager.Instance.IsHoldingItem())
            {
                if (nearbyTable.currentPlacedObject != null)
                {
                    Debug.Log("탁자 위에 이미 아이템이 있습니다!");
                    return;
                }

                Sprite heldSprite = HeldItemManager.Instance.GetHeldItemSprite();
                string heldName = HeldItemManager.Instance.GetHeldItemName();

                // Spot 위치에 아이템 오브젝트 생성
                GameObject tableItemObj = new GameObject("TableItem");
                SpriteRenderer sr = tableItemObj.AddComponent<SpriteRenderer>();
                sr.sprite = heldSprite;

                // Sorting Layer/Order를 탁자보다 높게 설정
                sr.sortingLayerName = "Obj";   // 원하는 Sorting Layer 이름
                sr.sortingOrder = 100;              // 탁자 SpriteRenderer보다 더 큰 값

                // 아이템 크기 조정 (예: 0.6배로 줄이기)
                tableItemObj.transform.localScale = new Vector3(0.6f, 0.6f, 1f);

                // Spot 위치에 배치
                tableItemObj.transform.position = nearbyTable.itemSpot.position;

                nearbyTable.currentPlacedObject = tableItemObj;

                HeldItemManager.Instance.HideHeldItem();

                SFXManager.Instance.PlayBbyongSFX();
                Debug.Log($"[E] {heldName}을(를) 탁자 위에 놓음");
                return;
            }

            // 4-1.탁자에서 아이템 회수(들고 있지 않은 상태에서 E키)
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
                        Debug.Log($"[E] 탁자에서 {tableName}을(를) 집음");
                    }
                    return;
                }
            }
        }

        // Space키: 제작 시도 (제작기 근처에서만)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // 1. 상자(창고) 인벤토리가 열려 있고, 플레이어가 아이템을 들고 있다면
            if (PlayerStoreBoxInventoryUIManager.Instance.IsOpen() &&
                HeldItemManager.Instance.IsHoldingItem())
            {
                string heldItemName = HeldItemManager.Instance.GetHeldItemName();

                StorageInventory.Instance.AddItem(heldItemName, 1);
                StorageInventory.Instance.SaveStorage();

                HeldItemManager.Instance.HideHeldItem();

                PlayerStoreBoxInventoryUIManager.Instance.UpdateSlots();
                SFXManager.Instance.PlayBbyongSFX();
                Debug.Log($"[Space] {heldItemName} 1개를 상자에 보관함");
                return;
            }

            // 2. 제작기 근처에서 재료가 1개 이상 쌓인 경우에만 제작 시도
            if (isNearMaker && currentMaker != null && currentMaker.inputItemNames.Count > 0)
            {
                var recipeSet = new HashSet<string>(currentMaker.inputItemNames);
                Sprite resultSprite = CraftingRecipeManager.Instance.GetResultSprite(currentMaker.makerId, recipeSet);

                if (resultSprite != null)
                {
                    // 제작 시작 시 슬롯 UI 비활성화 (여러 제작기 독립)
                    currentMaker.DeactivateSlotUI();

                    Debug.Log("[Space] 제작 성공, 결과: " + resultSprite.name);

                    // 진행바 + 결과 생성
                    StartCoroutine(currentMaker.ShowProgressAndSpawnItem(resultSprite));

                    // 인풋 인벤토리, 슬롯 UI 초기화
                    currentMaker.inputItemNames.Clear();
                    currentMaker.inputItemSprites.Clear();
                    if (currentMaker.slotUIManager != null)
                        currentMaker.slotUIManager.ClearSlots();
                }
                else
                {
                    Debug.LogWarning("[Space] 제작 실패: 레시피 매칭 실패");
                }
                return;
            }
        }
    }

    //private IEnumerator DelayedCraftingRoutine()
    //{
    //    yield return null; // 1프레임 대기

    //    if (currentMaker != null)
    //    {
    //        Debug.Log($"[지연된 제작 시도] makerId: {currentMaker.makerId}");

    //        // 1. 플레이어가 들고 있는 아이템 이름만 가져옴
    //        string heldItemName = HeldItemManager.Instance.GetHeldItemName();

    //        // 2. 레시피 인자 준비 (항상 한 가지 아이템만)
    //        var recipeSet = new HashSet<string>();
    //        if (!string.IsNullOrEmpty(heldItemName))
    //            recipeSet.Add(heldItemName);

    //        // 3. 실제 제작 실행
    //        Sprite resultSprite = CraftingRecipeManager.Instance.GetResultSprite(currentMaker.makerId, recipeSet);

    //        if (resultSprite != null)
    //        {
    //            // 제작 성공!
    //            Debug.Log("[제작 성공] 결과: " + resultSprite.name);

    //            // 소지 아이템 소모
    //            HeldItemManager.Instance.HideHeldItem();

    //            // 결과 오브젝트 스폰 
    //            StartCoroutine(currentMaker.ShowProgressAndSpawnItem(resultSprite));
    //        }
    //        else
    //        {
    //            Debug.LogWarning("[제작 실패] 레시피 없음/매칭 실패");
    //            // 소지 아이템 유지
    //        }
    //    }
    //    else
    //    {
    //        Debug.LogWarning("[지연된 제작 실패] currentMaker가 null입니다");
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
            Debug.Log($"접근: {currentMaker.makerId}");
        }

        if (other.CompareTag("StorageBox")) // 꼭 Tag 설정 필요
        {
            nearbyStorage = other.GetComponent<StorageInventory>();
        }

        if (other.CompareTag("Table"))
        {
            nearbyTable = other.GetComponent<TableInfo>();
            Debug.Log($"테이블 접근");
        }
            
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var maker = other.GetComponent<MakerInfo>();
        if (maker != null && currentMaker == maker)
        {
            isNearMaker = false;
            currentMaker = null;
            Debug.Log($"이탈: {maker.makerId}");
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
