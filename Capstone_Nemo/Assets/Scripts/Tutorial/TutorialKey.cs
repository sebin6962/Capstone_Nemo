using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialKey : MonoBehaviour
{
    [SerializeField] private GameObject eKey;
    [SerializeField] private GameObject spaceKey;

    public void ShowE()
    {
        if (eKey) eKey.SetActive(true);
        if (spaceKey) spaceKey.SetActive(false);
    }

    public void ShowSpace()
    {
        if (eKey) eKey.SetActive(false);
        if (spaceKey) spaceKey.SetActive(true);
    }
}
