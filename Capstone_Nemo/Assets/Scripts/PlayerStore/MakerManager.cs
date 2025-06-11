using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MakerManager : MonoBehaviour
{
    void OnApplicationQuit()
    {
        foreach (var maker in FindObjectsOfType<MakerInfo>())
        {
            maker.ClearAllSlots();
        }
    }
}
