using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DagwaRewardEntry
{
    [Tooltip("OrderManager에서 쓰는 다과 키 (ex: injeolmi_finish)")]
    public string key;
    public int exp = 20;
    public int starlight = 10;
}

public class DagwaRewardManager : MonoBehaviour
{
    public static DagwaRewardManager Instance;

    public List<DagwaRewardEntry> rewards = new List<DagwaRewardEntry>();

    private Dictionary<string, DagwaRewardEntry> dict;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildDict();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void BuildDict()
    {
        dict = new Dictionary<string, DagwaRewardEntry>();

        foreach (var r in rewards)
        {
            if (r == null || string.IsNullOrWhiteSpace(r.key)) continue;

            var k = r.key.Trim().ToLower();
            if (!dict.ContainsKey(k))
            {
                dict.Add(k, r);
            }
        }
    }

    public DagwaRewardEntry GetReward(string key)
    {
        if (dict == null) BuildDict();
        if (string.IsNullOrWhiteSpace(key)) return null;

        key = key.Trim().ToLower();
        dict.TryGetValue(key, out var entry);
        return entry;
    }
}
