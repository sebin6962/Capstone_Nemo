using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrderManager : MonoBehaviour
{
    public static OrderManager Instance;

    private List<string> dagwaList = new();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadOrders();
        }

        else
        {
            Destroy(gameObject);
        }
    }

    void LoadOrders()
    {
        TextAsset jsonText = Resources.Load<TextAsset>("data/craftingRecipe");
        RecipeList recipeList = JsonUtility.FromJson<RecipeList>(jsonText.text);

        HashSet<string> order = new HashSet<string>();
        foreach (var recipe in recipeList.recipes)
        {
            if (recipe.makerId.StartsWith("Siru") && !string.IsNullOrEmpty(recipe.resultSprite))
                order.Add(recipe.resultSprite);
        }
        dagwaList = new List<string>(order);
    }

    public string GetRandomDagwaList()
    {
        if (dagwaList.Count == 0)
        {
            Debug.LogError("다과 리스트 비어있음");
            return null;
        }
        int index = Random.Range(0, dagwaList.Count);
        return dagwaList[index];
    }

}
