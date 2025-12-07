using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WayPointInfo : MonoBehaviour
{
    [Tooltip("여기서 멈출지 여부")]
    public bool stopHere = false;

    [Tooltip("몇 초 동안 멈출지")]
    public float waitTime = 1f;
}
