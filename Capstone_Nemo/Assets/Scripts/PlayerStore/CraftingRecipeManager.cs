using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CraftingRecipeManager : MonoBehaviour
{
    public static CraftingRecipeManager Instance;

    private List<CraftingRecipe> allRecipes;

    void Awake()
    {
        Instance = this;

        TextAsset json = Resources.Load<TextAsset>("Data/CraftingRecipe");
        if (json == null)
        {
            Debug.LogError("������ JSON ������ ã�� �� �����ϴ�!");
            return;
        }

        CraftingRecipeList wrapper = JsonUtility.FromJson<CraftingRecipeList>(json.text);
        if (wrapper == null || wrapper.recipes == null || wrapper.recipes.Count == 0)
        {
            Debug.LogError("������ �Ľ� ���� �Ǵ� ��� ����");
            return;
        }

        allRecipes = wrapper.recipes;
        Debug.Log($"[������ �ε� �Ϸ�] {allRecipes.Count}�� �ε��");

    }

    public Sprite GetResultSprite(string makerId, IEnumerable<string> selectedIngredients)
    {
        Debug.Log($"[������ ��Ī �õ�] makerId = {makerId}, ��� = {string.Join(", ", selectedIngredients)}");

        foreach (var recipe in allRecipes)
        {
            Debug.Log($"�� �� ���: makerId = {recipe.makerId}, ��� = {string.Join(", ", recipe.ingredients)}");

            bool idMatch = recipe.makerId.Trim().ToLower() == makerId.Trim().ToLower();
            var set1 = new HashSet<string>(recipe.ingredients.Select(i => i.Trim().ToLower()));
            var set2 = new HashSet<string>(selectedIngredients.Select(i => i.Trim().ToLower()));
            bool ingredientsMatch = set1.SetEquals(set2);

            Debug.Log($"  �� ������ ��� Set: {string.Join(", ", set1)}");
            Debug.Log($"  �� ���õ� ��� Set: {string.Join(", ", set2)}");
            Debug.Log($"  - ID ��ġ: {idMatch}, ��� ��ġ: {ingredientsMatch}");

            if (idMatch && ingredientsMatch)
            {
                string path = "Sprites/Ingredients/" + recipe.resultSprite;
                Sprite sprite = Resources.Load<Sprite>(path);
                Debug.Log(sprite != null ? $"��������Ʈ �ε� ����: {path}" : $"��������Ʈ �ε� ����: {path}");
                return sprite;
            }
        }

        Debug.LogWarning("������ ��ġ ���� �� �⺻ ��� ���");
        return null;
    }

}
