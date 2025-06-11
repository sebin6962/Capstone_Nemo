using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Tool
{
    public string id;
    public string name;
    public string spritePath;
}

[System.Serializable]
public class ToolList
{
    public List<Tool> tools;
}
