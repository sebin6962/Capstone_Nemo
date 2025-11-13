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

    private BoxObject nearbyBox;
    private SinkInfo nearbySink;
    private TrashCanInfo nearbyTrash;

    public GameObject storageFullPanel;     // "창고가 가득 찼습니다" 패널
    public CanvasGroup storageFullGroup;    // 위 패널에 붙은 CanvasGroup
    private Coroutine storageFullCo;        // 중복 실행 방지

    private static readonly HashSet<string> NonDiscardableItems = new HashSet<string>
{
    "YakgwaMold",
    "JeolpyeonMold"// 보호 아이템: 버려지지 않음
};

    private void Awake()
    {
        Instance = this;
    }

    private static readonly HashSet<string> NonIngredientTools = new HashSet<string>
    {
        "YakgwaMold",
        "JeolpyeonMold"// 틀 이름
    };

    private void Update()
    {
        // E키
        if (Input.GetKeyDown(interactKey))
        {
            // 1. 상자(창고) 인벤토리가 열려 있고, 플레이어가 상자와 닿아있을 때 E키 → UI 닫기
            if (nearbyBox != null && PlayerStoreBoxInventoryUIManager.Instance.IsOpen())
            {
                PlayerStoreBoxInventoryUIManager.Instance.CloseUI();
                Debug.Log("[E] 상자 인벤토리 닫힘");
                return;
            }

            // 2. 상자(창고)에 닿아 있고, UI가 닫혀 있을 때 → E키로 열기
            if (nearbyBox != null)
            {
                PlayerStoreBoxInventoryUIManager.Instance.OpenUI(nearbyStorage);
                Debug.Log("[E] 상자 인벤토리 열기");
                return;
            }

            // 쓰레기통과 닿아있고, 아이템을 손에 든 상태일 때 E키 → 아이템 폐기
            if (nearbyTrash != null && HeldItemManager.Instance.IsHoldingItem())
            {
                string heldName = HeldItemManager.Instance.GetHeldItemName();

                // 보호 아이템은 버려지지 않음
                if (NonDiscardableItems.Contains(heldName))
                {
                    Debug.Log($"[Trash] 보호 아이템({heldName})은(는) 버릴 수 없음");
                    SFXManager.Instance.PlayBbyongSFX();
                    return;
                }

                // 정상 폐기
                HeldItemManager.Instance.HideHeldItem();
                SFXManager.Instance.PlayTrashDiscardSFX();
                Debug.Log($"[Trash] {heldName} 아이템을 버림");
                return;
            }

            // 싱크: 빈손일 때 E키 → 진행바 → 물 지급
            if (nearbySink != null)
            {
                // 1) 싱크 위에 이미 물 결과물이 있고, 플레이어는 빈손인 경우 → 물 줍기
                if (nearbySink.HasWaterResult && !HeldItemManager.Instance.IsHoldingItem())
                {
                    nearbySink.PickupWaterResult();
                    return;
                }

                // 2) 아직 결과물이 없고, 빈손이며, 진행 중이 아닐 때 → 물 긷기 시작 (진행바 + 결과물 생성)
                if (!HeldItemManager.Instance.IsHoldingItem() &&
                    !nearbySink.IsRunning &&
                    !nearbySink.HasWaterResult)
                {
                    StartCoroutine(nearbySink.FillAndGiveWater());
                    return;
                }

                // 3) 그 외 (이미 뭔가 들고 있거나, 진행 중이거나, 결과물이 있는데 손에 뭔가 들고 있을 때)는 무시
            }

            if (isNearMaker && currentMaker != null && currentMaker.IsLocked())
            {
                Debug.Log("[E] 잠긴 제작기라 상호작용 불가");
                SFXManager.Instance.PlayBbyongSFX();
                return;
            }

            // 3. 제작기 근처에 있을 때
            if (isNearMaker && currentMaker != null)
            {
                Debug.Log($"[E] 제작기와 접촉: {currentMaker}");

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

                            //제작완료이펙트삭제
                            currentMaker.KillActiveEffect(0.5f);

                            var makerMgr = FindObjectOfType<MakerManager>();
                            if (makerMgr != null)
                                makerMgr.SaveMakerState();
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

                    if (NonIngredientTools.Contains(heldItemName))
                    {
                        Debug.Log($"[CraftingTable] 도구({heldItemName})는 재료로 투입되지 않음");
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

                    // 투입 직후 저장
                    var makerMgr = FindObjectOfType<MakerManager>();
                    if (makerMgr != null)
                        makerMgr.SaveMakerState();

                    return;
                }
            }

            // 4. 탁자에 아이템 놓기
            if (nearbyTable != null && HeldItemManager.Instance.IsHoldingItem())
            {
                if (nearbyTable.currentPlacedObject != null)
                {
                    Debug.Log("탁자 위에 이미 아이템이 있음");
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
                sr.sortingOrder = 60;              // 탁자 SpriteRenderer보다 더 큰 값

                // 아이템 크기 조정 (예: 0.6배로 줄이기)
                tableItemObj.transform.localScale = new Vector3(1f, 1f, 1f);

                // Spot 위치에 배치
                tableItemObj.transform.position = nearbyTable.itemSpot.position;

                nearbyTable.currentPlacedObject = tableItemObj;

                // --- 테이블 아이템 상태 저장 ---
                var tableMgr = FindObjectOfType<TableManager>();
                if (tableMgr != null)
                    tableMgr.SaveTableState();

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

                        // --- 테이블 아이템 상태 저장 ---
                        var tableMgr = FindObjectOfType<TableManager>();
                        if (tableMgr != null)
                            tableMgr.SaveTableState();

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
            if (isNearMaker && currentMaker != null)
            {
                if (currentMaker.IsLocked())
                {
                    Debug.Log("[Space] 잠긴 제작기라 제작 불가");
                    SFXManager.Instance.PlayBbyongSFX();
                    return;
                }
            }

            // 1. 상자(창고) 인벤토리가 열려 있고, 플레이어가 아이템을 들고 있다면
            if (PlayerStoreBoxInventoryUIManager.Instance.IsOpen() &&
                HeldItemManager.Instance.IsHoldingItem())
            {
                string heldItemName = HeldItemManager.Instance.GetHeldItemName();

                // 초기 테이블 아이템은 상자에 못 넣게 방어
                if (TableInitialItemHelper.IsInitialTableItemName(heldItemName))
                {
                    Debug.Log($"[Space] 초기 테이블 아이템({heldItemName})은 상자에 넣을 수 없습니다.");
                    SFXManager.Instance.PlayBbyongSFX();
                    return;
                }

                // 시도 후 실패하면 메시지만 출력
                if (!StorageInventory.Instance.TryAddItem(heldItemName, 1))
                {
                    int currentCount = StorageInventory.Instance.GetItemCount(heldItemName);
                    bool hasThisItem = currentCount > 0;

                    if (hasThisItem && currentCount >= StorageInventory.Instance.maxStackPerItem)
                    {
                        ShowStorageFull();
                        Debug.Log($"[Space] {heldItemName} 스택이 최대치({StorageInventory.Instance.maxStackPerItem})라 더 넣을 수 없습니다.");
                    }
                    else if (!hasThisItem && StorageInventory.Instance.FreeSlots <= 0)
                    {
                        ShowStorageFull();
                        Debug.Log("[Space] 상자 슬롯이 가득 차서 더 이상 아이템을 보관할 수 없습니다.");
                    }
                    else
                    {
                        Debug.Log("[Space] 상자에 아이템을 넣을 수 없습니다. (TryAddItem 실패)");
                    }

                    SFXManager.Instance.PlayBbyongSFX();
                    return;
                }

                //StorageInventory.Instance.AddItem(heldItemName, 1);
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
                bool isRecipeMatched;

                // 약과 전용: ShapeMaker에서 Yakgwabanjuk을 만들려면 YakgwaMold를 들고 있어야 함
                bool requiresYakgwaMold =
                    currentMaker.makerId == "ShapeMaker"
                    && currentMaker.inputItemNames.Count == 1
                    && currentMaker.inputItemNames.Contains("Yakgwabanjuk");

                bool hasYakgwaMoldInHand = HeldItemManager.Instance.IsHoldingItem()
                                           && HeldItemManager.Instance.GetHeldItemName() == "YakgwaMold";

                // 절편 전용: ShapeMaker에서 Jeolpyeon_finish을 만들려면 JeolpyeonMold를 들고 있어야 함
                bool requiresJeolpyeonMold =
                    currentMaker.makerId == "ShapeMaker"
                    && currentMaker.inputItemNames.Count == 1
                    && currentMaker.inputItemNames.Contains("Mixing_Mepssal_Hot");

                bool hasJeolpyeonMoldInHand = HeldItemManager.Instance.IsHoldingItem()
                                           && HeldItemManager.Instance.GetHeldItemName() == "JeolpyeonMold";

                if (requiresYakgwaMold && !hasYakgwaMoldInHand)
                {
                    // 틀 미보유 → 강제 실패 결과 스폰
                    // (레시피 매칭 대신 실패 스프라이트 로드)
                    currentMaker.DeactivateSlotUI();

                    Sprite fail = Resources.Load<Sprite>("Sprites/Ingredients/FailRiceCake_finish");
                    StartCoroutine(currentMaker.ShowProgressAndSpawnItem(fail));

                    currentMaker.inputItemNames.Clear();
                    currentMaker.inputItemSprites.Clear();
                    if (currentMaker.slotUIManager != null)
                        currentMaker.slotUIManager.ClearSlots();

                    Debug.Log("[Space] Yakgwabanjuk 이지만 YakgwaMold 미보유 → 실패 처리");
                    return;
                }

                if (requiresJeolpyeonMold && !hasJeolpyeonMoldInHand)
                {
                    // 틀 미보유 → 강제 실패 결과 스폰
                    // (레시피 매칭 대신 실패 스프라이트 로드)
                    currentMaker.DeactivateSlotUI();

                    Sprite fail = Resources.Load<Sprite>("Sprites/Ingredients/FailRiceCake_finish");
                    StartCoroutine(currentMaker.ShowProgressAndSpawnItem(fail));

                    currentMaker.inputItemNames.Clear();
                    currentMaker.inputItemSprites.Clear();
                    if (currentMaker.slotUIManager != null)
                        currentMaker.slotUIManager.ClearSlots();

                    Debug.Log("[Space] JeolpyeonMold 미보유 → 실패 처리");
                    return;
                }

                var recipeSet = new HashSet<string>(currentMaker.inputItemNames);
                Sprite resultSprite = CraftingRecipeManager.Instance.GetResultSprite(currentMaker.makerId, recipeSet, out isRecipeMatched);


                // 제작 시작 시 슬롯 UI 비활성화 (여러 제작기 독립)
                currentMaker.DeactivateSlotUI();

                Debug.Log("[Space] 제작 성공, 결과: " + resultSprite.name);

                //// 진행바 + 결과 생성
                //StartCoroutine(currentMaker.ShowProgressAndSpawnItem(resultSprite));

                //// 인풋 인벤토리, 슬롯 UI 초기화
                //currentMaker.inputItemNames.Clear();
                //currentMaker.inputItemSprites.Clear();
                //if (currentMaker.slotUIManager != null)
                //    currentMaker.slotUIManager.ClearSlots();

                float duration = 3f; // 기존에 쓰던 제작 시간
                currentMaker.StartCraft(resultSprite, duration);

                //var makerMgr = FindObjectOfType<MakerManager>();
                //if (makerMgr != null)
                //    makerMgr.SaveMakerState();

                if (isRecipeMatched)
                    Debug.Log("[Space] 제작 성공, 결과: " + resultSprite.name);
                else
                    Debug.Log("[Space] 레시피 없음 → 랜덤으로 꽃다발 or 망한떡 생성됨, 결과: " + resultSprite.name);

                return;
            }
        }
    }

    public void ShowStorageFull()
    {
        if (storageFullCo != null) StopCoroutine(storageFullCo);
        storageFullCo = StartCoroutine(StorageFullRoutine());
    }

    private IEnumerator StorageFullRoutine()
    {
        storageFullPanel.SetActive(true);

        float duration = 0.5f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            storageFullGroup.alpha = Mathf.Lerp(0f, 1f, t / duration);
            yield return null;
        }
        storageFullGroup.alpha = 1f;

        yield return new WaitForSeconds(1f);

        t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            storageFullGroup.alpha = Mathf.Lerp(1f, 0f, t / duration);
            yield return null;
        }
        storageFullGroup.alpha = 0f;

        storageFullPanel.SetActive(false);
        storageFullCo = null;
    }
    //private IEnumerator DelayedCraftingRoutine()
    //{
    //    yield return null; // 1프레임 대기
    //
    //    if (currentMaker != null)
    //    {
    //        Debug.Log($"[지연된 제작 시도] makerId: {currentMaker.makerId}");
    //
    //        // 1. 플레이어가 들고 있는 아이템 이름만 가져옴
    //        string heldItemName = HeldItemManager.Instance.GetHeldItemName();
    //
    //        // 2. 레시피 인자 준비 (항상 한 가지 아이템만)
    //        var recipeSet = new HashSet<string>();
    //        if (!string.IsNullOrEmpty(heldItemName))
    //            recipeSet.Add(heldItemName);
    //
    //        // 3. 실제 제작 실행
    //        Sprite resultSprite = CraftingRecipeManager.Instance.GetResultSprite(currentMaker.makerId, recipeSet);
    //
    //        if (resultSprite != null)
    //        {
    //            // 제작 성공!
    //            Debug.Log("[제작 성공] 결과: " + resultSprite.name);
    //
    //            // 소지 아이템 소모
    //            HeldItemManager.Instance.HideHeldItem();
    //
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
            nearbyBox = other.GetComponent<BoxObject>();
        }

        if (other.CompareTag("Table"))
        {
            nearbyTable = other.GetComponent<TableInfo>();
            Debug.Log($"테이블 접근");
        }

        var sink = other.GetComponent<SinkInfo>();
        if (sink != null)
        {
            nearbySink = sink;
            Debug.Log("[PlayerInteract] 싱크 접근");
        }

        var trash = other.GetComponent<TrashCanInfo>();
        if (trash != null)
        {
            nearbyTrash = trash;
            Debug.Log("[PlayerInteract] 쓰레기통 접근");
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
            if (nearbyBox == other.GetComponent<BoxObject>())
                nearbyBox = null;
        }

        if (other.CompareTag("Table") && other.GetComponent<TableInfo>() == nearbyTable)
            nearbyTable = null;

        if (other.GetComponent<SinkInfo>() == nearbySink)
        {
            nearbySink = null;
            Debug.Log("[PlayerInteract] 싱크 이탈");
        }

        if (other.GetComponent<TrashCanInfo>() == nearbyTrash)
        {
            nearbyTrash = null;
            Debug.Log("[PlayerInteract] 쓰레기통 이탈");
        }
    }

}

