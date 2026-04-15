using UnityEngine;

public class NPCInteractable : MonoBehaviour
{
    [Header("기본 정보")]
    [SerializeField] private string npcId;
    [SerializeField] private string npcName;

    [Header("상호작용")]
    //[SerializeField] private GameObject interactHintUI;
    [SerializeField] private NPCPatrolRoute patrolRoute;

    private bool canInteract = false;
    private bool isTalking = false;

    public string NpcId => npcId;
    public string NpcName => npcName;

    private void Start()
    {
        //if (interactHintUI != null)
            //interactHintUI.SetActive(false);

        if (patrolRoute == null)
            patrolRoute = GetComponent<NPCPatrolRoute>();
    }

    private void Update()
    {
        if (!canInteract || isTalking) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("[NPCInteractable] E 입력 감지");

            if (NPCDialogueUIManager.Instance == null) return;

            isTalking = true;

            //f (interactHintUI != null)
                //interactHintUI.SetActive(false);

            if (patrolRoute != null)
                patrolRoute.SetActive(false);

            NPCDialogueUIManager.Instance.OpenDialogue(this);
        }
    }

    public void EndDialogue()
    {
        isTalking = false;

        if (patrolRoute != null)
            patrolRoute.SetActive(true);

        //if (canInteract && interactHintUI != null)
           // interactHintUI.SetActive(true);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        canInteract = true;
        Debug.Log("[NPCInteractable] 플레이어 접근 감지 성공");

        // if (!isTalking && interactHintUI != null)
        //  interactHintUI.SetActive(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        canInteract = false;

      //  if (interactHintUI != null)
           // interactHintUI.SetActive(false);
    }
}
