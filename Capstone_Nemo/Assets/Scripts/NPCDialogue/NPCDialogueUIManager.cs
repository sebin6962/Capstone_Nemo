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

    [Header("Dialogue Open Animation")]
    [SerializeField] private RectTransform dialoguePanelRect;
    [SerializeField] private float panelStartOffsetX = -900f;
    [SerializeField] private float panelSlideDuration = 0.28f;

    [SerializeField] private float portraitStartOffsetY = -180f;
    [SerializeField] private float portraitPopDuration = 0.25f;
    [SerializeField] private float portraitOvershootY = 18f;

    private Vector2 defaultPanelPosition;
    private Coroutine openAnimationCoroutine;
    private bool isOpeningAnimation = false;

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

    [Header("Next Button Move Effect")]
    [SerializeField] private float nextButtonMoveDistance = 8f;
    [SerializeField] private float nextButtonMoveDuration = 0.35f;
    [SerializeField] private bool hideNextButtonWhileTyping = false;

    private Vector2 defaultNextButtonPosition;

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
        public string npcId;
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

            RectTransform nextButtonRect = nextButton.GetComponent<RectTransform>();
            if (nextButtonRect != null)
                defaultNextButtonPosition = nextButtonRect.anchoredPosition;
        }

        if (portraitImage != null)
        {
            defaultPortraitScale = portraitImage.transform.localScale;
            defaultPortraitPosition = portraitImage.rectTransform.anchoredPosition;
        }

        if (dialoguePanel != null)
        {
            if (dialoguePanelRect == null)
                dialoguePanelRect = dialoguePanel.GetComponent<RectTransform>();

            if (dialoguePanelRect != null)
                defaultPanelPosition = dialoguePanelRect.anchoredPosition;
        }
    }

    private void Update()
    {
        if (dialoguePanel == null || !dialoguePanel.activeSelf)
            return;

        if (isOpeningAnimation)
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
            if (setting != null && setting.portrait == portrait)
            {
                portraitImage.transform.localScale = defaultPortraitScale * setting.scale;
                portraitImage.rectTransform.anchoredPosition =
                    defaultPortraitPosition + setting.positionOffset;
                return;
            }
        }
    }

    private void StartOpenAnimation(System.Action onFinished)
    {
        if (openAnimationCoroutine != null)
            StopCoroutine(openAnimationCoroutine);

        openAnimationCoroutine = StartCoroutine(OpenAnimationCoroutine(onFinished));
    }

    private IEnumerator OpenAnimationCoroutine(System.Action onFinished)
    {
        isOpeningAnimation = true;

        StopNextButtonBlink();
        SetNextButton(false, "다음");

        if (dialogueText != null)
        {
            dialogueText.text = "";
            dialogueText.maxVisibleCharacters = 0;
        }

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        if (dialoguePanelRect != null)
            dialoguePanelRect.anchoredPosition = defaultPanelPosition + new Vector2(panelStartOffsetX, 0f);

        bool hasPortrait = portraitImage != null && portraitImage.sprite != null;

        if (hasPortrait)
        {
            portraitImage.gameObject.SetActive(false);
            portraitImage.rectTransform.anchoredPosition =
                defaultPortraitPosition + new Vector2(0f, portraitStartOffsetY);
        }

        // 1) 대화창 패널: 왼쪽에서 들어오기
        float t = 0f;

        while (t < panelSlideDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / panelSlideDuration);
            float eased = EaseOutCubic(p);

            if (dialoguePanelRect != null)
            {
                dialoguePanelRect.anchoredPosition = Vector2.Lerp(
                    defaultPanelPosition + new Vector2(panelStartOffsetX, 0f),
                    defaultPanelPosition,
                    eased
                );
            }

            yield return null;
        }

        if (dialoguePanelRect != null)
            dialoguePanelRect.anchoredPosition = defaultPanelPosition;

        // 2) 초상화: 대화창 밑에서 뿅 올라오기
        if (hasPortrait)
        {
            portraitImage.gameObject.SetActive(true);

            Vector2 hiddenPos = defaultPortraitPosition + new Vector2(0f, portraitStartOffsetY);
            Vector2 overshootPos = defaultPortraitPosition + new Vector2(0f, portraitOvershootY);

            t = 0f;

            while (t < portraitPopDuration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / portraitPopDuration);

                if (p < 0.72f)
                {
                    float subP = p / 0.72f;
                    portraitImage.rectTransform.anchoredPosition =
                        Vector2.Lerp(hiddenPos, overshootPos, EaseOutCubic(subP));
                }
                else
                {
                    float subP = (p - 0.72f) / 0.28f;
                    portraitImage.rectTransform.anchoredPosition =
                        Vector2.Lerp(overshootPos, defaultPortraitPosition, EaseOutCubic(subP));
                }

                yield return null;
            }

            portraitImage.rectTransform.anchoredPosition = defaultPortraitPosition;
        }

        isOpeningAnimation = false;
        openAnimationCoroutine = null;

        onFinished?.Invoke();
    }

    private float EaseOutCubic(float x)
    {
        return 1f - Mathf.Pow(1f - x, 3f);
    }

    public bool IsDialogueOpen
    {
        get
        {
            return dialoguePanel != null && dialoguePanel.activeSelf;
        }
    }

    private Sprite GetPortraitByNpcId(string npcId)
    {
        if (portraitSettings == null || string.IsNullOrEmpty(npcId))
            return null;

        foreach (var setting in portraitSettings)
        {
            if (setting != null && setting.npcId == npcId)
                return setting.portrait;
        }

        return null;
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

        // NPC마다 다른 초상화 적용
        ApplyPortrait(GetPortraitByNpcId(currentDialogueData.npcId));

        BuildNodeDictionary(currentDialogueData);

        //if (dialoguePanel != null)
            //dialoguePanel.SetActive(true);

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

        //MoveToNode(entryNodeId);
        StartOpenAnimation(() =>
        {
            MoveToNode(entryNodeId);
        });
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

            RectTransform rt = nextButton.GetComponent<RectTransform>();
            if (rt != null)
                rt.anchoredPosition = defaultNextButtonPosition;

            Image img = nextButton.GetComponent<Image>();
            if (img != null)
            {
                Color c = img.color;
                c.a = 1f;
                img.color = c;
            }
        }
    }

    private IEnumerator BlinkNextButtonCoroutine()
    {
        if (nextButton == null)
            yield break;

        RectTransform rt = nextButton.GetComponent<RectTransform>();

        if (rt == null)
            yield break;

        Vector2 upPos = defaultNextButtonPosition;
        Vector2 downPos = defaultNextButtonPosition + new Vector2(0f, -nextButtonMoveDistance);

        while (true)
        {
            float t = 0f;

            // 아래로 살짝 내려가기
            while (t < nextButtonMoveDuration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / nextButtonMoveDuration);
                float eased = EaseInOutSine(p);

                rt.anchoredPosition = Vector2.Lerp(upPos, downPos, eased);

                yield return null;
            }

            rt.anchoredPosition = downPos;

            t = 0f;

            // 다시 원래 위치로 올라오기
            while (t < nextButtonMoveDuration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / nextButtonMoveDuration);
                float eased = EaseInOutSine(p);

                rt.anchoredPosition = Vector2.Lerp(downPos, upPos, eased);

                yield return null;
            }

            rt.anchoredPosition = upPos;
        }
    }

    private float EaseInOutSine(float x)
    {
        return -(Mathf.Cos(Mathf.PI * x) - 1f) / 2f;
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
        if (openAnimationCoroutine != null)
        {
            StopCoroutine(openAnimationCoroutine);
            openAnimationCoroutine = null;
        }

        isOpeningAnimation = false;

        if (dialoguePanelRect != null)
            dialoguePanelRect.anchoredPosition = defaultPanelPosition;

        if (portraitImage != null)
            portraitImage.rectTransform.anchoredPosition = defaultPortraitPosition;

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
