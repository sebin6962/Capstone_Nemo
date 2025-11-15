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
    Finish = 6
}

public class StoreTutorialManager : MonoBehaviour
{
    public static StoreTutorialManager Instance;

    [SerializeField] private GameObject[] stepPanels;

    [SerializeField] private GameObject storeTutorialPanel;
    [SerializeField] private CustomerSpawner customerSpawner;
    [SerializeField] private SeatManager seatManager;

    private TutorialStateData state;
    private string server;

    private StoreTutorialStep currentStep = StoreTutorialStep.Finish;

    public bool IsStoreTutorialRunning
    {
        get;
        private set;
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

        if (!state.tutorialDone)
        {
            StartStoreTutorial();
        }
        else
        {
            HideAllPanels();
        }
    }

    void StartStoreTutorial()
    {
        IsStoreTutorialRunning = true;
        currentStep = StoreTutorialStep.OpenStorage;

        if (customerSpawner)
            customerSpawner.SpawnTutorialCustomer("baekseolgi_finish", 15f);

        ShowStepPanel(currentStep);
    }

    void ShowStepPanel(StoreTutorialStep step)
    {
        HideAllPanels();

        int index = (int)step;
        if (stepPanels == null || index < 0 || index >= stepPanels.Length)
            return;

        if (stepPanels[index])
        {
            stepPanels[index].SetActive(true);
        }
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
                currentStep = StoreTutorialStep.Finish;
                break;
            case StoreTutorialStep.Finish:
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

        state.tutorialDone = true;
        TutorialState.Save(server, state);

        Debug.Log("튜토리얼 종료");
    }
}
