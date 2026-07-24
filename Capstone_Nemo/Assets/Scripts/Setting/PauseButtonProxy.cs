using UnityEngine;

public class PauseButtonProxy : MonoBehaviour
{
    public void OnClickPause()
    {
        if (PauseManager.Instance != null)
            PauseManager.Instance.PauseGame();
    }

    public void OnClickResume()
    {
        if (PauseManager.Instance != null)
            PauseManager.Instance.ResumeGame();
    }

    public void OnClickOpenSettings()
    {
        if (PauseManager.Instance != null)
            PauseManager.Instance.OpenSettings();
    }

    public void OnClickCloseSettingsAndBackToMenu()
    {
        if (PauseManager.Instance != null)
            PauseManager.Instance.CloseSettingsAndBackToMenu();
    }

    public void OnClickBackToMenu(string sceneName)
    {
        if (PauseManager.Instance != null)
            PauseManager.Instance.GoToMainScene(sceneName);
    }
}
