using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RecipeImageData
{
    public string tool;                 // 제작기
    public List<string> ingredients;   // 재료 (최대 4개)
    public string result;              // 완성된 아이템
}

[System.Serializable]
public class DoGamEntry
{
    public string name;
    public string nameKey;
    public string image;
    public string description;
    public string descriptionKey;
    public string category;
    public List<string> recipe;
    public List<string> recipeKeys;
    public List<string> recipeImage;
    public List<RecipeImageData> recipeImageBundle;

    public int rewardStarlight;
    public int rewardExp;
}

[System.Serializable]
public class DoGamEntryList
{
    public List<DoGamEntry> entries;
}
