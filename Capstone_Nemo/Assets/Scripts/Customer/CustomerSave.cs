using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CustomerSave
{
    public int seatIndex;
    public CustomerState state;

    public bool isTutorialCustomer;
    public string tutorialDagwaId;

    public string orderedDagwa;
    public float orderTimeLimit;
    public float remainingTime;

    public int currentWaypointIndex;
    public Vector3 position;

    public int prefabIndex;

    public bool hasScenePosition;

    public float walkElapsed;
}
