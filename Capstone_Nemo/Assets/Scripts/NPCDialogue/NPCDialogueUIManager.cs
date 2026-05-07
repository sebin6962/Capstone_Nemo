using System.Collections;
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
    [SerializeField] private Image portraitImage;

    [Header("Typing Effect")]
    [SerializeField] private float typingSpeed = 0.03f;

    [SerializeField] private float firstLineTypingStartDelay = 0.4f;
    private bool waitTypingDelayForNextLine = false;

    [Header("Next Button Blink")]
    [SerializeField] private float nextButtonBlinkInterval = 0.45f;
    [SerializeField] private bool hideNextButtonWhileTyping = false;
    [SerializeField] private float blinkMinAlpha = 0.25f;
    [SerializeField] private float blinkMaxAlpha = 1f;

    [SerializeField] private List<PortraitDisplaySetting> portraitSettings;
    private Vector3 defaultPortraitScale;
    private Vector2 defaultPortraitPosition;

    private readonly List<GameObject> spawnedOptions = new();
    private readonly Queue<string> pendingLines = new();
    private readonly Dictionary<string, NPCDialogueNodeData> nodeDict = new();

    private NPCInteractable currentNpc;
    private NPCDialogueData currentDialogueData;
    private NPCDialogueNodeData currentNode;
    private DialogueState currentState = DialogueState.None;
    private string nextNodeAfterLines;

    private Coroutine typingCoroutine;
    private Coroutine nextButtonBlinkCoroutine;

    private string currentFullLine = "";
    private bool isTyping = false;
    private string currentCategoryId = null;

    //튜토리얼용
    private System.Action tutorialDialogueFinishedCallback;
    private bool isTutorialDialogueMode = false;
    private Queue<TutorialDialogueLine> tutorialLines = new();
    private TutorialDialogueLine currentTutorialLine;

    //초상화크기
    [System.Serializable]
    public class PortraitDisplaySetting
    {
        public Sprite portrait;
        public float scale = 1f;
        public Vector2 positionOffset;
    }

    public bool IsOpen()
    {
        return dialoguePanel != null && dialoguePanel.activeSelf;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
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

        if (portraitImage != null)
        {
            defaultPortraitScale = portraitImage.transform.localScale;
            defaultPortraitPosition = portraitImage.rectTransform.anchoredPosition;
        }
    }

    private void Update()
    {
        if (dialoguePanel == null || !dialoguePanel.activeSelf)
            return;

        // 선택지 상태에서는 E로 넘기지 않음
        if (currentState != DialogueState.Line)
            return;

        if (Input.GetKeyDown(KeyCode.E)|| Input.GetKeyDown(KeyCode.Space))
        {
            OnClickNextButton();
        }
    }

    //초상화크기
    private void ApplyPortrait(Sprite portrait)
    {
        if (portraitImage == null)
            return;

        portraitImage.sprite = portrait;
        portraitImage.gameObject.SetActive(portrait != null);

        portraitImage.transform.localScale = defaultPortraitScale;
        portraitImage.rectTransform.anchoredPosition = defaultPortraitPosition;

        if (portrait == null)
            return;

        foreach (var setting in portraitSettings)
        {
            if (setting.portrait == portrait)
            {
                portraitImage.transform.localScale = defaultPortraitScale * setting.scale;
                portraitImage.rectTransform.anchoredPosition =
                    defaultPortraitPosition + setting.positionOffset;
                return;
            }
        }
    }

    public void OpenDialogue(NPCInteractable npc)
    {
        OpenDialogue(npc, null);
    }

    public void OpenDialogue(NPCInteractable npc, string categoryId)
    {
        if (npc == null) return;
        if (NPCDialogueDatabase.Instance == null) return;

        currentNpc = npc;
        currentCategoryId = categoryId;
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

        waitTypingDelayForNextLine = true;

        ClearOptions();
        pendingLines.Clear();
        nextNodeAfterLines = null;

        string entryNodeId = GetEntryNodeId(currentDialogueData, currentCategoryId);

        if (string.IsNullOrEmpty(entryNodeId))
        {
            Debug.LogWarning($"[NPCDialogueUIManager] npcId={npc.NpcId} 의 시작 노드를 찾을 수 없습니다.");
            CloseDialogue();
            return;
        }

        MoveToNode(entryNodeId);
    }

    //튜토리얼용
    public void OpenTutorialDialogue(List<TutorialDialogueLine> lines, System.Action onFinished = null)
    {
        if (lines == null || lines.Count == 0)
        {
            onFinished?.Invoke();
            return;
        }

        isTutorialDialogueMode = true;
        tutorialDialogueFinishedCallback = onFinished;

        tutorialLines.Clear();

        foreach (var line in lines)
        {
            tutorialLines.Enqueue(line);
        }

        ClearOptions();
        pendingLines.Clear();
        nextNodeAfterLines = null;

        currentState = DialogueState.Line;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        waitTypingDelayForNextLine = true;

        ShowNextTutorialLine();
    }

    //튜토리얼
    private void ShowNextTutorialLine()
    {
        if (tutorialLines.Count == 0)
        {
            CloseTutorialDialogue();
            return;
        }

        currentTutorialLine = tutorialLines.Dequeue();

        if (npcNameText != null)
            npcNameText.text = currentTutorialLine.speakerName;

        if (portraitImage != null)
        {
            ApplyPortrait(currentTutorialLine.portrait);
            portraitImage.gameObject.SetActive(currentTutorialLine.portrait != null);
        }

        StartTyping(currentTutorialLine.dialogue);
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

    private string GetEntryNodeId(NPCDialogueData data, string categoryId)
    {
        if (data == null)
            return null;

        NPCDialogueNpcProgressData npcProgress = null;

        if (NPCDialogueProgressManager.Instance != null)
            npcProgress = NPCDialogueProgressManager.Instance.GetOrCreateNpcProgress(data.npcId);

        string entryNodeId = NPCDialogueSelector.GetStartNodeId(data, npcProgress, categoryId);

        if (NPCDialogueProgressManager.Instance != null)
            NPCDialogueProgressManager.Instance.Save();

        return entryNodeId;
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
            ShowNextLine();
        else
            MoveToNode(nextNodeAfterLines);
    }

    private void StartChoiceNode(NPCDialogueNodeData node)
    {
        ClearOptions();
        pendingLines.Clear();
        nextNodeAfterLines = null;

        StopTypingImmediately();
        StopNextButtonBlink();

        currentState = DialogueState.Choice;
        SetNextButton(false, "다음");

        if (node.options == null || node.options.Count == 0)
        {
            CloseDialogue();
            return;
        }

        int createdOptionCount = 0;

        for (int i = 0; i < node.options.Count; i++)
        {
            NPCDialogueChoiceOptionData option = node.options[i];
            if (option == null) continue;

            if (!ShouldShowOption(option))
                continue;

            createdOptionCount++;

            CreateOption(option.text, () =>
            {
                HandleOptionSelected(option);
            });
        }

        if (createdOptionCount == 0)
            CloseDialogue();
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

        if (option.openRandomSetFromCategory && !string.IsNullOrEmpty(option.targetCategoryId))
        {
            currentCategoryId = option.targetCategoryId;

            string categoryEntryNodeId = GetEntryNodeId(currentDialogueData, currentCategoryId);

            if (string.IsNullOrEmpty(categoryEntryNodeId))
            {
                Debug.LogWarning($"[NPCDialogueUIManager] categoryId={currentCategoryId} 의 시작 노드를 찾을 수 없습니다.");
                CloseDialogue();
                return;
            }

            MoveToNode(categoryEntryNodeId);
            return;
        }

        MoveToNode(option.nextNodeId);
    }

    private void OnClickNextButton()
    {
        if (currentState != DialogueState.Line)
            return;

        if (isTyping)
        {
            CompleteCurrentTyping();
            return;
        }

        //튜토리얼
        if (isTutorialDialogueMode)
        {
            ShowNextTutorialLine();
            return;
        }


        ShowNextLine();
    }

    //튜토리얼
    private void CloseTutorialDialogue()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        tutorialLines.Clear();

        isTutorialDialogueMode = false;

        System.Action callback = tutorialDialogueFinishedCallback;
        tutorialDialogueFinishedCallback = null;

        callback?.Invoke();
        waitTypingDelayForNextLine = false;
    }

    private void ShowNextLine()
    {
        if (pendingLines.Count > 0)
        {
            string line = pendingLines.Dequeue();
            StartTyping(line);
            return;
        }

        MoveToNode(nextNodeAfterLines);
    }

    private void StartTyping(string line)
    {
        StopTypingImmediately();
        StopNextButtonBlink();

        currentFullLine = line;

        if (dialogueText == null)
            return;

        dialogueText.text = currentFullLine;
        dialogueText.maxVisibleCharacters = 0;

        if (hideNextButtonWhileTyping)
            SetNextButton(false, "다음");
        else
        {
            SetNextButton(true, "다음");
            if (nextButton != null)
                nextButton.interactable = false;
        }

        float startDelay = waitTypingDelayForNextLine ? firstLineTypingStartDelay : 0f;
        waitTypingDelayForNextLine = false;

        typingCoroutine = StartCoroutine(TypeLineCoroutine(startDelay));
    }

    private IEnumerator TypeLineCoroutine(float startDelay)
    {
        isTyping = true;

        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        dialogueText.ForceMeshUpdate();
        int totalVisibleCount = dialogueText.textInfo.characterCount;

        for (int i = 0; i <= totalVisibleCount; i++)
        {
            dialogueText.maxVisibleCharacters = i;
            yield return new WaitForSeconds(typingSpeed);
        }

        dialogueText.maxVisibleCharacters = totalVisibleCount;
        isTyping = false;
        typingCoroutine = null;

        ActivateAndBlinkNextButton();
    }

    private void CompleteCurrentTyping()
    {
        if (dialogueText == null) return;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        dialogueText.text = currentFullLine;
        dialogueText.ForceMeshUpdate();
        dialogueText.maxVisibleCharacters = dialogueText.textInfo.characterCount;
        isTyping = false;

        ActivateAndBlinkNextButton();
    }

    private void ActivateAndBlinkNextButton()
    {
        SetNextButton(true, "다음");

        if (nextButton != null)
            nextButton.interactable = true;

        StartNextButtonBlink();
    }

    private void StartNextButtonBlink()
    {
        StopNextButtonBlink();

        if (nextButton == null)
            return;

        nextButtonBlinkCoroutine = StartCoroutine(BlinkNextButtonCoroutine());
    }

    private void StopNextButtonBlink()
    {
        if (nextButtonBlinkCoroutine != null)
        {
            StopCoroutine(nextButtonBlinkCoroutine);
            nextButtonBlinkCoroutine = null;
        }

        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(true);

            Image img = nextButton.GetComponent<Image>();
            if (img != null)
            {
                Color c = img.color;
                c.a = blinkMaxAlpha;
                img.color = c;
            }
        }
    }

    private IEnumerator BlinkNextButtonCoroutine()
    {
        Image img = nextButton != null ? nextButton.GetComponent<Image>() : null;

        if (img == null)
            yield break;

        while (true)
        {
            Color c = img.color;
            c.a = blinkMinAlpha;
            img.color = c;

            yield return new WaitForSeconds(nextButtonBlinkInterval);

            c.a = blinkMaxAlpha;
            img.color = c;

            yield return new WaitForSeconds(nextButtonBlinkInterval);
        }
    }

    private void StopTypingImmediately()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        isTyping = false;

        if (dialogueText != null)
            dialogueText.maxVisibleCharacters = int.MaxValue;
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
        StopTypingImmediately();
        StopNextButtonBlink();
        currentFullLine = "";
        currentCategoryId = null;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        if (currentNpc != null)
            currentNpc.EndDialogue();

        currentNpc = null;
        currentDialogueData = null;
        currentNode = null;
        currentState = DialogueState.None;
        waitTypingDelayForNextLine = false;
    }
}
