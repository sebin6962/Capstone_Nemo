using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/*public enum TutorialStep
{
    None,
    DogamIntro,
    Village,
    Shop
}*/

public enum VillageSecondStep
{
    GoToField = 0,
    OpenStorage = 1,
    PickUpSeed = 2,
    PlantSeed = 3,
    OpenStorage2 = 4,
    RestoreSeed = 5,
    PickUp_WateringCan = 6,
    Water = 7,
    CropGrowing = 8,
    Restore_WateringCan = 9,
    HarvestCrop = 10,
    GoToMill = 11,
    VillageSecond_Finish = 12
}

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    //public TutorialStep currentStep = TutorialStep.None;
    private VillageSecondStep villageSecondStep = VillageSecondStep.GoToField;

    [SerializeField] private Button dogamButton;
    [SerializeField] private Button dogamCloseButton;
    [SerializeField] private ParticleSystem dogamHighlightFX;   // FX의 루트 PS
    [SerializeField] private CanvasGroup overlayBlocker;
    [SerializeField] private PlayerManager player;
    [SerializeField] private DoGamUIManager dogamUI;
    [SerializeField] private GameObject tutorialText;

    [SerializeField] private GameObject villageTutorialPanel;

    [SerializeField] private GameObject tutorialBlocker;
    //[SerializeField] private GameObject secondTutorialBlocker;

    [SerializeField] private GameObject[] stepPanels;
    [SerializeField] private GameObject[] fixedStepPanels;

    private string server;
    private TutorialStateData state;

    public bool IsVillageSecondTutorialRunning;

    private Coroutine showStepPanelRoutine;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        server = PlayerPrefs.GetString("SelectedSave", "default");
        state = TutorialState.Load(server);

        var flow = TutorialFlowManager.Instance;

        if (state.tutorialDone || flow == null || flow.currentStep == GlobalTutorialStep.Done)
        {
            CleanupTutorialVisuals();
            if (tutorialBlocker) tutorialBlocker.SetActive(false);
            //if (secondTutorialBlocker) secondTutorialBlocker.SetActive(false);
            return;
        }

        var globalStep = TutorialFlowManager.Instance.currentStep;

        Debug.Log($"[TutorialManager] Start in Village, flowStep={flow?.currentStep}, tutorialDone={state.tutorialDone}");


        switch (globalStep)
        {
            case GlobalTutorialStep.DogamIntro:
                //currentStep = TutorialStep.DogamIntro;
                tutorialBlocker.gameObject.SetActive(true);
               // secondTutorialBlocker.gameObject.SetActive(false);
                StartTutorial_DogamIntro();
                break;

            case GlobalTutorialStep.Village_First:
                break;

            case GlobalTutorialStep.Village_Second:
                StartTutorial_VillageSecond();
                break;

            default:
                CleanupTutorialVisuals();
                tutorialBlocker.SetActive(false);
                break;
        }
    }

    void StartTutorial_DogamIntro()
    {
        //시간 정지, 이동X
        SetPlayerInput(false);

        if (TutorialFlowManager.Instance != null)
            TutorialFlowManager.Instance.RequestTutorialTimePause();

        //Time.timeScale = 0f;

        //클릭X
        if (overlayBlocker)
        {
            overlayBlocker.gameObject.SetActive(true);
            overlayBlocker.blocksRaycasts = true;
            overlayBlocker.interactable = true;
            overlayBlocker.alpha = 0.9f;
        }

        if (tutorialText)
        {
            tutorialText.gameObject.SetActive(true);
        }

        if (dogamCloseButton)
        {
            dogamCloseButton.onClick.RemoveListener(OnDogamClicked);
            dogamCloseButton.onClick.AddListener(OnDogamClicked);
            dogamCloseButton.interactable = true;
        }

        //dlvprxm
        if (dogamHighlightFX)
        {
            dogamHighlightFX.gameObject.SetActive(true);

            var pSystems = dogamHighlightFX.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in pSystems)
            {
                var main = ps.main;
                main.useUnscaledTime = true;  // Time.timeScale=0에서도 재생
                ps.Clear(true);
                ps.Play(true);
            }
        }
    }

    void StartTutorial_VillageSecond()
    {
        //시간 정지
        if (TutorialFlowManager.Instance != null)
        {
            TutorialFlowManager.Instance.RequestTutorialTimePause();
            TutorialFlowManager.Instance.LockScenePortal();
        }

        CleanupTutorialVisuals();

        if (tutorialBlocker)
            tutorialBlocker.SetActive(false);

        /*if (secondTutorialBlocker)
            secondTutorialBlocker.SetActive(false);*/

        IsVillageSecondTutorialRunning = true;
        villageSecondStep = VillageSecondStep.GoToField;
        ShowStepPanel(villageSecondStep);
    }

    public void GoToNextVillageSecondStep()
    {

        switch (villageSecondStep)
        {
            //TutorialTriggerArea.cs
            case VillageSecondStep.GoToField:
                //secondTutorialBlocker.gameObject.SetActive(true);
                villageSecondStep = VillageSecondStep.OpenStorage;    
                break;
            //BoxInventoryManager.cs
            case VillageSecondStep.OpenStorage:
                villageSecondStep = VillageSecondStep.PickUpSeed;
                break;
            //HeldItemManager.cs
            case VillageSecondStep.PickUpSeed:
                villageSecondStep = VillageSecondStep.PlantSeed;
                break;
            case VillageSecondStep.PlantSeed:
                villageSecondStep = VillageSecondStep.OpenStorage2;
                break;
            //FarmManager.cs
            case VillageSecondStep.OpenStorage2:
                villageSecondStep = VillageSecondStep.RestoreSeed;
                break;
            //HeldItemManager.cs
            case VillageSecondStep.RestoreSeed:
                villageSecondStep = VillageSecondStep.PickUp_WateringCan;
                break;
            //WateringCanAnchor.cs
            case VillageSecondStep.PickUp_WateringCan:
                villageSecondStep = VillageSecondStep.Water;
                break;
            //FarmManager.cs
            case VillageSecondStep.Water:
                villageSecondStep = VillageSecondStep.CropGrowing;
                break;
            //FarmManager.cs
            case VillageSecondStep.CropGrowing:
                villageSecondStep = VillageSecondStep.Restore_WateringCan;
                break;
            //WateringCanAnchor.cs
            case VillageSecondStep.Restore_WateringCan:
                villageSecondStep = VillageSecondStep.HarvestCrop;
                break;
            //FarmManager.cs
            case VillageSecondStep.HarvestCrop:
                if (TutorialFlowManager.Instance != null)
                    TutorialFlowManager.Instance.UnlockScenePortal();
                villageSecondStep = VillageSecondStep.GoToMill;
                break;
            case VillageSecondStep.GoToMill:
                villageSecondStep = VillageSecondStep.VillageSecond_Finish;
                break;
            case VillageSecondStep.VillageSecond_Finish:
                CompleteVillageSecondTutorial();
                return;

            default:
                //secondTutorialBlocker.gameObject.SetActive(false);
                return;
        }

        ShowStepPanel(villageSecondStep);

        UpdateTriggerAreas();
    }

    public void UpdateTriggerAreas()
    {
        var areas = FindObjectsOfType<TutorialTriggerArea>(true);

        foreach (var area in areas)
        {
            bool shouldBeActive = IsCurrentStep(area.TargetStep);
            area.gameObject.SetActive(shouldBeActive);
        }
    }

    public void CompleteVillageSecondTutorial()
    {
        if (!IsVillageSecondTutorialRunning)
            return;

        IsVillageSecondTutorialRunning = false;
        {
            HideAllPanels();
        }

        TutorialFlowManager.Instance.SetStep(GlobalTutorialStep.Mill);
    }

    void ShowStepPanel(VillageSecondStep step)
    {
        if (showStepPanelRoutine != null)
            StopCoroutine(showStepPanelRoutine);

        // 2초 뒤에 패널 켜는 코루틴으로 빼뒀어요!!
        showStepPanelRoutine = StartCoroutine(ShowStepPanelAfterDelay(step, 1.0f));
    }

    private IEnumerator ShowStepPanelAfterDelay(VillageSecondStep step, float delay)
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

        if (fixedStepPanels != null && index >= 0 && index < fixedStepPanels.Length && fixedStepPanels[index])
            fixedStepPanels[index].SetActive(true);


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

        if (fixedStepPanels != null)
        {
            foreach (var panel in fixedStepPanels)
                if (panel) panel.SetActive(false);
        }
    }

    public bool IsCurrentStep(VillageSecondStep step)
    { 
        return IsVillageSecondTutorialRunning && villageSecondStep == step;
    }

    void OnDogamClicked()
    {
        if (TutorialFlowManager.Instance.currentStep != GlobalTutorialStep.DogamIntro)
        {
            return;
        }

        if (overlayBlocker)
        {
            overlayBlocker.blocksRaycasts = false;
            overlayBlocker.interactable = false;
            overlayBlocker.alpha = 0f;
            overlayBlocker.gameObject.SetActive(false);
        }

        if (tutorialText)
        {
            tutorialText.gameObject.SetActive(false);
        }

        if (dogamHighlightFX)
        {
            var pSystems = dogamHighlightFX.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in pSystems)
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            dogamHighlightFX.gameObject.SetActive(false);
        }

        SetPlayerInput(true);

        //if (villageTutorialPanel)
        //{
        //    SFXManager.Instance.PlayBbyongSFX();
        //    villageTutorialPanel.SetActive(true);
        //}

        //currentStep = TutorialStep.Village;

        //지금 시간만 멈추고 플레이어만 움직이게 할 수 없는듯? 추후 수정 예정
        //우선 시간 흐르게...
        //Time.timeScale = 1f;

        if (dogamCloseButton != null)
            dogamCloseButton.onClick.RemoveListener(OnDogamClicked);

        //효과음 넣으니까 겹쳐서 2초 지연 코루틴으로 뺐습니다!
        StartCoroutine(ShowVillageTutorialAfterDelay(1.2f));
    }

    private IEnumerator ShowVillageTutorialAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (villageTutorialPanel)
        {
            SFXManager.Instance.PlayTutorialSFX();
            villageTutorialPanel.SetActive(true);
        }

        //currentStep = TutorialStep.Village;

        TutorialFlowManager.Instance.SetStep(GlobalTutorialStep.Village_First);
    }


    /*void ResumeAndComplete()
    {
        SetPlayerInput(true);
        Time.timeScale = 1f;

        CleanupTutorialVisuals();

        state.tutorialDone = true;
        TutorialState.Save(server, state);

        if (dogamCloseButton != null)
            dogamCloseButton.onClick.RemoveListener(OnDogamClicked);
    }*/


    void CleanupTutorialVisuals()
    {
        if (overlayBlocker)
        {
            overlayBlocker.blocksRaycasts = false;
            overlayBlocker.interactable = false;
            overlayBlocker.alpha = 0f;
            overlayBlocker.gameObject.SetActive(false);
        }

        if (tutorialText)
        {
            tutorialText.gameObject.SetActive(false);
        }

        if (dogamHighlightFX)
        {
            var pSystems = dogamHighlightFX.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in pSystems)
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            dogamHighlightFX.gameObject.SetActive(false);
        }
    }

    void SetPlayerInput(bool enable)
    {
        if (player != null) player.enabled = enable;
    }

    /*public void FinishAllTutorial()
    {
        CompleteVillageSecondTutorial();

        state.tutorialDone = true;
        TutorialState.Save(server, state);

        //시간 정지 해제
        if (TutorialFlowManager.Instance != null)
            TutorialFlowManager.Instance.ReleaseTutorialTimePause();

        Debug.Log("튜토리얼 완전히 종료");
    }*/
}
