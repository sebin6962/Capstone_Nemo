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
    public string cropName;         // ���� �̸� (��: RiceCrop)
    public string harvestItemName;  // ��Ȯ�� �̸� (��: Rice)
    public List<CropStage> stages;
}