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
}