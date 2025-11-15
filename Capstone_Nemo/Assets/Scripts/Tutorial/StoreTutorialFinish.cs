using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoreTutorialFinish : MonoBehaviour
{
    public void OnClickConfirm()
    {
        if (StoreTutorialManager.Instance == null)
            return;

        if (StoreTutorialManager.Instance.IsCurrentStep(StoreTutorialStep.Finish))
        {
            StoreTutorialManager.Instance.CompleteStoreTutorial();
        }
    }
}
