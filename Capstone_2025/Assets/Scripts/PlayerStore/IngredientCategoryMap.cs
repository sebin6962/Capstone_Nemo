using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class IngredientCategoryEntry
{
    public string name;
    public string category;
}

[System.Serializable]
public class IngredientCategoryList
{
    public List<IngredientCategoryEntry> items;
}

public static class IngredientCategoryMap
{
    public static Dictionary<string, string> categoryByItem = new();

    static IngredientCategoryMap()
    {
        LoadCategoryData();
    }

    private static void LoadCategoryData()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("Data/IngredientsCategories");
        if (jsonFile == null)
        {
            Debug.LogError("ItemCategories.json ������ ã�� �� �����ϴ�. ���: Resources/Data/IngredientsCategories.json");
            return;
        }

        IngredientCategoryList data = JsonUtility.FromJson<IngredientCategoryList>(jsonFile.text);
        categoryByItem.Clear();

        foreach (var entry in data.items)
        {
            if (!categoryByItem.ContainsKey(entry.name))
                categoryByItem[entry.name] = entry.category;
        }

        Debug.Log($"[ItemCategoryMap] {categoryByItem.Count}���� ������-ī�װ� ���� �ε� �Ϸ�");
    }
}
