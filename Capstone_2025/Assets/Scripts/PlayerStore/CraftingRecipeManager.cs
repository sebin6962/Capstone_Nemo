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
            Debug.LogError("레시피 JSON 파일을 찾을 수 없습니다!");
            return;
        }

        CraftingRecipeList wrapper = JsonUtility.FromJson<CraftingRecipeList>(json.text);
        if (wrapper == null || wrapper.recipes == null || wrapper.recipes.Count == 0)
        {
            Debug.LogError("레시피 파싱 실패 또는 비어 있음");
            return;
        }

        allRecipes = wrapper.recipes;
        Debug.Log($"[레시피 로드 완료] {allRecipes.Count}개 로드됨");

    }

    public Sprite GetResultSprite(string makerId, IEnumerable<string> selectedIngredients)
    {
        Debug.Log($"[레시피 매칭 시도] makerId = {makerId}, 재료 = {string.Join(", ", selectedIngredients)}");

        foreach (var recipe in allRecipes)
        {
            Debug.Log($"→ 비교 대상: makerId = {recipe.makerId}, 재료 = {string.Join(", ", recipe.ingredients)}");

            bool idMatch = recipe.makerId.Trim().ToLower() == makerId.Trim().ToLower();
            var set1 = new HashSet<string>(recipe.ingredients.Select(i => i.Trim().ToLower()));
            var set2 = new HashSet<string>(selectedIngredients.Select(i => i.Trim().ToLower()));
            bool ingredientsMatch = set1.SetEquals(set2);

            Debug.Log($"  → 레시피 재료 Set: {string.Join(", ", set1)}");
            Debug.Log($"  → 선택된 재료 Set: {string.Join(", ", set2)}");
            Debug.Log($"  - ID 매치: {idMatch}, 재료 매치: {ingredientsMatch}");

            if (idMatch && ingredientsMatch)
            {
                string path = "Sprites/Ingredients/" + recipe.resultSprite;
                Sprite sprite = Resources.Load<Sprite>(path);
                Debug.Log(sprite != null ? $"스프라이트 로드 성공: {path}" : $"스프라이트 로드 실패: {path}");
                return sprite;
            }
        }

        Debug.LogWarning("레시피 일치 실패 → 기본 결과 사용");
        return null;
    }

}
