using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]

public class CropStage
{
    public Sprite sprite;
    public float timeToNextStage;
}


[CreateAssetMenu(menuName = "Crop/Crop Data")]
public class CropData : ScriptableObject
{
    public string cropName;         // 씨앗 이름 (예: RiceCrop)
    public string harvestItemName;  // 수확물 이름 (예: Rice)
    public List<CropStage> stages;

    public Sprite outlineSprite;

    [Header("아웃라인 위치")]
    public Vector3 outlineOffset;

    // 나무 전용 플래그/옵션
    public bool isTree = false;          // 나무면 true
    public int harvestResetStage = 1;    // 수확 후 되돌릴 단계
    public int minLevelToInteract = 7;   // 물주기/수확 최소 레벨
}