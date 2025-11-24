using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum StoreTutorialStep
{
    OpenStorage = 0,
    Sieve = 1,
    SieveFinish = 2,
    Mixing = 3,
    Siru = 4,
    Serve = 5,
    NextOrder = 6,
    DogamCheck = 7,
    StoreFirst_Finish = 8
}

public class StoreTutorialManager : MonoBehaviour
{
    public static StoreTutorialManager Instance;

    [SerializeField] private GameObject[] stepPanels;

    [SerializeField] private GameObject storeTutorialPanel;
    [SerializeField] private CustomerSpawner customerSpawner;
    [SerializeField] private SeatManager seatManager;

    [SerializeField] private GameObject tutorialBlocker;

    private TutorialStateData state;
    private string server;

    private StoreTutorialStep currentStep = StoreTutorialStep.StoreFirst_Finish;

    private Coroutine showStepPanelRoutine;


    public bool IsStoreTutorialRunning;

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
            IsStoreTutorialRunning = true;
            currentStep = StoreTutorialStep.OpenStorage;

            if (customerSpawner)
                customerSpawner.SpawnTutorialCustomer("baekseolgi_finish", 15f);

            ShowStepPanel(currentStep);
        }
        else
        {
            // 그 외 상황에서는 튜토리얼 안 켜고 조용히 있음
            HideAllPanels();
            IsStoreTutorialRunning = false;
        }
    }

    void StartStoreTutorial(GlobalTutorialStep globalStep)
    {
        IsStoreTutorialRunning = true;
        currentStep = StoreTutorialStep.OpenStorage;

        if (customerSpawner)
            customerSpawner.SpawnTutorialCustomer("baekseolgi_finish", 15f);

        ShowStepPanel(currentStep);
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
        showStepPanelRoutine = StartCoroutine(ShowStepPanelAfterDelay(step, 2.2f));
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
            //PlayerInteract.cs
            case StoreTutorialStep.OpenStorage:
                currentStep = StoreTutorialStep.Sieve;
                break;
            case StoreTutorialStep.Sieve:
                currentStep = StoreTutorialStep.SieveFinish;
                break;
            case StoreTutorialStep.SieveFinish:
                currentStep = StoreTutorialStep.Mixing;
                break;
            case StoreTutorialStep.Mixing:
                currentStep = StoreTutorialStep.Siru;
                break;
            case StoreTutorialStep.Siru:
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
                currentStep = StoreTutorialStep.StoreFirst_Finish;
                tutorialBlocker.SetActive(false);
                break;
            case StoreTutorialStep.StoreFirst_Finish:
                CompleteStoreTutorial();
                return;

            default:
                return;
        }

        ShowStepPanel(currentStep);
    }

    public void CompleteStoreTutorial()
    {
        if (!IsStoreTutorialRunning)
            return;

        IsStoreTutorialRunning = false;
        {
            HideAllPanels();
        }

        if (customerSpawner)
            customerSpawner.EndTutorial();

        TutorialFlowManager.Instance.SetStep(GlobalTutorialStep.Village_Second);
    }
}
