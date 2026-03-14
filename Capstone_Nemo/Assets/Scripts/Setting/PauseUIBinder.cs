using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseUIBinder : MonoBehaviour
{
    [SerializeField] private GameObject settingsPanel;

    private void Start()
    {
        if (PauseManager.Instance == null)
            return;

        if (settingsPanel == null)
            return;

        PauseManager.Instance.RegisterSettingsPanel(settingsPanel);
    }
}
