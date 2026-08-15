using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

    [SerializeField] private GameObject millTutorialPanel;
/*    [SerializeField] private CustomerSpawner customerSpawner;
    [SerializeField] private SeatManager seatManager;*/

    //[SerializeField] private GameObject tutorialBlocker;

    private TutorialStateData state;
    private string server;

    public MillTutorialStep currentStep = MillTutorialStep.TalkToNpc;

    private Coroutine showStepPanelRoutine;


    public bool IsMillTutorialRunning;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
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

        //시간 정지
        if (TutorialFlowManager.Instance != null)
        {
            TutorialFlowManager.Instance.RequestTutorialTimePause();
            TutorialFlowManager.Instance.LockScenePortal();
        }


        /*if (customerSpawner)
            customerSpawner.SpawnTutorialCustomer("baekseolgi_finish", 15f);*/

        PlayDialogueThen(() =>
        {
            ShowStepPanel(currentStep);
        }, millStartDialogues);
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
        yield return new WaitForSeconds(delay);

        SFXManager.Instance.PlayTutorialSFX();
        HideAllPanels();

        int index = (int)step;
        if (stepPanels == null || index < 0 || index >= stepPanels.Length)
            yield break;

        if (stepPanels[index])
        {
            stepPanels[index].SetActive(true);
        }

        // 코루틴 끝남
        showStepPanelRoutine = null;
    }

    void HideAllPanels()
    {
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

    public void GoToNextMillStep()
    {
        switch (currentStep)
        {
            //MillNpc.cs
            case MillTutorialStep.TalkToNpc:
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
                break;
            case MillTutorialStep.OpenStore:
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
                }, afterTalkToNpcDialogues);
                break;

            case MillTutorialStep.GrindQuit: 
                PlayDialogueThen(() =>
                {
                    ShowStepPanel(step);
                }, afterGrindDialogues);
                break;

            case MillTutorialStep.QuitStore: 
                PlayDialogueThen(() =>
                {
                    ShowStepPanel(step);
                }, afterOpenStoreDialogues);
                break;

            case MillTutorialStep.Mill_Finish: 
                PlayDialogueThen(() =>
                {
                    ShowStepPanel(step);
                }, afterQuitStoreDialogues);
                break;

            default:
                ShowStepPanel(step);
                break;
        }
    }

    void PlayDialogueThen(System.Action onFinished, List<TutorialDialogueLine> lines)
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

        NPCDialogueUIManager.Instance.OpenTutorialDialogue(lines, () =>
        {
            onFinished?.Invoke();
        });
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
}
