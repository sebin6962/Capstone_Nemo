using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TutorialDialogueLine
{
    public string speakerName;
    [TextArea(2, 4)]
    public string dialogue;
    public Sprite portrait;
}
