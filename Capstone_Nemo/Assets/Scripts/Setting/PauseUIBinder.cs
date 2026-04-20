using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PauseUIBinder : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Image fadeImage;

    private IEnumerator Start()
    {
        yield return null;

        if (PauseManager.Instance == null)
        {
            Debug.LogWarning("[PauseUIBinder] PauseManager.Instance is null");
            yield break;
        }

        PauseManager.Instance.RegisterPauseUI(pauseMenuPanel, settingsPanel);
        PauseManager.Instance.RegisterFadeImage(fadeImage);
    }
}