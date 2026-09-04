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

    [SerializeField] private List<TutorialDialogueLine> secondStoreStartDialogues;
    [SerializeField] private List<TutorialDialogueLine> afterSiruFinishDialogues;

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
            customerSpawner.SpawnSeatedTutorialCustomer("Danhobakseolgi_finish", 0f);

        PlayDialogueThen(() =>
        {
            ShowStepPanel(currentStep);
        }, secondStoreStartDialogues);
    }

    void ShowStepWithOptionalDialogue(SecondStoreTutorialStep step)
    {
        switch (step)
        {
            case SecondStoreTutorialStep.Serve: 
                PlayDialogueThen(() =>
                {
                    ShowStepPanel(step);
                }, afterSiruFinishDialogues);
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

    void ShowStepPanel(SecondStoreTutorialStep step)
    {
        if (showStepPanelRoutine != null)
            StopCoroutine(showStepPanelRoutine);

        showStepPanelRoutine = StartCoroutine(ShowStepPanelAfterDelay(step, 0.3f));
    }

    private IEnumerator ShowStepPanelAfterDelay(
    SecondStoreTutorialStep step,
    float delay
)
    {
        // 튜토리얼 중 시간이 정지되어도 동작하도록 Realtime 사용
        yield return new WaitForSecondsRealtime(delay);

        if (SFXManager.Instance != null)
            SFXManager.Instance.PlayTutorialSFX();

        HideAllPanels();

        int index = (int)step;

        if (stepPanels == null ||
            index < 0 ||
            index >= stepPanels.Length)
        {
            Debug.LogWarning(
                $"[SecondStoreTutorial] " +
                $"stepPanels에 {step} 패널이 없습니다. index={index}"
            );

            showStepPanelRoutine = null;
            yield break;
        }

        if (stepPanels[index] != null)
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

    public bool CanConsumeTutorialMaterial(
    params SecondStoreTutorialStep[] allowedSteps
)
    {
        // 해당 튜토리얼 진행 중이 아니면 평소처럼 허용
        if (!IsStoreTutorialRunning)
            return true;

        // 현재 단계가 허용된 단계 중 하나인지 검사
        if (allowedSteps != null)
        {
            foreach (SecondStoreTutorialStep allowedStep in allowedSteps)
            {
                if (currentStep == allowedStep)
                    return true;
            }
        }

        Debug.LogWarning(
            $"[SecondStoreTutorial] 잘못된 공정으로 인한 재료 유실 차단. " +
            $"현재 단계={currentStep}"
        );

        ReShowCurrentStepPanel();

        return false;
    }

    public void ReShowCurrentStepPanel()
    {
        if (!IsStoreTutorialRunning)
            return;

        // 현재 켜져 있는 패널을 먼저 끄고
        HideAllPanels();

        // 0.3초 후 현재 단계 패널을 다시 표시
        ShowStepPanel(currentStep);
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

        ShowStepWithOptionalDialogue(currentStep);
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

        Debug.Log("튜토리얼끝!");
    }
}
