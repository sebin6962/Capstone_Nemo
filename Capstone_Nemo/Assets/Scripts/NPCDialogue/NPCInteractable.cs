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

    [Header("기본 정보")]
    [SerializeField] private string npcId;
    [SerializeField] private string npcName;

    [Header("상호작용")]
    [SerializeField] private NPCPatrolRoute patrolRoute;

    [Header("입력 설정")]
    [SerializeField] private bool useDirectInteractKey = true;

    private bool canInteract = false;
    private bool isTalking = false;
    private bool waitForInteractKeyRelease = false;

    public string NpcId => npcId;
    public string NpcName => npcName;

    private void Start()
    {
        if (patrolRoute == null)
            patrolRoute = GetComponent<NPCPatrolRoute>();
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

        if (patrolRoute != null)
            patrolRoute.SetActive(false);

        if (TimeManager.Instance != null)
            TimeManager.Instance.SetTimeFlow(false);

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
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
            patrolRoute.SetActive(true);

        if (DialogueFocusManager.Instance != null)
            DialogueFocusManager.Instance.EndFocus();

        if (TimeManager.Instance != null)
            TimeManager.Instance.SetTimeFlow(true);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerCollidersInRange.Add(other);
        canInteract = true;
        interactableNpcsInRange.Add(this);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerCollidersInRange.Remove(other);

        if (playerCollidersInRange.Count > 0) return;

        canInteract = false;
        interactableNpcsInRange.Remove(this);
    }

    private void OnDisable()
    {
        playerCollidersInRange.Clear();
        canInteract = false;
        interactableNpcsInRange.Remove(this);
    }
}