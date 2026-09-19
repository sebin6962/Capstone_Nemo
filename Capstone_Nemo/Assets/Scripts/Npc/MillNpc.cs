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

    [Header("대화 중 NPC 애니메이션")]
    [Tooltip("대화 중 멈출 NPC Animator")]
    public Animator npcAnimator;

    [Tooltip("idle 스프라이트를 표시할 SpriteRenderer")]
    public SpriteRenderer npcSpriteRenderer;

    [Tooltip("대화 중 위치를 이동시킬 자식 Visual 오브젝트")]
    public Transform npcVisual;

    [Tooltip("이 NPC가 대화 중 사용할 idle 스프라이트")]
    public Sprite dialogueIdleSprite;

    [Header("대화 중 idle 위치")]
    [Tooltip("대화 중 idle 스프라이트를 표시할 로컬 위치")]
    public Vector3 dialogueIdleLocalPosition;

    private bool wasPlayerNear = false;
    private bool wasThisNpcDialogueOpen = false;
    private bool animatorWasEnabledBeforeDialogue = false;
    private Vector3 originalSpriteLocalPosition;
    private bool originalSpritePositionCaptured = false;

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

        if (npcAnimator == null)
            npcAnimator = GetComponentInChildren<Animator>(true);

        if (npcSpriteRenderer == null)
            npcSpriteRenderer = GetComponentInChildren<SpriteRenderer>(true);

        if (npcVisual == null && npcSpriteRenderer != null)
            npcVisual = npcSpriteRenderer.transform;
    }

    void Update()
    {
        if (trigger == null)
            return;

        bool isNear = trigger.isPlayerNearNpc;
        bool isAnyDialogueOpen = IsAnyDialogueOpen();
        bool isThisNpcDialogueOpen = IsThisNpcDialogueOpen();

        if (isThisNpcDialogueOpen && !wasThisNpcDialogueOpen)
        {
            EnterDialogueIdle();
        }
        else if (!isThisNpcDialogueOpen && wasThisNpcDialogueOpen)
        {
            ExitDialogueIdle();
        }

        wasThisNpcDialogueOpen = isThisNpcDialogueOpen;

        if (isNear && !wasPlayerNear && !IsMillOpen() && !isAnyDialogueOpen)
        {
            OpenActionMenu();
        }

        if (!isNear && wasPlayerNear)
        {
            CloseActionMenu();
        }

        wasPlayerNear = isNear;

        if (IsActionMenuOpen() && !IsMillOpen() && !isAnyDialogueOpen)
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
        if (actionPanel == null)
            return;

        actionPanel.SetActive(true);
    }

    private void CloseActionMenu()
    {
        if (actionPanel != null)
            actionPanel.SetActive(false);
    }

    private void OpenMillByMenu()
    {
        if (trigger != null && !trigger.isPlayerNearNpc)
            return;

        if (IsMillOpen())
            return;

        CloseActionMenu();
        OpenMill();
    }

    private void StartTalk()
    {
        if (trigger != null && !trigger.isPlayerNearNpc)
            return;

        if (npcInteractable == null)
            return;

        if (NPCDialogueUIManager.Instance == null)
            return;

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

        if (MillTutorialManager.Instance &&
            MillTutorialManager.Instance.IsCurrentStep(MillTutorialStep.TalkToNpc))
        {
            MillTutorialManager.Instance.GoToNextMillStep();
        }
    }

    private void EnterDialogueIdle()
    {
        if (npcAnimator != null)
        {
            animatorWasEnabledBeforeDialogue = npcAnimator.enabled;
            npcAnimator.enabled = false;
        }

        if (npcSpriteRenderer != null && dialogueIdleSprite != null)
        {
            npcSpriteRenderer.sprite = dialogueIdleSprite;
        }

        if (npcVisual != null)
        {
            if (!originalSpritePositionCaptured)
            {
                originalSpriteLocalPosition = npcVisual.localPosition;
                originalSpritePositionCaptured = true;
            }

            npcVisual.localPosition = dialogueIdleLocalPosition;
        }
    }

    private void ExitDialogueIdle()
    {
        if (npcVisual != null && originalSpritePositionCaptured)
        {
            npcVisual.localPosition = originalSpriteLocalPosition;
        }

        if (npcAnimator != null && animatorWasEnabledBeforeDialogue)
        {
            npcAnimator.enabled = true;
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

    private bool IsAnyDialogueOpen()
    {
        return NPCDialogueUIManager.Instance != null &&
               NPCDialogueUIManager.Instance.IsOpen();
    }

    private bool IsThisNpcDialogueOpen()
    {
        return NPCDialogueUIManager.Instance != null &&
               NPCDialogueUIManager.Instance.IsDialogueOpenFor(npcInteractable);
    }
}
