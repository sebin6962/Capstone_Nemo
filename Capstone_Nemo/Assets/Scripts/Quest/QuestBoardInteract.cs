using UnityEngine;

public class QuestBoardInteract : MonoBehaviour
{
    [SerializeField] private GameObject interactHintUI;
    private bool canInteract = false;

    private void Start()
    {
        if (interactHintUI != null)
            interactHintUI.SetActive(false);
    }

    private void Update()
    {
        if (!canInteract) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (QuestBoardUIManager.Instance == null) return;

            if (QuestBoardUIManager.Instance.IsOpen())
                QuestBoardUIManager.Instance.CloseAll();
            else
                QuestBoardUIManager.Instance.OpenQuestList();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        canInteract = true;

        if (interactHintUI != null)
            interactHintUI.SetActive(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        canInteract = false;

        if (interactHintUI != null)
            interactHintUI.SetActive(false);

        if (QuestBoardUIManager.Instance != null && QuestBoardUIManager.Instance.IsOpen())
            QuestBoardUIManager.Instance.CloseAll();
    }
}
