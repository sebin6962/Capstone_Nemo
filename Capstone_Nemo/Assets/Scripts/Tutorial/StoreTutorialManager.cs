using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum StoreTutorialStep
{
    OpenStorage = 0,
    CloseStorage = 1,
    SieveInsert = 2,
    SieveSpace = 3, 
    SieveFinish = 4,
    MixingInsert = 5,
    WaterOn = 6,
    WaterFinish = 7,
    WaterInsert = 8,
    MixingSpace = 9,
    MixingFinish = 10,
    SiruInsert = 11,
    SiruSpace = 12,
    SiruFinish = 13,
    Serve = 14,
    NextOrder = 15,
    DogamCheck = 16,
    DogamCheck2 = 17,
    DogamClose = 18,
    StoreFirst_Finish = 19
}

public class StoreTutorialManager : MonoBehaviour
{
    public static StoreTutorialManager Instance;

    [SerializeField] private GameObject[] stepPanels;

    [SerializeField] private GameObject storeTutorialPanel;
    [SerializeField] private CustomerSpawner customerSpawner;
    [SerializeField] private SeatManager seatManager;

    [SerializeField] private GameObject tutorialBlocker;

    [SerializeField] private List<TutorialDialogueLine> storeStartDialogues;
    [SerializeField] private List<TutorialDialogueLine> afterSiruFinishDialogues;
    [SerializeField] private List<TutorialDialogueLine> afterNextOrderDialogues;

    private TutorialStateData state;
    private string server;

    public StoreTutorialStep currentStep = StoreTutorialStep.OpenStorage;

    private Coroutine showStepPanelRoutine;

    public bool IsStoreTutorialRunning;

    private IEnumerator StartStoreTutorialAfterTransition(GlobalTutorialStep globalStep)
    {
        // 원형 전환 오브젝트가 존재하고, 전환 중이거나 검은 화면이 켜져 있으면 대기
        if (CircleSceneTransition.Instance != null)
        {
            yield return new WaitUntil(() =>
                !CircleSceneTransition.Instance.IsTransitioning &&
                !CircleSceneTransition.Instance.IsCoverVisible
            );
        }

        // 전환이 끝난 뒤 살짝 텀
        yield return new WaitForSecondsRealtime(0.2f);

        StartStoreTutorial(globalStep);
    }

    private IEnumerator StartStoreSecondTutorialAfterTransition()
    {
        if (CircleSceneTransition.Instance != null)
        {
            yield return new WaitUntil(() =>
                !CircleSceneTransition.Instance.IsTransitioning &&
                !CircleSceneTransition.Instance.IsCoverVisible
            );
        }

        yield return new WaitForSecondsRealtime(0.2f);

        // 여기에서 방앗간 이후 가게 튜토리얼을 시작하면 됩니다.
        // 예시:
        // StartStoreSecondTutorial();

        Debug.Log("[StoreTutorial] PlayerStore_Second 전환 완료 후 튜토리얼 시작 가능");
    }

    void Awake()
    {
        if(Instance != null && Instance != this)
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
            IsStoreTutorialRunning = false;
            return;
        }

        if (flow.currentStep == GlobalTutorialStep.PlayerStore_First)
        {
            /*IsStoreTutorialRunning = true;
            currentStep = StoreTutorialStep.OpenStorage;

            if (customerSpawner)
                customerSpawner.SpawnTutorialCustomer("baekseolgi_finish", 15f);

            ShowStepPanel(currentStep);*/

            //StartStoreTutorial(flow.currentStep);
            StartCoroutine(StartStoreTutorialAfterTransition(flow.currentStep));
        }
        else if (flow.currentStep == GlobalTutorialStep.PlayerStore_Second)
        {
            StartCoroutine(StartStoreSecondTutorialAfterTransition());
        }
        else
        {
            HideAllPanels();
            IsStoreTutorialRunning = false;
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

    void ShowStepWithOptionalDialogue(StoreTutorialStep step)
    {
        switch (step)
        {
            case StoreTutorialStep.NextOrder:
                PlayDialogueThen(() =>
                {
                    ShowStepPanel(step);
                }, afterSiruFinishDialogues);
                break;

            case StoreTutorialStep.DogamCheck:
                PlayDialogueThen(() =>
                {
                    ShowStepPanel(step);
                }, afterNextOrderDialogues);
                break;

            default:
                ShowStepPanel(step);
                break;
        }
    }

    void StartStoreTutorial(GlobalTutorialStep globalStep)
    {
        IsStoreTutorialRunning = true;
        currentStep = StoreTutorialStep.OpenStorage;

        if (TutorialFlowManager.Instance != null)
        {
            TutorialFlowManager.Instance.RequestTutorialTimePause();
            TutorialFlowManager.Instance.LockScenePortal();
        }

        if (customerSpawner)
            customerSpawner.SpawnSeatedTutorialCustomer("baekseolgi_finish", 1f);

        PlayDialogueThen(() =>
        {
            ShowStepPanel(currentStep);
        }, storeStartDialogues);
    }

    void ShowStepPanel(StoreTutorialStep step)
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
        showStepPanelRoutine = StartCoroutine(ShowStepPanelAfterDelay(step, 0.3f));
    }

    private IEnumerator ShowStepPanelAfterDelay(StoreTutorialStep step, float delay)
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

    public bool IsCurrentStep(StoreTutorialStep step)
    {
        return IsStoreTutorialRunning && currentStep == step;
    }

    public void GoToNextStep()
    {
        switch (currentStep)
        {
            //HeldItemManager.cs
            case StoreTutorialStep.OpenStorage:
                currentStep = StoreTutorialStep.CloseStorage;
                break;
            //PlayerInteract.cs
            case StoreTutorialStep.CloseStorage:
                currentStep = StoreTutorialStep.SieveInsert;
                break;
            case StoreTutorialStep.SieveInsert:
                currentStep = StoreTutorialStep.SieveSpace;
                break;
            case StoreTutorialStep.SieveSpace:
                currentStep = StoreTutorialStep.SieveFinish;
                break;
            case StoreTutorialStep.SieveFinish:
                currentStep = StoreTutorialStep.MixingInsert;
                break;
            case StoreTutorialStep.MixingInsert:
                currentStep = StoreTutorialStep.WaterOn;
                break;
            case StoreTutorialStep.WaterOn:
                currentStep = StoreTutorialStep.WaterFinish;
                break;
            case StoreTutorialStep.WaterFinish:
                currentStep = StoreTutorialStep.WaterInsert;
                break;
            case StoreTutorialStep.WaterInsert:
                currentStep = StoreTutorialStep.MixingSpace;
                break;
                //MakerInfo.cs
            case StoreTutorialStep.MixingSpace:
                currentStep = StoreTutorialStep.MixingFinish;
                break;
            //PlayerInteract.cs
            case StoreTutorialStep.MixingFinish:
                currentStep = StoreTutorialStep.SiruInsert;
                break;
            case StoreTutorialStep.SiruInsert:
                currentStep = StoreTutorialStep.SiruSpace;
                break;
            //MakerInfo.cs
            case StoreTutorialStep.SiruSpace:
                currentStep = StoreTutorialStep.SiruFinish;
                break;
            //MakerInfo.cs
            case StoreTutorialStep.SiruFinish:
                currentStep = StoreTutorialStep.Serve;
                break;
            //customer.cs
            case StoreTutorialStep.Serve:
                currentStep = StoreTutorialStep.NextOrder;
                break;
            //DogamUIManager.cs
            case StoreTutorialStep.NextOrder:
                currentStep = StoreTutorialStep.DogamCheck;
                break;
            case StoreTutorialStep.DogamCheck:
                currentStep = StoreTutorialStep.DogamCheck2;
                break;
            case StoreTutorialStep.DogamCheck2:
                currentStep = StoreTutorialStep.DogamClose;
                break;
            case StoreTutorialStep.DogamClose:
                currentStep = StoreTutorialStep.StoreFirst_Finish;
                if (TutorialFlowManager.Instance != null)
                    TutorialFlowManager.Instance.UnlockScenePortal();
                tutorialBlocker.SetActive(false);
                break;
            case StoreTutorialStep.StoreFirst_Finish:
                CompleteStoreTutorial();
                return;

            default:
                return;
        }

        ShowStepWithOptionalDialogue(currentStep);
    }

    public void CompleteStoreTutorial()
    {
        if (!IsStoreTutorialRunning)
        {
            Debug.Log("[StoreTutorial] CompleteStoreTutorial called but not running");
            return;
        }

        IsStoreTutorialRunning = false;
        HideAllPanels();

        if (customerSpawner)
            customerSpawner.EndTutorial();

        //시간 정지 해제
        if (TutorialFlowManager.Instance != null)
            TutorialFlowManager.Instance.ReleaseTutorialTimePause();

        TutorialFlowManager.Instance.SetStep(GlobalTutorialStep.Village_Second);
        Debug.Log($"[StoreTutorial] Complete -> Flow step NOW = {TutorialFlowManager.Instance.currentStep}");
    }
}
