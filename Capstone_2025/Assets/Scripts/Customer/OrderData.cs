using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Recipe
{
    public string[] ingredients;
    public string resultSprite;
    public string makerId;
}

[System.Serializable]
public class RecipeList
{
    //jsonŰ recipes
    public List<Recipe> recipes;
}
