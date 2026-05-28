//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public class MillNpc : MonoBehaviour
//{
//    public GameObject MillPanel;
//    public NpcTrigger trigger;

//    // Update is called once per frame
//    void Update()
//    {
//        if (Input.GetKeyDown(KeyCode.E) && trigger.isPlayerNearNpc && !IsMillOpen())
//        {
//            Debug.Log("E키 눌림 - 방앗간 토글 시도");
//            OpenMill();

//            //Mill 튜토리얼 진행 트리거 1
//            if (MillTutorialManager.Instance && MillTutorialManager.Instance.IsCurrentStep(MillTutorialStep.TalkToNpc))
//            {
//                MillTutorialManager.Instance.GoToNextMillStep();
//            }
//        }
//    }

//    private void OpenMill()
//    {
//        MillPanel.SetActive(true);

//        if (SFXManager.Instance != null)
//        {
//            SFXManager.Instance.PlayBbyongSFX();
//        }
//    }

//    public bool IsMillOpen()
//    {
//        return MillPanel.activeSelf;
//    }
//}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MillNpc : MonoBehaviour
{
    [Header("방앗간")]
    public GameObject MillPanel;
    public MillManager millManager;
    public NpcTrigger trigger;

    [Header("행동 선택 UI")]
    public GameObject actionPanel;
    public Button millButton;
    public Button talkButton;

    [Header("대화 연결")]
    public NPCInteractable npcInteractable;

    private bool wasPlayerNear = false;

    void Start()
    {
        if (actionPanel != null)
            actionPanel.SetActive(false);

        if (millButton != null)
        {
            millButton.onClick.RemoveAllListeners();
            millButton.onClick.AddListener(OnClickMill);
        }

        if (talkButton != null)
        {
            talkButton.onClick.RemoveAllListeners();
            talkButton.onClick.AddListener(OnClickTalk);
        }

        if (npcInteractable == null)
            npcInteractable = GetComponent<NPCInteractable>();
    }

    void Update()
    {
        if (trigger == null) return;

        bool isNear = trigger.isPlayerNearNpc;

        // NPC 범위에 처음 들어왔을 때 행동 선택 UI 열기
        if (isNear && !wasPlayerNear && !IsMillOpen() && !IsDialogueOpen())
        {
            OpenActionMenu();
        }

        // NPC 범위에서 벗어나면 행동 선택 UI 닫기
        if (!isNear && wasPlayerNear)
        {
            CloseActionMenu();
        }

        wasPlayerNear = isNear;

        // 행동 선택 UI가 열려 있을 때만 키 입력 처리
        if (IsActionMenuOpen() && !IsMillOpen() && !IsDialogueOpen())
        {
            HandleMenuInput();
        }
    }

    private void HandleMenuInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            OpenMillByMenu();
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

    private void OpenActionMenu()
    {
        if (actionPanel == null) return;

        actionPanel.SetActive(true);
    }

    private void CloseActionMenu()
    {
        if (actionPanel != null)
            actionPanel.SetActive(false);
    }

    private void OpenMillByMenu()
    {
        if (trigger != null && !trigger.isPlayerNearNpc) return;
        if (IsMillOpen()) return;

        CloseActionMenu();
        OpenMill();
    }

    private void StartTalk()
    {
        if (trigger != null && !trigger.isPlayerNearNpc) return;
        if (npcInteractable == null) return;
        if (NPCDialogueUIManager.Instance == null) return;

        CloseActionMenu();

        npcInteractable.StartDialogueExternally();
    }

    private void OnClickMill()
    {
        OpenMillByMenu();
    }

    private void OnClickTalk()
    {
        StartTalk();
    }

    private void OpenMill()
    {
        if (millManager != null)
        {
            millManager.OpenMill();
        }
        else
        {
            Debug.LogWarning("[MillNpc] millManager가 연결되지 않았습니다.");
            return;
        }

        if (SFXManager.Instance != null)
        {
            SFXManager.Instance.PlayBbyongSFX();
        }

        // Mill 튜토리얼 진행 트리거 1
        if (MillTutorialManager.Instance && MillTutorialManager.Instance.IsCurrentStep(MillTutorialStep.TalkToNpc))
        {
            MillTutorialManager.Instance.GoToNextMillStep();
        }
    }

    public bool IsMillOpen()
    {
        return MillPanel != null && MillPanel.activeSelf;
    }

    public bool IsActionMenuOpen()
    {
        return actionPanel != null && actionPanel.activeSelf;
    }

    private bool IsDialogueOpen()
    {
        return NPCDialogueUIManager.Instance != null && NPCDialogueUIManager.Instance.IsOpen();
    }
}