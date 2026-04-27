using UnityEngine;

public class StatueColorChangeInteract : MonoBehaviour
{
    public KeyCode interactKey = KeyCode.E;

    private bool _canInteract = false;

    void Update()
    {
        if (!_canInteract) return;
        if (StatueColorChangeUIManager.Instance == null) return;

        if (Input.GetKeyDown(interactKey) && !StatueColorChangeUIManager.Instance.IsOpen())
        {
            StatueColorChangeUIManager.Instance.Open();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            _canInteract = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            _canInteract = false;
    }
}