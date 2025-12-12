using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Tutorial/Key Flow", fileName = "TutorialKeyFlow")]

public class TutorialKeyFlow : MonoBehaviour
{
    public StoreTutorialStep step;         
    public string requiredMakerId;         
    public TutorialAction[] sequence;
}
