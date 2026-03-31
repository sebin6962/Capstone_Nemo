using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseMenuButtons : MonoBehaviour
{
    [SerializeField] private string mainSceneName = "SaveSelectScene";

    public void OnClickOpenSettings()
    {
        if (PauseManager.Instance != null)
            PauseManager.Instance.OpenSettings();
    }

    public void OnClickBackToMenu()
    {
        if (PauseManager.Instance != null)
            PauseManager.Instance.GoToMainScene(mainSceneName);
    }

    public void OnClickQuitGame()
    {
        if (PauseManager.Instance != null)
            PauseManager.Instance.QuitGame();
    }

    public void OnClickResume()
    {
        if (PauseManager.Instance != null)
            PauseManager.Instance.ResumeGame();
    }

    public void OnClickCloseSettings()
    {
        if (PauseManager.Instance != null)
            PauseManager.Instance.CloseSettingsAndBackToMenu();
    }
}
