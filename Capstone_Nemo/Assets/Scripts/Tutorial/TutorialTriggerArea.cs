using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialTriggerArea : MonoBehaviour
{
    [SerializeField] private VillageSecondStep targetStep;  
    [SerializeField] private bool onlyOnce = true;

    private bool triggered = false;

    public VillageSecondStep TargetStep => targetStep;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        var flow = TutorialFlowManager.Instance;
        if (flow != null)
        {
            if (flow.currentStep == GlobalTutorialStep.Mill ||
                flow.currentStep == GlobalTutorialStep.Done)
            {
                return;
            }
        }

        if (onlyOnce && triggered)
            return;

        var tm = FindObjectOfType<TutorialManager>();
        if (tm == null)
            return;

        if (tm.IsCurrentStep(targetStep))
        {
            triggered = true;
            tm.GoToNextVillageSecondStep();
        }
    }
}

