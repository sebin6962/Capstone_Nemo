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

        // ������ �̸� �� �۹� �̸� ��ȯ �õ�
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
    // �ʿ��� ������ �߰�
};
}
