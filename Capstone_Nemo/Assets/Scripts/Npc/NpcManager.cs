using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NpcManager : MonoBehaviour
{
    [Header("기존")]
    public GameObject shopPanel;
    public ShopManager shopManager;
    public NpcTrigger trigger;
    public string dataPath;

    [Header("행동 선택 UI")]
    public GameObject actionPanel;
    public Button shopButton;
    public Button talkButton;

    [Header("대화 연결")]
    public NPCInteractable npcInteractable;

    private bool wasPlayerNear = false;

    void Start()
    {
        if (actionPanel != null)
            actionPanel.SetActive(false);

        if (shopButton != null)
            shopButton.onClick.AddListener(OnClickShop);

        if (talkButton != null)
            talkButton.onClick.AddListener(OnClickTalk);

        if (npcInteractable == null)
            npcInteractable = GetComponent<NPCInteractable>();
    }

    void Update()
    {
        if (trigger == null) return;

        bool isNear = trigger.isPlayerNearNpc;

        // NPC 범위에 처음 들어왔을 때
        if (isNear && !wasPlayerNear && !IsShopOpen() && !IsDialogueOpen())
        {
            OpenActionMenu();
        }

        // NPC 범위에서 벗어났을 때
        if (!isNear && wasPlayerNear)
        {
            CloseActionMenu();
        }

        wasPlayerNear = isNear;

        // 행동 선택 UI가 열려 있을 때만 입력 처리
        if (IsActionMenuOpen() && !IsShopOpen() && !IsDialogueOpen())
        {
            HandleMenuInput();
        }
    }

    void HandleMenuInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            OpenShopByMenu();
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            StartTalk();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseActionMenu();
        }
    }

    void OpenActionMenu()
    {
        if (actionPanel == null) return;
        actionPanel.SetActive(true);
    }

    void CloseActionMenu()
    {
        if (actionPanel != null)
            actionPanel.SetActive(false);
    }

    void OpenShopByMenu()
    {
        if (trigger != null && !trigger.isPlayerNearNpc) return;

        CloseActionMenu();

        if (shopManager != null)
        {
            shopManager.LoadShopData(dataPath);
            shopManager.OpenShop();
        }
    }

    void StartTalk()
    {
        if (trigger != null && !trigger.isPlayerNearNpc) return;
        if (npcInteractable == null) return;
        if (NPCDialogueUIManager.Instance == null) return;

        CloseActionMenu();
        npcInteractable.StartDialogueExternally();
    }

    void OnClickShop()
    {
        if (trigger != null && !trigger.isPlayerNearNpc) return;
        if (IsShopOpen()) return;

        OpenShopByMenu();
    }

    void OnClickTalk()
    {
        if (trigger != null && !trigger.isPlayerNearNpc) return;
        if (IsShopOpen()) return;

        StartTalk();
    }

    bool IsDialogueOpen()
    {
        return NPCDialogueUIManager.Instance != null && NPCDialogueUIManager.Instance.IsOpen();
    }

    public bool IsShopOpen()
    {
        return shopPanel != null && shopPanel.activeSelf;
    }

    public bool IsActionMenuOpen()
    {
        return actionPanel != null && actionPanel.activeSelf;
    }
}
