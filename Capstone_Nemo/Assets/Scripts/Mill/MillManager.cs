using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MillManager : MonoBehaviour
{
    public GameObject MillPanel;
    public Transform inventoryPanelParent;
    public GameObject inventoryPanel;
    public GameObject SlotPrefab;
    public SelectedSlot selectedSlot;
    public Sprite[] testIcons;
    public Button confirmButton;

    private MillItemData selectedItem = null;
    private List<MillItemData> Inventory;

    [SerializeField] private Image ResultEffectImage;
    [SerializeField] private float displayDuration = 0.7f;

    [Header("애니메이션(추가)")]
    [SerializeField] private GameObject jeolguUI;
    [SerializeField] private Animator jeolguAnimator;
    [SerializeField] private AnimationClip millClip;   // 절구 재생 클립(길이 측정용)
    [SerializeField] private string millTriggerName = "Play"; // Animator 이름
    [SerializeField] private bool useAnimationEvent = false;  // 이벤트로 끝 처리할지
    [SerializeField] private bool hideJeolguUIAfter = true;   // 끝나고 절구 UI 끄기

    private bool isMilling = false;

    // 애니 끝에 쓸 캐시(코루틴/이벤트 공용)
    private string cachedSourceName, cachedResultName;
    private Sprite cachedResultSprite;

    [Header("재고 추가 효과")]
    [SerializeField] private Transform flyStartWorld; 
    [SerializeField] private Camera sourceUICamera;

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
        if (SFXManager.Instance) SFXManager.Instance.PlayBbyongSFX();
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

    public void OnSelectedSlotClicked()
    {
        if (SFXManager.Instance) SFXManager.Instance.PlayBtnClickSFX();
        Debug.Log("[MillManager] OnSelectedSlotClicked");
        // 선택창을 눌렀을 때 inventory 패널을 켠다
        RebuildInventoryGrid();

        if (inventoryPanel == null)
        {
            Debug.LogError("[MillManager] inventoryPanel is NULL");
            return;
        }
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
        return itemName == "Danhobak" || itemName == "Baeknyeoncho" || itemName == "Mugwort" || itemName == "cinnamon";
    }


    public void SelectItem(MillItemData item)
    {
        if (SFXManager.Instance) SFXManager.Instance.PlayBtnClickSFX();
        if (ReferenceEquals(selectedItem, item))
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

        inventoryPanel.SetActive(false);
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



    public void Confirm()
    {
        if (SFXManager.Instance) SFXManager.Instance.PlayBbyongSFX();
        if (selectedItem == null)
            return;

        string sourceName = selectedItem.itemName;
        if (!MillDB.GrindResult.TryGetValue(sourceName, out string resultName))
        {
            return;
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

        // 절구 UI 보여주고 애니메이션 재생
        if (jeolguUI) jeolguUI.SetActive(true);
        if (jeolguAnimator && !string.IsNullOrEmpty(millTriggerName))
        {
            jeolguAnimator.ResetTrigger(millTriggerName);
            jeolguAnimator.SetTrigger(millTriggerName);
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
        // 실제 변환(애니 끝난 시점)
        StorageInventory.Instance.AddItem(cachedSourceName, -1);
        StorageInventory.Instance.AddItem(cachedResultName, 1);
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
            yield return new WaitForSeconds(displayDuration); // 결과 이펙트 표시 시간만큼 대기
            ResultEffectImage.gameObject.SetActive(false);    // 이펙트 종료
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

        if (hideJeolguUIAfter && jeolguUI) jeolguUI.SetActive(false);
        isMilling = false;
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
