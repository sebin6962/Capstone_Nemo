using System;
using System.Collections.Generic;

[Serializable]
public class QuestData
{
    public string id;
    public string npcSprite;
    public string title;
    public string description;

    public string targetType;
    public string targetId;
    public int targetCount;

    public string rewardType;
    public string rewardId;
    public int rewardAmount;
}

[Serializable]
public class QuestDataList
{
    public List<QuestData> quests;
}