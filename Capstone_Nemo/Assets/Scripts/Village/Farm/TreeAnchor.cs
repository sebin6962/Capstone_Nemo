using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeAnchor : MonoBehaviour
{
    public CropData treeData;       // 오미자/유자/모과/금귤 중 하나
    [Tooltip("시작 단계(보통 0=시든나무)")]
    public int startStage = 0;
}
