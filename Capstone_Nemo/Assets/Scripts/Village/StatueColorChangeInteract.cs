using UnityEngine;

public class StatueColorChangeInteract : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Header("Key Guide")]
    [SerializeField] private GameObject keyGuideUI;

    private bool _canInteract;

    private void Start()
    {
        if (keyGuideUI != null)
            keyGuideUI.SetActive(false);
    }

    private void Update()
    {
        if (!_canInteract)
            return;

        if (StatueColorChangeUIManager.Instance == null)
            return;

        if (Input.GetKeyDown(interactKey) &&
            !StatueColorChangeUIManager.Instance.IsOpen())
        {
            if (keyGuideUI != null)
                keyGuideUI.SetActive(false);

            StatueColorChangeUIManager.Instance.Open();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        _canInteract = true;

        if (keyGuideUI != null)
            keyGuideUI.SetActive(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        _canInteract = false;

        if (keyGuideUI != null)
            keyGuideUI.SetActive(false);
    }
}