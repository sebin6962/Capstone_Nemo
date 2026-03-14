using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseExitButton : MonoBehaviour
{
    public void OnClickResume()
    {
        if (PauseManager.Instance != null)
            PauseManager.Instance.ResumeGame();
    }
}
