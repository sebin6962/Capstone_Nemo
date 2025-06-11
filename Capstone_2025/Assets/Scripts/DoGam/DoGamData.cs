using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DoGamEntry
{
    public string name;
    public string image;
    public string description;
    public List<string> recipe;
    public string category;
}

[System.Serializable]
public class DoGamEntryList
{
    public List<DoGamEntry> entries;
}
