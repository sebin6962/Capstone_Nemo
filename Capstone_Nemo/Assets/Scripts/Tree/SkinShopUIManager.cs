using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkinShopUIManager : MonoBehaviour
{
    public static SkinShopUIManager Instance;

    [Header("Root")]
    public GameObject panelRoot;

    [System.Serializable]
    public class SkinSlot
    {
        public Toggle toggle;          // SkinSlot0/1/2 Toggle
        public Image previewImage;     // 썸네일
        public TMP_Text priceText;     // 가격 표시
        public GameObject ownedBadge;  // 보유 표시
        public GameObject lockBadge;   // 잠금 표시
    }

    [Header("스킨 슬롯 목록")]
    public List<SkinSlot> slots = new List<SkinSlot>();

    [Header("Buttons")]
    public Button btnConfirm;
    public Button btnClose;

    public TMP_Text confirmButtonText;

    [Header("Info Text")]
    //public TMP_Text txtInfo;

    private SkinSlot[] _slots;
    private int _selectedIndex = 0;
    private bool _binding = false;

    void Awake()
    {
        Instance = this;

        if (panelRoot) panelRoot.SetActive(false);
    }

    public bool IsOpen() => panelRoot != null && panelRoot.activeSelf;

    public void Open()
    {
        BindSlots();

        // 기본 선택: 0번
        Select(0, silent: true);

        if (panelRoot) panelRoot.SetActive(true);

        btnConfirm.onClick.RemoveAllListeners();
        btnConfirm.onClick.AddListener(OnConfirm);

        btnClose.onClick.RemoveAllListeners();
        btnClose.onClick.AddListener(Close);
    }

    public void Close()
    {
        if (panelRoot) panelRoot.SetActive(false);
        //if (txtInfo) txtInfo.text = "";
    }

    private void BindSlots()
    {
        var skinMgr = PlayerSkinManager.Instance;
        if (skinMgr == null)    
        {
            Debug.LogWarning("[SkinShop] PlayerSkinDataManager.Instance 없음");
            return;
        }

        _binding = true;

        for (int i = 0; i < slots.Count; i++)
        {
            int idx = i;
            var s = slots[i];

            if (s == null || s.toggle == null) continue;

            bool exists = skinMgr.skins != null && idx < skinMgr.skins.Count;

            s.toggle.interactable = exists;
            s.toggle.isOn = false;

            if (exists)
            {
                if (s.previewImage) s.previewImage.sprite = skinMgr.GetPreview(idx);

                bool owned = skinMgr.IsOwned(idx);
                int price = skinMgr.GetPrice(idx);

                if (s.priceText)
                {
                    if (idx == 0)
                        s.priceText.text = "기본";
                    else if (owned)
                        s.priceText.text = "구매완료";
                    else
                        s.priceText.text = $"{price}";
                }

                if (s.ownedBadge) s.ownedBadge.SetActive(owned);
                if (s.lockBadge) s.lockBadge.SetActive(!owned);
            }
            else
            {
                if (s.previewImage) s.previewImage.sprite = null;
                if (s.priceText) s.priceText.text = "";
                if (s.ownedBadge) s.ownedBadge.SetActive(false);
                if (s.lockBadge) s.lockBadge.SetActive(false);
            }

            s.toggle.onValueChanged.RemoveAllListeners();
            s.toggle.onValueChanged.AddListener(isOn =>
            {
                if (_binding) return;
                if (isOn) Select(idx, silent: false);
            });
        }

        _binding = false;
    }

    private void Select(int idx, bool silent)
    {
        var skinMgr = PlayerSkinManager.Instance;
        if (skinMgr == null) return;

        // 존재하는 스킨만 선택 가능
        if (skinMgr.skins == null || idx < 0 || idx >= skinMgr.skins.Count) return;

        _selectedIndex = idx;

        _binding = true;
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null && slots[i].toggle != null)
                slots[i].toggle.isOn = (i == _selectedIndex);
        }
        _binding = false;

        RefreshInfo();
    }

    private void RefreshInfo()
    {
        var skinMgr = PlayerSkinManager.Instance;
        if (skinMgr == null) return;

        bool owned = skinMgr.IsOwned(_selectedIndex);
        bool equipped = (skinMgr.EquippedIndex == _selectedIndex);

        //if (txtInfo != null)
        //{
        //    if (equipped) txtInfo.text = "착용중";
        //    else if (owned) txtInfo.text = "보유중";
        //    else txtInfo.text = $"가격: {skinMgr.GetPrice(_selectedIndex)} 별빛";
        //}

        if (confirmButtonText != null)
        {
            confirmButtonText.text = equipped ? "착용중" : (owned ? "착용" : "구매");
        }

        if (btnConfirm != null)
        {
            btnConfirm.interactable = !equipped;
        }
    }

    private void OnConfirm()
    {
        var skinMgr = PlayerSkinManager.Instance;
        if (skinMgr == null) return;

        // 미보유면 구매
        if (!skinMgr.IsOwned(_selectedIndex))
        {
            if (!skinMgr.TryBuy(_selectedIndex, out var reason))
            {
                //if (txtInfo) txtInfo.text = reason;
                return;
            }
        }

        // 즉시 착용
        skinMgr.Apply(_selectedIndex, save: true);

        // UI 표시(보유/잠금) 갱신
        BindSlots();

        Close();
    }
}
