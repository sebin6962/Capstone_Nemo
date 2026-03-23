using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SecondStoreTutorialStep
{
    OpenStorage = 0,
    CloseStorage = 1,
    MixingInsert = 2,
    WaterOn = 3,
    WaterFinish = 4,
    WaterInsert = 5,
    MixingSpace = 6,
    MixingFinish = 7,
    SiruInsert = 8,
    SiruSpace = 9,
    SiruFinish = 10,
    Serve = 11,
    StoreSecond_Finish = 12
}

public class SecondStoreTutorialManager : MonoBehaviour
{
    public static SecondStoreTutorialManager Instance;

    [SerializeField] private GameObject[] stepPanels;

    [SerializeField] private GameObject storeTutorialPanel;
    [SerializeField] private CustomerSpawner customerSpawner;
    [SerializeField] private SeatManager seatManager;

    [SerializeField] private GameObject tutorialBlocker;

    private TutorialStateData state;
    private string server;

    public SecondStoreTutorialStep currentStep = SecondStoreTutorialStep.OpenStorage;

    private Coroutine showStepPanelRoutine;

    public bool IsStoreTutorialRunning;

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

        if (flow == null || state.tutorialDone || flow.currentStep == GlobalTutorialStep.Done)
        {
            HideAllPanels();
            IsStoreTutorialRunning = false;
            return;
        }

        if (flow.currentStep == GlobalTutorialStep.PlayerStore_Second)
        {
            StartStoreTutorial();
        }
        else
        {
            HideAllPanels();
            IsStoreTutorialRunning = false;
        }
    }

    void StartStoreTutorial()
    {
        IsStoreTutorialRunning = true;
        currentStep = SecondStoreTutorialStep.OpenStorage;

        if (TutorialFlowManager.Instance != null)
        {
            TutorialFlowManager.Instance.RequestTutorialTimePause();
            TutorialFlowManager.Instance.LockScenePortal();
        }

        if (customerSpawner)
            customerSpawner.SpawnTutorialCustomer(1, "Danhobakseolgi_finish", 15f);

        ShowStepPanel(currentStep);
    }

    void ShowStepPanel(SecondStoreTutorialStep step)
    {
        if (showStepPanelRoutine != null)
            StopCoroutine(showStepPanelRoutine);

        showStepPanelRoutine = StartCoroutine(ShowStepPanelAfterDelay(step, 0.3f));
    }

    private IEnumerator ShowStepPanelAfterDelay(SecondStoreTutorialStep step, float delay)
    {
        yield return new WaitForSeconds(delay);

        SFXManager.Instance.PlayTutorialSFX();
        HideAllPanels();

        int index = (int)step;
        if (stepPanels == null || index < 0 || index >= stepPanels.Length)
            yield break;

        if (stepPanels[index])
            stepPanels[index].SetActive(true);

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

    public bool IsCurrentStep(SecondStoreTutorialStep step)
    {
        return IsStoreTutorialRunning && currentStep == step;
    }

    public void GoToNextStep()
    {
        switch (currentStep)
        {
            //HeldItemManager.cs
            case SecondStoreTutorialStep.OpenStorage:
                currentStep = SecondStoreTutorialStep.CloseStorage;
                break;

            //PlayerInteract.cs
            case SecondStoreTutorialStep.CloseStorage:
                currentStep = SecondStoreTutorialStep.MixingInsert;
                break;

            case SecondStoreTutorialStep.MixingInsert:
                currentStep = SecondStoreTutorialStep.WaterOn;
                break;

            case SecondStoreTutorialStep.WaterOn:
                currentStep = SecondStoreTutorialStep.WaterFinish;
                break;

            case SecondStoreTutorialStep.WaterFinish:
                currentStep = SecondStoreTutorialStep.WaterInsert;
                break;

            case SecondStoreTutorialStep.WaterInsert:
                currentStep = SecondStoreTutorialStep.MixingSpace;
                break;

            case SecondStoreTutorialStep.MixingSpace:
                currentStep = SecondStoreTutorialStep.MixingFinish;
                break;

            case SecondStoreTutorialStep.MixingFinish:
                currentStep = SecondStoreTutorialStep.SiruInsert;
                break;

            case SecondStoreTutorialStep.SiruInsert:
                currentStep = SecondStoreTutorialStep.SiruSpace;
                break;

            case SecondStoreTutorialStep.SiruSpace:
                currentStep = SecondStoreTutorialStep.SiruFinish;
                break;

            case SecondStoreTutorialStep.SiruFinish:
                currentStep = SecondStoreTutorialStep.Serve;
                break;

            case SecondStoreTutorialStep.Serve:
                currentStep = SecondStoreTutorialStep.StoreSecond_Finish;
                if (TutorialFlowManager.Instance != null)
                    TutorialFlowManager.Instance.UnlockScenePortal();
                break;
            /*case SecondStoreTutorialStep.Serve:
                currentStep = SecondStoreTutorialStep.StoreSecond_Finish;
                if (TutorialFlowManager.Instance != null)
                    TutorialFlowManager.Instance.UnlockScenePortal();
                break;*/

            case SecondStoreTutorialStep.StoreSecond_Finish:
                FinishStoreSecondTutorial();
                return;

            default:
                return;
        }

        ShowStepPanel(currentStep);
    }

    public void FinishStoreSecondTutorial()
    {
        if (!IsStoreTutorialRunning)
        {
            Debug.Log("[SecondStoreTutorial] CompleteStoreSecondTutorial called but not running");
            return;
        }

        IsStoreTutorialRunning = false;
        HideAllPanels();

        if (customerSpawner)
            customerSpawner.EndTutorial();

        if (TutorialFlowManager.Instance != null)
        {
            TutorialFlowManager.Instance.ReleaseTutorialTimePause();
            TutorialFlowManager.Instance.FinishAllTutorial();
        }

        Debug.Log("Æ©Åä¸®¾ó³¡!");
    }
}
