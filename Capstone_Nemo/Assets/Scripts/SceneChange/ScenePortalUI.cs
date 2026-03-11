using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ScenePortalUI : MonoBehaviour
{
    [SerializeField] private string targetSceneName;
    [SerializeField] private GameObject interactionUI;
    [SerializeField] private TMP_Text interactionText;
    [SerializeField] private string message;

    private bool playerInRange = false;

    private void Start()
    {
        if (interactionUI != null)
            interactionUI.SetActive(false);

        if(interactionText != null)
            interactionText.text = message;
    }
}
