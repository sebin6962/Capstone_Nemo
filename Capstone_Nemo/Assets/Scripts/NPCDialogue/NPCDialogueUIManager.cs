using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NPCDialogueUIManager : MonoBehaviour
{
    public static NPCDialogueUIManager Instance;

    private enum DialogueState
    {
        None,
        Line,
        Choice
    }

    [Header("UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text npcNameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Transform optionParent;
    [SerializeField] private GameObject optionButtonPrefab;
    [SerializeField] private Button nextButton;
    [SerializeField] private TMP_Text nextButtonText;

    private readonly List<GameObject> spawnedOptions = new();
    private readonly Queue<string> pendingLines = new();
    private readonly Dictionary<string, NPCDialogueNodeData> nodeDict = new();

    private NPCInteractable currentNpc;
    private NPCDialogueData currentDialogueData;
    private NPCDialogueNodeData currentNode;
    private DialogueState currentState = DialogueState.None;
    private string nextNodeAfterLines;

    public bool IsOpen()
    {
        return dialoguePanel != null && dialoguePanel.activeSelf;
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnClickNextButton);
        }
    }

    public void OpenDialogue(NPCInteractable npc)
    {
        if (npc == null) return;
        if (NPCDialogueDatabase.Instance == null) return;

        currentNpc = npc;
        currentDialogueData = NPCDialogueDatabase.Instance.GetDialogueByNpcId(npc.NpcId);

        if (currentDialogueData == null)
        {
            Debug.LogWarning($"[NPCDialogueUIManager] npcId={npc.NpcId} 의 대화 데이터가 없습니다.");
            CloseDialogue();
            return;
        }

        if (npcNameText != null)
            npcNameText.text = string.IsNullOrEmpty(currentDialogueData.npcName) ? npc.NpcName : currentDialogueData.npcName;

        BuildNodeDictionary(currentDialogueData);

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        ClearOptions();
        pendingLines.Clear();
        nextNodeAfterLines = null;

        string entryNodeId = GetEntryNodeId(currentDialogueData);

        if (string.IsNullOrEmpty(entryNodeId))
        {
            Debug.LogWarning($"[NPCDialogueUIManager] npcId={npc.NpcId} 의 시작 노드를 찾을 수 없습니다.");
            CloseDialogue();
            return;
        }

        MoveToNode(entryNodeId);
    }

    private void BuildNodeDictionary(NPCDialogueData data)
    {
        nodeDict.Clear();

        if (data == null || data.nodes == null)
            return;

        for (int i = 0; i < data.nodes.Count; i++)
        {
            NPCDialogueNodeData node = data.nodes[i];
            if (node == null || string.IsNullOrEmpty(node.nodeId))
                continue;

            nodeDict[node.nodeId] = node;
        }
    }

    private string GetEntryNodeId(NPCDialogueData data)
    {
        if (data == null) return null;

        if (data.randomGreetingNodeIds != null && data.randomGreetingNodeIds.Count > 0)
        {
            List<string> validIds = new List<string>();

            for (int i = 0; i < data.randomGreetingNodeIds.Count; i++)
            {
                string id = data.randomGreetingNodeIds[i];
                if (!string.IsNullOrWhiteSpace(id) && nodeDict.ContainsKey(id))
                    validIds.Add(id);
            }

            if (validIds.Count > 0)
            {
                int randomIndex = Random.Range(0, validIds.Count);
                return validIds[randomIndex];
            }
        }

        return data.startNodeId;
    }

    private void MoveToNode(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId))
        {
            CloseDialogue();
            return;
        }

        if (!nodeDict.TryGetValue(nodeId, out currentNode) || currentNode == null)
        {
            Debug.LogWarning($"[NPCDialogueUIManager] nodeId={nodeId} 를 찾을 수 없습니다.");
            CloseDialogue();
            return;
        }

        string nodeType = currentNode.type?.Trim().ToLower();

        switch (nodeType)
        {
            case "line":
                StartLineNode(currentNode);
                break;

            case "choice":
                StartChoiceNode(currentNode);
                break;

            case "end":
                CloseDialogue();
                break;

            default:
                Debug.LogWarning($"[NPCDialogueUIManager] 알 수 없는 node type: {currentNode.type}");
                CloseDialogue();
                break;
        }
    }

    private void StartLineNode(NPCDialogueNodeData node)
    {
        ClearOptions();
        pendingLines.Clear();

        if (node.lines != null)
        {
            for (int i = 0; i < node.lines.Count; i++)
            {
                string line = node.lines[i];
                if (!string.IsNullOrWhiteSpace(line))
                    pendingLines.Enqueue(line);
            }
        }

        nextNodeAfterLines = node.nextNodeId;
        currentState = DialogueState.Line;

        if (pendingLines.Count > 0)
        {
            ShowNextLine();
        }
        else
        {
            MoveToNode(nextNodeAfterLines);
        }
    }

    private void StartChoiceNode(NPCDialogueNodeData node)
    {
        ClearOptions();
        pendingLines.Clear();
        nextNodeAfterLines = null;

        currentState = DialogueState.Choice;
        SetNextButton(false, "다음");

        if (node.options == null || node.options.Count == 0)
        {
            CloseDialogue();
            return;
        }

        for (int i = 0; i < node.options.Count; i++)
        {
            NPCDialogueChoiceOptionData option = node.options[i];
            if (option == null) continue;

            if (!ShouldShowOption(option))
                continue;

            CreateOption(option.text, () =>
            {
                HandleOptionSelected(option);
            });
        }
    }

    private bool ShouldShowOption(NPCDialogueChoiceOptionData option)
    {
        if (option == null) return false;

        bool hasQuestIdCondition = !string.IsNullOrEmpty(option.requiredQuestId);
        bool hasQuestTargetNpcCondition = !string.IsNullOrEmpty(option.requiredQuestTargetNpcId);

        if (!hasQuestIdCondition && !hasQuestTargetNpcCondition)
            return true;

        if (QuestAcceptManager.Instance == null)
            return false;

        if (hasQuestIdCondition)
            return QuestAcceptManager.Instance.IsAccepted(option.requiredQuestId);

        if (hasQuestTargetNpcCondition)
        {
            QuestData talkQuest = QuestAcceptManager.Instance.GetAcceptedTalkQuestForNpc(option.requiredQuestTargetNpcId);
            return talkQuest != null;
        }

        return true;
    }

    private void HandleOptionSelected(NPCDialogueChoiceOptionData option)
    {
        if (option == null) return;

        if (option.completeTalkQuestOnSelect && QuestAcceptManager.Instance != null && currentNpc != null)
        {
            QuestData talkQuest = QuestAcceptManager.Instance.GetAcceptedTalkQuestForNpc(currentNpc.NpcId);
            if (talkQuest != null)
                QuestAcceptManager.Instance.CompleteAcceptedTalkQuest(talkQuest.id);
        }

        MoveToNode(option.nextNodeId);
    }

    private void OnClickNextButton()
    {
        if (currentState != DialogueState.Line)
            return;

        ShowNextLine();
    }

    private void ShowNextLine()
    {
        if (pendingLines.Count > 0)
        {
            if (dialogueText != null)
                dialogueText.text = pendingLines.Dequeue();

            SetNextButton(true, "다음");
            return;
        }

        MoveToNode(nextNodeAfterLines);
    }

    private void CreateOption(string text, UnityEngine.Events.UnityAction callback)
    {
        if (optionButtonPrefab == null || optionParent == null) return;

        GameObject obj = Instantiate(optionButtonPrefab, optionParent, false);
        spawnedOptions.Add(obj);

        RectTransform rt = obj.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            rt.anchoredPosition = Vector2.zero;
        }

        Button button = obj.GetComponent<Button>();
        TMP_Text textComp = obj.GetComponentInChildren<TMP_Text>();

        if (textComp != null)
            textComp.text = text;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(callback);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(optionParent as RectTransform);
    }

    private void ClearOptions()
    {
        for (int i = spawnedOptions.Count - 1; i >= 0; i--)
        {
            if (spawnedOptions[i] != null)
                Destroy(spawnedOptions[i]);
        }

        spawnedOptions.Clear();
    }

    private void SetNextButton(bool visible, string text)
    {
        if (nextButton != null)
            nextButton.gameObject.SetActive(visible);

        if (nextButtonText != null)
            nextButtonText.text = text;
    }

    public void CloseDialogue()
    {
        ClearOptions();
        pendingLines.Clear();
        nodeDict.Clear();
        nextNodeAfterLines = null;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        if (currentNpc != null)
            currentNpc.EndDialogue();

        currentNpc = null;
        currentDialogueData = null;
        currentNode = null;
        currentState = DialogueState.None;
    }
}
