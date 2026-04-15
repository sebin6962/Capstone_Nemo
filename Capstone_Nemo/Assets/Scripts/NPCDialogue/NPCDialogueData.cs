using System;
using System.Collections.Generic;

[Serializable]
public class NPCDialogueChoiceOptionData
{
    public string text;
    public string nextNodeId;

    // 이 선택지를 보여줄 조건
    public string requiredQuestId;
    public string requiredQuestTargetNpcId;

    // 이 선택지를 고르면 현재 NPC 관련 Talk 퀘스트를 완료 처리할지
    public bool completeTalkQuestOnSelect;
}

[Serializable]
public class NPCDialogueNodeData
{
    public string nodeId;
    public string type; // "line", "choice", "end"

    public List<string> lines;
    public List<NPCDialogueChoiceOptionData> options;

    public string nextNodeId;
}

[Serializable]
public class NPCDialogueData
{
    public string npcId;
    public string npcName;

    // 기본 시작 노드(랜덤 인사말을 안 쓸 때 fallback)
    public string startNodeId;

    // 랜덤 인사말 후보 노드들
    public List<string> randomGreetingNodeIds;

    public List<NPCDialogueNodeData> nodes;
}

[Serializable]
public class NPCDialogueDataList
{
    public List<NPCDialogueData> npcs;
}
