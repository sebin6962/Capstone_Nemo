using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AllTutorialFinishButton : MonoBehaviour
{
    public void OnClickFinish()
    {
        TutorialManager.Instance.FinishAllTutorial();
    }
}
