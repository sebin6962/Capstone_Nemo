using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MillManager : MonoBehaviour
{
    public static MillManager Instance;

    public GameObject MillPanel;
    public Transform inventoryPanelParent;
    public GameObject inventoryPanel;
    public GameObject SlotPrefab;
    public SelectedSlot selectedSlot;
    public Sprite[] testIcons;
    public Button confirmButton;
    public Button closeButton;

    public GraphicRaycaster raycaster;


    private int queuedCount = 0;

    private MillItemData selectedItem = null;
    private List<MillItemData> Inventory;

    [SerializeField] private Image ResultEffectImage;
    [SerializeField] private Image ResultEffectCountImage;
    [SerializeField] private TMP_Text ResultEffectText;
    [SerializeField] private float displayDuration = 0.7f;

    [Header("애니메이션(추가)")]
    [SerializeField] private GameObject jeolguUI;
    [SerializeField] private Animator jeolguAnimator;
    [SerializeField] private AnimationClip millClip;   // 절구 재생 클립
    [SerializeField] private string millTriggerName = "Play"; // Animator 이름
    [SerializeField] private bool useAnimationEvent = false;  // 이벤트로 끝 처리할지
    [SerializeField] private bool hideJeolguUIAfter = true;   // 끝나고 절구 UI 끄기

    private bool isMilling = false;

    // 애니 끝에 쓸 캐시
    private string cachedSourceName, cachedResultName;
    private Sprite cachedResultSprite;

    [Header("재고 추가 효과")]
    [SerializeField] private Transform flyStartWorld; 
    [SerializeField] private Camera sourceUICamera;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

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

        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        OpenMill();
    }

    public void OpenMill()
    {
        // 박스 인벤토리 열려 있으면 오픈 막기
        if (BoxInventoryManager.Instance != null && BoxInventoryManager.Instance.IsInventoryOpen())
            return;

        // 도감 패널이 열려 있으면 오픈 막기
        if (DoGamUIManager.Instance != null && DoGamUIManager.Instance.IsOpen())
            return;

        gameObject.SetActive(true);

        //SFXManager.Instance.PlayBbyongSFX();

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

    public void OnSelectedSlotClicked()
    {
        if (SFXManager.Instance) SFXManager.Instance.PlayBtnClickSFX();
        Debug.Log("[MillManager] OnSelectedSlotClicked");

        if (inventoryPanel == null)
        {
            Debug.LogError("[MillManager] inventoryPanel is NULL");
            return;
        }
        else
        {
            ReturnQueuedToInventory();
            ClearSelectionUI();
        }
        //선택창 눌렀을 때 inventory 패널 켬
        RebuildInventoryGrid();

        inventoryPanel.SetActive(true);

        Debug.Log(
            $"[MillManager] inventoryPanel activeSelf={inventoryPanel.activeSelf}, " +
            $"activeInHierarchy={inventoryPanel.activeInHierarchy}, " +
            $"childCount(Content)={inventoryPanelParent.childCount}"
            );
    }

    private bool IsMillable(string itemName)
    {
        //가루로 만들 수 있는 재료
        return itemName == "Rice" || itemName == "Danhobak" || itemName == "Baeknyeoncho" || itemName == "Mugwort" || itemName == "cinnamon";
    }


    public void SelectItem(MillItemData item)
    {
        if (SFXManager.Instance) SFXManager.Instance.PlayBtnClickSFX();
        //기존 하나씩만 변환되는 코드
        /*if (ReferenceEquals(selectedItem, item))
        {
            item.itemQuantity += 1;
            selectedItem = null;
            selectedSlot.Clear();
            UpdateInventoryUI();
            confirmButton.interactable = false;
            inventoryPanel.SetActive(false);
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

        inventoryPanel.SetActive(false);*/
        if (item.itemQuantity <= 0 && (selectedItem == null || selectedItem.itemName != item.itemName))
        {
            return;
        }

        if (selectedItem == null)
        {
            selectedItem = item;
            queuedCount = 1;
            item.itemQuantity -= 1;

            selectedSlot.Set(item, queuedCount);
            UpdateInventoryUI();
            confirmButton.interactable = true;
            return;
        }

        if(selectedItem.itemName == item.itemName)
        {
            if (item.itemQuantity <= 0)
            {
                return;
            }
            item.itemQuantity -= 1;
            queuedCount += 1;

            selectedSlot.Set(item, queuedCount);
            UpdateInventoryUI();
            confirmButton.interactable = true;
            return;
        }

        ReturnQueuedToInventory();

        selectedItem = item;
        item.itemQuantity -= 1;
        queuedCount = 1;

        selectedSlot.Set(item, queuedCount);
        UpdateInventoryUI();
        confirmButton.interactable = true;

    }

    private void ReturnQueuedToInventory()
    {
        if(selectedItem != null && queuedCount > 0)
        {
            selectedItem.itemQuantity += queuedCount;
        }
        queuedCount = 0;
    }

    private void ClearSelectionUI()
    {
        selectedItem = null;
        queuedCount = 0;
        selectedSlot.Clear();
        UpdateInventoryUI();
        confirmButton.interactable = false;
    }

    private void RebuildInventoryGrid()
    {
        foreach (Transform child in inventoryPanelParent)
            Destroy(child.gameObject);

        foreach (var item in Inventory)
        {
            if (item.itemQuantity <= 0) continue;
            var obj = Instantiate(SlotPrefab, inventoryPanelParent);
            obj.GetComponent<MillInventory>().Setup(item, this);
        }
    }

    private void UpdateInventoryUI()
    {
        foreach (Transform child in inventoryPanelParent)
        {
            var slot = child.GetComponent<MillInventory>();
            slot?.UpdateQuantityText();
        }
    }

    /// sourceName 재료를 useCount만큼 빼고
    /// resultName 가루를 useCount만큼 넣었을 때
    /// 스택/슬롯 기준으로 창고에 들어갈 수 있는지 체크 함수 추가
    private bool CanConvertToPowder(string sourceName, string resultName, int useCount)
    {
        var storage = StorageInventory.Instance;
        if (storage == null) return true; // 안전빵

        if (useCount <= 0) return false;

        int maxStack = storage.maxStackPerItem;
        int freeSlots = storage.FreeSlots;

        int sourceCnt = storage.GetItemCount(sourceName);
        int resultCnt = storage.GetItemCount(resultName);

        if (sourceCnt <= 0) return false;

        // 재료를 useCount만큼 빼면 몇개 슬롯이 비는지 계산
        int srcStacksBefore = Mathf.CeilToInt(sourceCnt / (float)maxStack);

        int srcRemain = Mathf.Max(sourceCnt - useCount, 0);
        int srcStacksAfter = srcRemain > 0
            ? Mathf.CeilToInt(srcRemain / (float)maxStack)
            : 0;

        int freedSlotsBySource = Mathf.Max(0, srcStacksBefore - srcStacksAfter);

        // 재료를 빼서 생기는 슬롯까지 포함한 실제 사용 가능 슬롯 수
        int effectiveFreeSlots = freeSlots + freedSlotsBySource;

        // 가루 아이템을 useCount만큼 넣으면 필요한 추가 스택(슬롯) 수 계산
        int resStacksBefore = Mathf.CeilToInt(resultCnt / (float)maxStack);
        int resStacksAfter = Mathf.CeilToInt((resultCnt + useCount) / (float)maxStack);

        int extraStacksNeeded = Mathf.Max(0, resStacksAfter - resStacksBefore);

        // 필요한 추가 슬롯 == 실제 여유 슬롯이라면 가능
        return extraStacksNeeded <= effectiveFreeSlots;
    }

    public void Confirm()
    {
        if (SFXManager.Instance) SFXManager.Instance.PlayBbyongSFX();
        if (selectedItem == null)
            return;

        int useCount = queuedCount;
        if (useCount <= 0)
            return;

        string sourceName = selectedItem.itemName;
        if (!MillDB.GrindResult.TryGetValue(sourceName, out string resultName))
        {
            return;
        }

        // **** 인벤토리 공간 체크 (재료 -> 가루 변환 고려) *****
        if (!CanConvertToPowder(sourceName, resultName, useCount))
        {
            // 창고 가득 찼을 때 사용하는 알림 패널 재사용
            if (PlayerInteract.Instance != null)
                PlayerInteract.Instance.ShowStorageFull();

            if (SFXManager.Instance != null)
                SFXManager.Instance.PlayBbyongSFX();

            Debug.Log("[MillManager] 인벤토리가 가득 차서 가루 변환을 할 수 없습니다.");
            return; // 여기서 애니 시작도 안 하고 그냥 종료
        }

        // 결과 스프라이트 미리 로드(끝에 표시)
        Sprite resultSprite = Resources.Load<Sprite>("Sprites/Ingredients/" + resultName);

        // 캐시
        cachedSourceName = sourceName;
        cachedResultName = resultName;
        cachedResultSprite = resultSprite;

        // 상태 잠금 & 버튼 잠금
        isMilling = true;
        confirmButton.interactable = false;
        closeButton.interactable = false;
        raycaster.enabled = false;
        inventoryPanel.SetActive(false);

        // 절구 UI 보여주고 애니메이션 재생
        if (jeolguUI) jeolguUI.SetActive(true);
        if (jeolguAnimator && !string.IsNullOrEmpty(millTriggerName))
        {
            jeolguAnimator.ResetTrigger(millTriggerName);
            jeolguAnimator.SetTrigger(millTriggerName);
        }

        if (SFXManager.Instance != null)
        {
            SFXManager.Instance.PlayMillLoop();
        }

        // 애니메이션 이벤트를 쓰지 않으면, 클립 길이만큼 대기 후 마무리
        if (!useAnimationEvent)
        {
            float wait = millClip ? millClip.length : 1.2f;
            StartCoroutine(FinishMillingAfterDelay(wait));
        }

        //StorageInventory.Instance.AddItem(sourceName, -1);
        //StorageInventory.Instance.AddItem(resultName, 1);
        //StorageInventory.Instance.SaveStorage();
        //Debug.Log($"{sourceName} → {resultName}로 변환");

        //Sprite resultSprite = Resources.Load<Sprite>("Sprites/Ingredients/" + resultName);
        //if (resultSprite != null)
        //    ShowResultEffect(resultSprite);

        //else
        //    Debug.LogWarning($"[MillManager] 스프라이트 로드 실패: {resultName}");

        //selectedItem = null;
        //selectedSlot.Clear();
        //UpdateInventoryUI();
        //confirmButton.interactable = false;

    }

    private IEnumerator FinishMillingAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        CompleteMilling();
    }



    public void MillAnimationComplete()
    {
        if (useAnimationEvent) CompleteMilling();
    }

    // 실제 변환 + UI 갱신 공용 로직
    private void CompleteMilling()
    {
        if (SFXManager.Instance != null)
        {
            SFXManager.Instance.StopMillLoop();
        }

        int useCount = queuedCount;
        if (useCount <= 0)
        {
            ClearSelectionUI();
            closeButton.interactable = true;
            if (hideJeolguUIAfter && jeolguUI) jeolguUI.SetActive(false);
            isMilling = false;
            return;
        }

        // 실제 변환(애니 끝난 시점)
        StorageInventory.Instance.AddItem(cachedSourceName, -useCount);
        StorageInventory.Instance.AddItem(cachedResultName, useCount);
        StorageInventory.Instance.SaveStorage();
        Debug.Log($"{cachedSourceName} → {cachedResultName} 변환 완료(애니 종료 후)");

        //==결과 이펙트 끝나고 재고 추가 효과 시작하도록 코루틴으로 분리했어요!!==
        StartCoroutine(PlayResultThenFlyAndAlert());

        //// 결과 이펙트
        //if (cachedResultSprite) ShowResultEffect(cachedResultSprite);

        //// === [추가] 창고로 날아가는 효과 ===
        //if (cachedResultSprite && StorageIconFlyEffect.Instance != null)
        //{
        //    var startPos = flyStartWorld != null
        //        ? flyStartWorld.position
        //        : (jeolguUI != null ? jeolguUI.transform.position : transform.position);

        //    var cam = sourceUICamera != null ? sourceUICamera : Camera.main;
        //    StorageIconFlyEffect.Instance.Play(cachedResultSprite, startPos, cam);
        //}

        //// === [추가] 창고 NEW 알림 ===
        //if (StorageAlertManager.Instance != null)
        //{
        //    StorageAlertManager.Instance.NotifyNewHarvestedItem(cachedResultName);
        //}

        //// 선택/인벤토리/UI 정리
        //if (selectedItem != null) selectedItem = null;
        //selectedSlot.Clear();
        //UpdateInventoryUI();
        //confirmButton.interactable = true;

        //// 절구 UI 끄기
        //if (hideJeolguUIAfter && jeolguUI) jeolguUI.SetActive(false);

        //isMilling = false;
    }

    // ===================코루틴 추가====================
    private IEnumerator PlayResultThenFlyAndAlert()
    {
        // 결과 이펙트 표시 
        if (cachedResultSprite)
        {
            ResultEffectImage.sprite = cachedResultSprite;
            ResultEffectImage.gameObject.SetActive(true);

            if (ResultEffectText != null)
            {
                if (queuedCount > 1)
                {
                    ResultEffectText.text = $"{queuedCount}";
                    ResultEffectText.gameObject.SetActive(true);

                    if (ResultEffectCountImage != null)
                        ResultEffectCountImage.gameObject.SetActive(true);
                }
                else
                {
                    ResultEffectText.gameObject.SetActive(false);

                    if (ResultEffectCountImage != null)
                        ResultEffectCountImage.gameObject.SetActive(false);
                }
            }

            if (SFXManager.Instance) SFXManager.Instance.PlayCorrectSFX();
            yield return new WaitForSeconds(displayDuration); // 결과 이펙트 표시 시간만큼 대기
            ResultEffectImage.gameObject.SetActive(false);    // 이펙트 종료

            if (ResultEffectText != null)
                ResultEffectText.gameObject.SetActive(false);

            if (ResultEffectCountImage != null)
                ResultEffectCountImage.gameObject.SetActive(false);
        }

        // 창고로 날아가는 효과
        if (cachedResultSprite && StorageIconFlyEffect.Instance != null)
        {
            var startPos = flyStartWorld != null
                ? flyStartWorld.position
                : (jeolguUI != null ? jeolguUI.transform.position : transform.position);

            var cam = sourceUICamera != null ? sourceUICamera : Camera.main;
            StorageIconFlyEffect.Instance.Play(cachedResultSprite, startPos, cam);
        }

        // 창고 NEW 알림
        if (StorageAlertManager.Instance != null)
        {
            StorageAlertManager.Instance.NotifyNewHarvestedItem(cachedResultName);
        }

        // 선택/인벤토리/UI 정리
        if (selectedItem != null) selectedItem = null;
        selectedSlot.Clear();
        UpdateInventoryUI();
        confirmButton.interactable = true;
        closeButton.interactable = true;
        raycaster.enabled = true;

        if (hideJeolguUIAfter && jeolguUI) jeolguUI.SetActive(false);
        isMilling = false;
    }



    public void ShowResultEffect(Sprite sprite, int count)
    {
        ResultEffectImage.sprite = sprite;
        ResultEffectImage.gameObject.SetActive(true);

        if (ResultEffectText != null)
        {
            ResultEffectText.text = count > 1 ? $"x{count}" : "";
            ResultEffectText.gameObject.SetActive(true);
            ResultEffectCountImage.gameObject.SetActive(true);
        }

        StartCoroutine(HideResultEffectAfterDelay());
    }

    IEnumerator HideResultEffectAfterDelay()
    {
        yield return new WaitForSeconds(displayDuration);
        ResultEffectImage.gameObject.SetActive(false);

        if (ResultEffectText != null)
        {
            ResultEffectText.gameObject.SetActive(false);
            ResultEffectCountImage.gameObject.SetActive(false);
        }
    }

    public void CloseMill()
    {
        ReturnQueuedToInventory();
        ClearSelectionUI();

        SFXManager.Instance.PlayBbyongSFX();
        MillPanel.SetActive(false);
    }

    public bool IsOpen()
    {
        return MillPanel != null && MillPanel.activeSelf;
    }
}
