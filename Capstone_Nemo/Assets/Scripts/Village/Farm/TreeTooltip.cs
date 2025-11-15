using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeTooltip : MonoBehaviour
{
    [Tooltip("툴팁에 표시할 나무 이름 (예: '유자나무')")]
    public string treeName;

    [Tooltip("툴팁이 나무 기준에서 얼마나 위에 뜰지 조절")]
    public Vector3 worldOffset = new Vector3(0f, 1.5f, 0f);
}
