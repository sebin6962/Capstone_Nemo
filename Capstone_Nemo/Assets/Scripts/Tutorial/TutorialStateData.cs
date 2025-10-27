using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

[Serializable]
public class TutorialStateData
{
    public bool tutorialDone = false;
}

public static class TutorialState
{
    private static string PathFor(string server)
    {
        return Path.Combine(Application.persistentDataPath, $"tutorial_{server}.json");
    }

    public static TutorialStateData Load(string server)
    {
        var path = PathFor(server);
        if (!File.Exists(path))
            return new TutorialStateData();

        try
        {
            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<TutorialStateData>(json) ?? new TutorialStateData();
        }
        catch (Exception e)
        {
            Debug.LogError($"[TutorialState] Load failed: {e.Message}");
            return new TutorialStateData();
        }
    }

    public static void Save(string server, TutorialStateData data)
    {
        var path = PathFor(server);
        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(path, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[TutorialState] Save failed: {e.Message}");
        }
    }
}