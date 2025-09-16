using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class HowToItem
{
    public string title;   // 항목 제목(넘버링 등)
    public string text;    // 본문
    public string image;   // 리소스명 (Sprites/Guide/ + image)
}

[System.Serializable]
public class HowToList
{
    public List<HowToItem> items;
}
