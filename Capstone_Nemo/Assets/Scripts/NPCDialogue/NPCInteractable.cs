using System.Collections.Generic;
using UnityEngine;

public class NPCInteractable : MonoBehaviour
{
    // 아이템 상호작용 스크립트가 NPC와의 입력 충돌을 피할 때 사용합니다.
    private static readonly HashSet<NPCInteractable> interactableNpcsInRange
        = new HashSet<NPCInteractable>();

    // 플레이어에 Collider2D가 여러 개 있어도 하나가 먼저 빠졌다는 이유로
    // NPC 범위 밖으로 잘못 판정하지 않도록 실제로 겹친 콜라이더를 기록합니다.
    private readonly HashSet<Collider2D> playerCollidersInRange
        = new HashSet<Collider2D>();

    public static bool HasInteractableNpcInRange
    {
        get
        {
            interactableNpcsInRange.RemoveWhere(
                npc => npc == null || !npc.isActiveAndEnabled || !npc.canInteract);

            return interactableNpcsInRange.Count > 0;
        }
    }

    // NPC 상호작용은 다른 E키 월드 상호작용보다 우선합니다.
    // 대화창만 검사하면 대화를 여는 첫 프레임에는 같은 E 입력이
    // 다른 Update()에도 전달될 수 있으므로 NPC 범위도 함께 검사합니다.
    public static bool BlocksOtherWorldInteraction
    {
        get
        {
            bool isDialogueOpen =
                NPCDialogueUIManager.Instance != null &&
                NPCDialogueUIManager.Instance.IsDialogueOpen;

            return isDialogueOpen || HasInteractableNpcInRange;
        }
    }

    [Header("기본 정보")]
    [SerializeField] private string npcId;
    [SerializeField] private string npcName;

    [Header("상호작용")]
    [SerializeField] private NPCPatrolRoute patrolRoute;

    [Header("입력 설정")]
    [SerializeField] private bool useDirectInteractKey = true;

    [Header("상호작용 UI")]
    [SerializeField] private GameObject interactionBubbleUI;

    private bool canInteract = false;
    private bool isTalking = false;
    private bool waitForInteractKeyRelease = false;

    // 대화 시작 직전 NPC가 바라보던 방향을 저장해 두었다가
    // 대화가 끝나면 다시 원래 방향으로 되돌립니다.
    private Vector2 facingDirectionBeforeDialogue = Vector2.down;
    private bool hasStoredFacingDirection = false;

    public string NpcId => npcId;
    public string NpcName => npcName;

    private void Start()
    {
        if (patrolRoute == null)
            patrolRoute = GetComponent<NPCPatrolRoute>();

        if (interactionBubbleUI != null)
            interactionBubbleUI.SetActive(false);
    }

    private void Update()
    {
        // 대화 종료 직후 E키가 아직 눌려 있으면 재상호작용 방지
        if (waitForInteractKeyRelease)
        {
            if (!Input.GetKey(KeyCode.E))
                waitForInteractKeyRelease = false;

            return;
        }

        if (!useDirectInteractKey) return;

        // 대화창이 활성화되어 있으면 NPC 상호작용 입력 무시
        if (NPCDialogueUIManager.Instance != null &&
            NPCDialogueUIManager.Instance.IsDialogueOpen)
        {
            return;
        }

        if (!canInteract || isTalking) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            StartDialogueInternally();
        }
    }

    public void StartDialogueExternally()
    {
        if (!canInteract || isTalking) return;

        if (waitForInteractKeyRelease)
            return;

        if (NPCDialogueUIManager.Instance != null &&
            NPCDialogueUIManager.Instance.IsDialogueOpen)
        {
            return;
        }

        StartDialogueInternally();
    }

    private void StartDialogueInternally()
    {
        if (NPCDialogueUIManager.Instance == null) return;

        isTalking = true;

        if (interactionBubbleUI != null)
            interactionBubbleUI.SetActive(false);

        // 플레이어를 바라보기 전에 현재 NPC 방향을 먼저 기억합니다.
        if (patrolRoute != null)
        {
            facingDirectionBeforeDialogue = patrolRoute.GetFacingDirection();
            hasStoredFacingDirection = true;
            patrolRoute.SetActive(false);
        }

        if (TimeManager.Instance != null)
            TimeManager.Instance.SetTimeFlow(false);

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        // 대화 시작 순간 NPC가 플레이어 방향을 바라보도록 변경
        if (playerObj != null && patrolRoute != null)
            patrolRoute.FaceTarget(playerObj.transform);
        if (DialogueFocusManager.Instance != null)
            DialogueFocusManager.Instance.BeginFocus(playerObj, gameObject);

        NPCDialogueUIManager.Instance.OpenDialogue(this);
    }

    public void EndDialogue()
    {
        isTalking = false;

        // 대화 종료에 사용된 E 입력이 바로 NPC 상호작용으로 이어지는 것 방지
        waitForInteractKeyRelease = true;

        if (patrolRoute != null)
        {
            patrolRoute.SetActive(true);

            // 대화창이 닫히면 상호작용 직전 바라보던 방향으로 복원합니다.
            if (hasStoredFacingDirection)
                patrolRoute.SetFacingDirection(facingDirectionBeforeDialogue);
        }

        hasStoredFacingDirection = false;

        if (DialogueFocusManager.Instance != null)
            DialogueFocusManager.Instance.EndFocus();

        if (TimeManager.Instance != null)
            TimeManager.Instance.SetTimeFlow(true);

        // 플레이어가 아직 상호작용 범위 안에 있다면 다시 표시
        if (interactionBubbleUI != null && canInteract)
            interactionBubbleUI.SetActive(true);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerCollidersInRange.Add(other);
        canInteract = true;
        interactableNpcsInRange.Add(this);

        if (interactionBubbleUI != null && !isTalking)
            interactionBubbleUI.SetActive(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerCollidersInRange.Remove(other);

        if (playerCollidersInRange.Count > 0) return;

        canInteract = false;
        interactableNpcsInRange.Remove(this);

        if (interactionBubbleUI != null)
            interactionBubbleUI.SetActive(false);
    }

    private void OnDisable()
    {
        playerCollidersInRange.Clear();
        canInteract = false;
        interactableNpcsInRange.Remove(this);

        if (interactionBubbleUI != null)
            interactionBubbleUI.SetActive(false);
    }
}
