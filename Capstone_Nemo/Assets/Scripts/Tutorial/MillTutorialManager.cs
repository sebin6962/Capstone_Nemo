using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;

public enum MillTutorialStep
{
    TalkToNpc = 0,
    SelectCrop = 1,
    Grind = 2,
    GrindQuit = 3,
    OpenStore = 4,
    QuitStore = 5,
    Mill_Finish = 6
}

public class MillTutorialManager : MonoBehaviour
{
    public static MillTutorialManager Instance;

    private bool isFinishingMillTutorial = false;

    [SerializeField] private GameObject[] stepPanels;

    [SerializeField] private List<TutorialDialogueLine> millStartDialogues;
    [SerializeField] private List<TutorialDialogueLine> afterTalkToNpcDialogues;
    [SerializeField] private List<TutorialDialogueLine> afterGrindDialogues;
    [SerializeField] private List<TutorialDialogueLine> afterOpenStoreDialogues;
    [SerializeField] private List<TutorialDialogueLine> afterQuitStoreDialogues;

    [Header("Mill tutorial - Grandma standing position")]
    [SerializeField] private GameObject grandmaNpcObject;
    [SerializeField] private Transform grandmaMillStandingPoint;

    private void PositionGrandmaForMillTutorial()
    {
        if (grandmaNpcObject == null) return;

        if (grandmaMillStandingPoint != null)
        {
            Vector3 position = grandmaMillStandingPoint.position;
            position.z = grandmaNpcObject.transform.position.z;
            grandmaNpcObject.transform.position = position;
        }

        grandmaNpcObject.SetActive(true);
    }

    [SerializeField] private GameObject millTutorialPanel;
    /*    [SerializeField] private CustomerSpawner customerSpawner;
        [SerializeField] private SeatManager seatManager;*/

    //[SerializeField] private GameObject tutorialBlocker;

    [Header("NPC 대화 UI")]
    [Header("NPC 숫자 UI")]
    [SerializeField] private GameObject talkToNpcNumberObject;
    [SerializeField] private Transform talkToNpcTarget;

    [SerializeField] private GameObject openStoreNumberObject;
    [SerializeField] private Transform openStoreNpcTarget;

    [SerializeField] private Vector2 npcNumberOffset = new Vector2(0f, 100f);

    private Transform currentNpcTarget;

    private RectTransform npcNumberRectTransform;
    private Canvas npcNumberCanvas;
    private RectTransform npcNumberCanvasRectTransform;

    private TutorialStateData state;
    private string server;

    public MillTutorialStep currentStep = MillTutorialStep.TalkToNpc;

    private Coroutine showStepPanelRoutine;

    // Hide key guides immediately after completing a step while retaining the text panel.
    // Restore previously visible guides when their panel is shown again.
    private readonly HashSet<GameObject> temporarilyHiddenGuides = new HashSet<GameObject>();

    // Keep tutorial hints hidden immediately after a step completes,
    // including hints re-enabled by other scripts before the next panel appears.
    private bool suppressCompletedStepGuides;

    private void HideActiveStepGuides()
    {
        suppressCompletedStepGuides = true;
        SuppressVisibleTutorialGuides();
    }

    private void LateUpdateTutorialGuides()
    {
        if (suppressCompletedStepGuides)
            SuppressVisibleTutorialGuides();
    }

    private void SuppressVisibleTutorialGuides()
    {
        // Do not rely on stepPanels entries being the exact active UI root.
        // Some prefabs place the visible guide under a nested step object.
        foreach (Transform item in FindObjectsOfType<Transform>(true))
        {
            string n = item.name;
            if (n != "InterectGuide" && !n.StartsWith("InterectGuide ("))
                continue;

            GameObject guide = item.gameObject;
            if (!guide.activeInHierarchy || !guide.activeSelf)
                continue;

            // Restrict to tutorial hierarchy to avoid hiding normal E-key prompts.
            bool isTutorialUI = false;
            for (Transform parent = item.parent; parent != null; parent = parent.parent)
            {
                if (parent.name.IndexOf("Tutorial", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    isTutorialUI = true;
                    break;
                }
            }
            if (!isTutorialUI) continue;

            temporarilyHiddenGuides.Add(guide);
            guide.SetActive(false);
        }
    }

    private void LateUpdate()
    {
        LateUpdateTutorialGuides();
    }

    private void RestoreGuidesForPanel(GameObject panel)
    {
        if (panel == null || temporarilyHiddenGuides.Count == 0) return;
        foreach (Transform child in panel.GetComponentsInChildren<Transform>(true))
        {
            if (temporarilyHiddenGuides.Remove(child.gameObject))
                child.gameObject.SetActive(true);
        }
    }

    private void RestoreAllTemporarilyHiddenGuides()
    {
        foreach (GameObject guide in temporarilyHiddenGuides)
            if (guide != null) guide.SetActive(true);
        temporarilyHiddenGuides.Clear();
    }



    public bool IsMillTutorialRunning;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (talkToNpcNumberObject != null)
            talkToNpcNumberObject.SetActive(false);

        if (openStoreNumberObject != null)
            openStoreNumberObject.SetActive(false);
    }

    void Start()
    {
        server = PlayerPrefs.GetString("SelectedSave", "default");
        state = TutorialState.Load(server);

        var flow = TutorialFlowManager.Instance;

        if (state.tutorialDone || TutorialFlowManager.Instance.currentStep == GlobalTutorialStep.Done)
        {
            HideAllPanels();
            IsMillTutorialRunning = false;
            return;
        }

        if (flow.currentStep == GlobalTutorialStep.Mill)
        {
            /*IsStoreTutorialRunning = true;
            currentStep = StoreTutorialStep.OpenStorage;

            if (customerSpawner)
                customerSpawner.SpawnTutorialCustomer("baekseolgi_finish", 15f);

            ShowStepPanel(currentStep);*/

            StartMillTutorial(flow.currentStep);
        }
        else
        {
            HideAllPanels();
            IsMillTutorialRunning = false;
        }
    }

    void StartMillTutorial(GlobalTutorialStep globalStep)
    {
        IsMillTutorialRunning = true;
        currentStep = MillTutorialStep.TalkToNpc;
        PositionGrandmaForMillTutorial();

        // 시간 정지
        if (TutorialFlowManager.Instance != null)
        {
            TutorialFlowManager.Instance.RequestTutorialTimePause();
            TutorialFlowManager.Instance.LockScenePortal();
        }

        PlayDialogueThen(() =>
        {
            ShowNpcNumber(
                talkToNpcNumberObject,
                talkToNpcTarget
            );

            ShowStepPanel(currentStep);

        }, millStartDialogues, "tutorial.mill.start", talkToNpcTarget);
    }

    void ShowStepPanel(MillTutorialStep step)
    {
        //SFXManager.Instance.PlayBbyongSFX();
        //HideAllPanels();

        //int index = (int)step;
        //if (stepPanels == null || index < 0 || index >= stepPanels.Length)
        //    return;

        //if (stepPanels[index])
        //{
        //    stepPanels[index].SetActive(true);
        //}

        // 이전에 돌고 있던 코루틴 있다면 정지
        if (showStepPanelRoutine != null)
            StopCoroutine(showStepPanelRoutine);

        // 2초 뒤에 패널 켜는 코루틴으로 빼뒀어요!!
        showStepPanelRoutine = StartCoroutine(ShowStepPanelAfterDelay(step, 1.0f));
    }

    private IEnumerator ShowStepPanelAfterDelay(MillTutorialStep step, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);

        SFXManager.Instance.PlayTutorialSFX();

        int index = (int)step;
        if (stepPanels == null || index < 0 || index >= stepPanels.Length)
            yield break;

        if (stepPanels[index])
        {
            SwitchStepPanel(index);
        }

        // 코루틴 끝남
        showStepPanelRoutine = null;
    }

    // 이전 패널을 유지하다가 새 패널이 준비되면 같은 프레임에 교체
    private void SwitchStepPanel(int index)
    {
        suppressCompletedStepGuides = false;
        if (stepPanels == null || index < 0 || index >= stepPanels.Length || stepPanels[index] == null)
            return;

        RestoreGuidesForPanel(stepPanels[index]);
        stepPanels[index].SetActive(true);
        for (int i = 0; i < stepPanels.Length; i++)
        {
            if (i == index || !stepPanels[i]) continue;
            stepPanels[i].SetActive(false);
            RestoreGuidesForPanel(stepPanels[i]);
        }
        RestoreAllTemporarilyHiddenGuides();
    }

    void HideAllPanels()
    {
        suppressCompletedStepGuides = false;
        RestoreAllTemporarilyHiddenGuides();
        if (stepPanels == null) return;
        foreach (var panel in stepPanels)
        {
            if (panel) panel.SetActive(false);
        }
    }

    public bool IsCurrentStep(MillTutorialStep step)
    {
        return IsMillTutorialRunning && currentStep == step;
    }

    // The mouse hint points at the left slot only. Hide it after that slot is clicked,
    // without advancing SelectCrop (the inventory item still needs to be selected).
    public void HideSelectCropMouseGuide()
    {
        if (IsCurrentStep(MillTutorialStep.SelectCrop))
            HideActiveStepGuides();
    }

    // The powder conversion has finished, but its result/fly presentation and
    // dialogue have not. The instruction must disappear immediately here.
    public void HideGrindPanelImmediately()
    {
        if (!IsCurrentStep(MillTutorialStep.Grind)) return;
        if (showStepPanelRoutine != null)
        {
            StopCoroutine(showStepPanelRoutine);
            showStepPanelRoutine = null;
        }
        HideAllPanels();
    }

    public void GoToNextMillStep()
    {
        HideActiveStepGuides();
        switch (currentStep)
        {
            //MillNpc.cs
            case MillTutorialStep.TalkToNpc:
                HideNpcNumber(talkToNpcNumberObject);
                currentStep = MillTutorialStep.SelectCrop;
                break;

            //MillManager.cs
            case MillTutorialStep.SelectCrop:
                currentStep = MillTutorialStep.Grind;
                break;

            case MillTutorialStep.Grind:
                currentStep = MillTutorialStep.GrindQuit;
                break;

            case MillTutorialStep.GrindQuit:
                currentStep = MillTutorialStep.OpenStore;
                ShowNpcNumber(
                    openStoreNumberObject,
                    openStoreNpcTarget
                );
                break;

            case MillTutorialStep.OpenStore:
                HideNpcNumber(openStoreNumberObject);
                currentStep = MillTutorialStep.QuitStore;
                break;

            case MillTutorialStep.QuitStore:
                currentStep = MillTutorialStep.Mill_Finish;
                break;

            case MillTutorialStep.Mill_Finish:
                FinishMillTutorial();
                return;

            default:
                return;
        }

        ShowMillStepWithOptionalDialogue(currentStep);
    }

    void ShowMillStepWithOptionalDialogue(MillTutorialStep step)
    {
        switch (step)
        {
            case MillTutorialStep.SelectCrop:
                PlayDialogueThen(() =>
                {
                    ShowStepPanel(step);
                }, afterTalkToNpcDialogues, "tutorial.mill.after_talk", talkToNpcTarget);
                break;

            case MillTutorialStep.GrindQuit:
                PlayDialogueThen(() =>
                {
                    ShowStepPanel(step);
                }, afterGrindDialogues, "tutorial.mill.after_grind", openStoreNpcTarget);
                break;

            case MillTutorialStep.QuitStore:
                PlayDialogueThen(() =>
                {
                    ShowStepPanel(step);
                }, afterOpenStoreDialogues, "tutorial.mill.after_open_store", openStoreNpcTarget);
                break;

            case MillTutorialStep.Mill_Finish:
                PlayDialogueThen(() =>
                {
                    ShowStepPanel(step);
                }, afterQuitStoreDialogues, "tutorial.mill.after_quit_store", openStoreNpcTarget);
                break;

            default:
                ShowStepPanel(step);
                break;
        }
    }

    void PlayDialogueThen(
        System.Action onFinished,
        List<TutorialDialogueLine> lines,
        string dialogueKeyPrefix,
        Transform focusNpc = null)
    {
        if (lines == null || lines.Count == 0)
        {
            onFinished?.Invoke();
            return;
        }

        if (NPCDialogueUIManager.Instance == null)
        {
            Debug.LogError("NPCDialogueUIManager가 없습니다.");
            onFinished?.Invoke();
            return;
        }

        StartCoroutine(PlayLocalizedDialogueThenRoutine(
            onFinished,
            lines,
            dialogueKeyPrefix,
            focusNpc));
    }

    private IEnumerator PlayLocalizedDialogueThenRoutine(
        System.Action onFinished,
        List<TutorialDialogueLine> lines,
        string dialogueKeyPrefix,
        Transform focusNpc)
    {
        for (int i = 0; i < lines.Count; i++)
        {
            TutorialDialogueLine line = lines[i];

            string speakerKey = GetSpeakerNameKey(line.speakerName);
            if (!string.IsNullOrEmpty(speakerKey))
            {
                var speakerHandle = LocalizationSettings.StringDatabase
                    .GetLocalizedStringAsync("Tutorial", speakerKey);

                yield return speakerHandle;

                if (!string.IsNullOrWhiteSpace(speakerHandle.Result) &&
                    speakerHandle.Result != speakerKey)
                {
                    line.speakerName = speakerHandle.Result;
                }
            }

            string dialogueKey = $"{dialogueKeyPrefix}.{i + 1:00}";
            var dialogueHandle = LocalizationSettings.StringDatabase
                .GetLocalizedStringAsync("Tutorial", dialogueKey);

            yield return dialogueHandle;

            // 키 또는 번역이 없으면 인스펙터에 입력된 한국어를 그대로 사용한다.
            if (!string.IsNullOrWhiteSpace(dialogueHandle.Result) &&
                dialogueHandle.Result != dialogueKey)
            {
                line.dialogue = dialogueHandle.Result;
            }

            lines[i] = line;
        }

        NPCDialogueUIManager.Instance.OpenTutorialDialogue(
            lines,
            () => onFinished?.Invoke(),
            focusNpc != null ? focusNpc.gameObject : null
            );
    }

    private string GetSpeakerNameKey(string speakerName)
    {
        switch (speakerName)
        {
            case "든해":
            case "Deunhae":
                return "tutorial.grandma.name";

            case "가람":
            case "Garam":
                return "tutorial.mill.speaker.garam";

            case "윤슬":
            case "Yoonseul":
                return "tutorial.mill.speaker.yoonseul";

            default:
                return null;
        }
    }

    public void FinishMillTutorial()
    {
        if (isFinishingMillTutorial)
            return;

        isFinishingMillTutorial = true;

        IsMillTutorialRunning = false;
        HideAllPanels();

        /*  if (customerSpawner)
            customerSpawner.EndTutorial();*/

        if (TutorialFlowManager.Instance != null)
        {
            TutorialFlowManager.Instance.UnlockScenePortal();
            TutorialFlowManager.Instance.ReleaseTutorialTimePause();
            TutorialFlowManager.Instance.SetStep(GlobalTutorialStep.PlayerStore_Second);
        }

        if (CircleSceneTransition.Instance != null)
        {
            CircleSceneTransition.Instance.TransitionToScene("PlayerStoreScene");
        }
        else
        {
            Debug.LogWarning("[MillTutorial] CircleSceneTransition이 없어서 일반 씬 전환을 실행합니다.");
            SceneManager.LoadScene("PlayerStoreScene");
        }
    }

    public void FinishAllTutorial()
    {
        FinishMillTutorial();

        state.tutorialDone = true;
        TutorialState.Save(server, state);

        //시간 정지 해제
        if (TutorialFlowManager.Instance != null)
            TutorialFlowManager.Instance.ReleaseTutorialTimePause();

        Debug.Log("튜토리얼 완전히 종료");
    }

    private void ShowNpcNumber(
    GameObject numberObject,
    Transform targetNpc)
    {
        if (numberObject == null || targetNpc == null)
            return;

        RectTransform numberRect =
            numberObject.GetComponent<RectTransform>();

        Canvas canvas =
            numberObject.GetComponentInParent<Canvas>();

        if (numberRect == null || canvas == null)
            return;

        RectTransform canvasRect =
            canvas.GetComponent<RectTransform>();

        if (canvasRect == null)
            return;

        numberObject.SetActive(true);

        Vector3 npcWorldPosition = targetNpc.position;
        Vector2 npcCanvasPosition;

        if (canvas.renderMode == RenderMode.WorldSpace)
        {
            npcCanvasPosition =
                canvasRect.InverseTransformPoint(npcWorldPosition);
        }
        else
        {
            Camera worldCamera = Camera.main;

            if (worldCamera == null)
                return;

            Vector2 screenPosition =
                RectTransformUtility.WorldToScreenPoint(
                    worldCamera,
                    npcWorldPosition);

            Camera uiCamera =
                canvas.renderMode == RenderMode.ScreenSpaceOverlay
                    ? null
                    : (canvas.worldCamera != null
                        ? canvas.worldCamera
                        : worldCamera);

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    screenPosition,
                    uiCamera,
                    out npcCanvasPosition))
                return;
        }

        numberRect.anchoredPosition =
            npcCanvasPosition + npcNumberOffset;
    }

    private void HideNpcNumber(GameObject numberObject)
    {
        if (numberObject != null)
            numberObject.SetActive(false);
    }

    private void UpdateNpcNumberPosition(
    GameObject numberObject,
    Transform targetNpc)
    {
        if (numberObject == null || targetNpc == null)
            return;

        RectTransform numberRect =
            numberObject.GetComponent<RectTransform>();

        Canvas canvas =
            numberObject.GetComponentInParent<Canvas>();

        if (numberRect == null || canvas == null)
            return;

        RectTransform canvasRect =
            canvas.GetComponent<RectTransform>();

        if (canvasRect == null)
            return;

        Vector3 npcWorldPosition = targetNpc.position;
        Vector2 npcCanvasPosition;

        if (canvas.renderMode == RenderMode.WorldSpace)
        {
            npcCanvasPosition =
                canvasRect.InverseTransformPoint(npcWorldPosition);
        }
        else
        {
            Camera worldCamera = Camera.main;

            if (worldCamera == null)
                return;

            Vector2 screenPosition =
                RectTransformUtility.WorldToScreenPoint(
                    worldCamera,
                    npcWorldPosition
                );

            Camera uiCamera =
                canvas.renderMode == RenderMode.ScreenSpaceOverlay
                    ? null
                    : (canvas.worldCamera != null
                        ? canvas.worldCamera
                        : worldCamera);

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    screenPosition,
                    uiCamera,
                    out npcCanvasPosition))
            {
                return;
            }
        }

        numberRect.anchoredPosition =
            npcCanvasPosition + npcNumberOffset;
    }
}
