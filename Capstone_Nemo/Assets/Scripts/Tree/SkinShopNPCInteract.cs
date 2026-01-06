using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkinShopNPCInteract : MonoBehaviour
{
    public KeyCode interactKey = KeyCode.E;

    public NpcAreaSpeechBubble speechBubble;

    private bool _canInteract = false;

    public bool playOnEnter = true;

    void Update()
    {
        if (!_canInteract) return;
        if (SkinShopUIManager.Instance == null) return;

        if (Input.GetKeyDown(interactKey) && !SkinShopUIManager.Instance.IsOpen())
        {
            if (speechBubble != null)
                speechBubble.ShowBubbleOnce();

            SkinShopUIManager.Instance.Open();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            _canInteract = true;
        if (!playOnEnter) return;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            _canInteract = false;
    }
}
