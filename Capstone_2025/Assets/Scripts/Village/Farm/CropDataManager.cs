using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CropDataManager : MonoBehaviour
{
    public static CropDataManager Instance;

    public List<CropData> cropDataList;

    private Dictionary<string, CropData> cropDataDict;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        cropDataDict = new Dictionary<string, CropData>();

        foreach (var data in cropDataList)
        {
            cropDataDict[data.cropName] = data;
        }
    }

    public CropData GetCropDataByItemName(string itemName)
    {
        if (string.IsNullOrEmpty(itemName)) return null;

        // 아이템 이름 → 작물 이름 변환 시도
        if (itemToCropNameMap.TryGetValue(itemName, out var cropName))
        {
            itemName = cropName;
        }

        return cropDataDict.TryGetValue(itemName, out var data) ? data : null;
    }

    private Dictionary<string, string> itemToCropNameMap = new()
{
    { "Rice_seedBag", "RiceCrop" },
    { "Mugwort_seedBag", "MugwortCrop" }
    // 필요한 아이템 추가
};
}
