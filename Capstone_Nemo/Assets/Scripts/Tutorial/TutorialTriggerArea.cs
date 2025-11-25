using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialTriggerArea : MonoBehaviour
{
    [SerializeField] private VillageSecondStep targetStep;  
    [SerializeField] private bool onlyOnce = true;

    private bool triggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

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

