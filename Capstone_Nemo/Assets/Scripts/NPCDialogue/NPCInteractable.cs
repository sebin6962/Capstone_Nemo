using UnityEngine;

public class NPCInteractable : MonoBehaviour
{
    [Header("기본 정보")]
    [SerializeField] private string npcId;
    [SerializeField] private string npcName;

    [Header("상호작용")]
    [SerializeField] private NPCPatrolRoute patrolRoute;

    [Header("입력 설정")]
    [SerializeField] private bool useDirectInteractKey = true;

    private bool canInteract = false;
    private bool isTalking = false;

    public string NpcId => npcId;
    public string NpcName => npcName;

    private void Start()
    {
        if (patrolRoute == null)
            patrolRoute = GetComponent<NPCPatrolRoute>();
    }

    private void Update()
    {
        if (!useDirectInteractKey) return;
        if (!canInteract || isTalking) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            StartDialogueInternally();
        }
    }

    public void StartDialogueExternally()
    {
        if (!canInteract || isTalking) return;
        StartDialogueInternally();
    }

    private void StartDialogueInternally()
    {
        if (NPCDialogueUIManager.Instance == null) return;

        isTalking = true;

        if (patrolRoute != null)
            patrolRoute.SetActive(false);

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (DialogueFocusManager.Instance != null)
            DialogueFocusManager.Instance.BeginFocus(playerObj, gameObject);

        NPCDialogueUIManager.Instance.OpenDialogue(this);
    }

    public void EndDialogue()
    {
        isTalking = false;

        if (patrolRoute != null)
            patrolRoute.SetActive(true);

        if (DialogueFocusManager.Instance != null)
            DialogueFocusManager.Instance.EndFocus();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        canInteract = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        canInteract = false;
    }
}