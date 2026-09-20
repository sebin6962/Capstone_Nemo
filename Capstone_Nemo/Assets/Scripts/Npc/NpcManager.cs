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
    private bool isActionMenuActive = false;

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

        // 메뉴의 논리 상태와 실제 화면 표시를 분리한다.
        // 방앗간 튜토리얼 중에는 actionPanel만 숨기고 입력/액션은 그대로 유지한다.
        RefreshActionPanelVisibility();

        // actionPanel이 화면에 보이지 않아도 논리적으로 메뉴가 열려 있으면 입력 처리
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
        isActionMenuActive = true;
        RefreshActionPanelVisibility();
    }

    void CloseActionMenu()
    {
        isActionMenuActive = false;

        if (actionPanel != null)
            actionPanel.SetActive(false);
    }

    void RefreshActionPanelVisibility()
    {
        if (actionPanel == null) return;

        bool isMillTutorialRunning =
            MillTutorialManager.Instance != null &&
            MillTutorialManager.Instance.IsMillTutorialRunning;

        bool shouldShow =
            isActionMenuActive &&
            !isMillTutorialRunning &&
            trigger != null &&
            trigger.isPlayerNearNpc &&
            !IsShopOpen() &&
            !IsDialogueOpen();

        if (actionPanel.activeSelf != shouldShow)
            actionPanel.SetActive(shouldShow);
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
        return isActionMenuActive;
    }
}
