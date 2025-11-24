using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoreTutorialFinish : MonoBehaviour
{
    public void OnClickConfirm()
    {
        SFXManager.Instance.PlayUnlockSlotSFX();
        if (StoreTutorialManager.Instance == null)
            return;

        if (StoreTutorialManager.Instance.IsCurrentStep(StoreTutorialStep.StoreFirst_Finish))
        {            
            StoreTutorialManager.Instance.CompleteStoreTutorial();
        }
    }
}
