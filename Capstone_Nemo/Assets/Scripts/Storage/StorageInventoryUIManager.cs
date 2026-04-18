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
    public Button openButton;

    void Awake()
    {
        Instance = this;
        if (StorageInventory.Instance != null)
            StorageInventory.Instance.LoadStorage();
        SyncMaxSlotsToInventory();
        StartCoroutine(RefreshNextFrame());
    }

    System.Collections.IEnumerator RefreshNextFrame()
    {
        yield return null;        // 다른 싱글턴/Collider 재빌드 대기
        UpdateSlots();
    }

    void OnEnable()
    {
        if (StorageInventory.Instance != null)
            StorageInventory.Instance.LoadStorage();
        SyncMaxSlotsToInventory();
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.G))
            return;

        // 이미 열려 있으면 버튼 상태와 상관없이 닫기 허용
        if (IsOpen())
        {
            ToggleStorageUIByHotkey();
            return;
        }

        // 닫혀 있을 때만 버튼 상태 검사
        if (openButton == null ||
            !openButton.gameObject.activeInHierarchy ||
            !openButton.interactable)
            return;

        ToggleStorageUIByHotkey();
    }

    public void SyncMaxSlotsToInventory()
    {
        if (StorageInventory.Instance == null) return;


        int available = slots.Count;


        StorageInventory.Instance.maxSlots = available;
    }

    public void ToggleStorageUI()
    {
        ToggleStorageUIInternal(false);
    }

    public void ToggleStorageUIByHotkey()
    {
        ToggleStorageUIInternal(true);
    }

    public void ToggleStorageUIInternal(bool ignoreButtonCheck)
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

        // 가루 변환 패널 열려 있으면 도감 오픈 막기
        if (MillManager.Instance != null && MillManager.Instance.IsOpen())
            return;

        // 상점 패널 열려 있으면 도감 오픈 막기
        if (ShopManager.Instance != null && ShopManager.Instance.IsOpen())
            return;

        // UI 버튼 외에는 열 수 없게 조건문 추가
        //if (!EventSystem.current.currentSelectedGameObject ||
        //    EventSystem.current.currentSelectedGameObject.GetComponent<Button>() == null)
        //    return;

        // 버튼 클릭이 아닐 때는 막되, 단축키 호출은 예외
        if (!ignoreButtonCheck)
        {
            if (!EventSystem.current.currentSelectedGameObject ||
                EventSystem.current.currentSelectedGameObject.GetComponent<Button>() == null)
                return;
        }

        if (panel.activeSelf)
        {
            panel.SetActive(false);
            SFXManager.Instance.PlayBoxOpenSFX();
        }
        else
        {
            UpdateSlots();
            panel.SetActive(true);
            SFXManager.Instance.PlayBoxOpenSFX();
        }

        // 창고 열 때 확인 처리
        StorageAlertManager.Instance.OnStorageOpened();
    }

    public void UpdateSlots()
    {
        // 모든 슬롯 초기화
        foreach (var slot in slots)
            slot.ClearSlot();

        // 인벤토리 싱글턴이 아직 준비 안 됐으면 그냥 리턴
        var inv = StorageInventory.Instance;
        if (inv == null) return;

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
