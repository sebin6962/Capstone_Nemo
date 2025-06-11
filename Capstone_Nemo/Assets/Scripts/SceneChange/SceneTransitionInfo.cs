using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneTransitionInfo : MonoBehaviour
{
    public static SceneTransitionInfo Instance;

    public string fromScene = "";
    public string toScene = "";

    public string entranceID = ""; // "FromStore", "FromVillage" µî

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
