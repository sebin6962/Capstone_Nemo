using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GlobalTutorialStep
{
    None = 0,

    DogamIntro,
    Village_First,
    PlayerStore_First,
    Village_Second,
    Mill,
    PlayerStore_Second,

    Done
}

public class TutorialFlowManager : MonoBehaviour
{
    public static TutorialFlowManager Instance;

    public bool IsScenePortalLocked { get; private set; } = false;

    public GlobalTutorialStep currentStep = GlobalTutorialStep.None;

    //튜토리얼 전 자동 컷신용 추가
    public bool VillageIntroAutoSequencePlayed { get; set; } = false;

    private string server;
    private TutorialStateData state;

    private int timePauseRequestCount = 0;

    public static void ForceResetInstance()
    {
        if (Instance != null)
        {
            Debug.Log("[TutorialFlow] ForceResetInstance: 예전 인스턴스 파괴");
            Destroy(Instance.gameObject);
            Instance = null;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        //server = PlayerPrefs.GetString("SelectedSave", "default");
        //state = TutorialState.Load(server);

        //if (state.tutorialDone)
        //{
        //    currentStep = GlobalTutorialStep.Done;
        //}

        //else
        //{
        //    currentStep = GlobalTutorialStep.DogamIntro;
        //}

        InitializeForCurrentSave();
    }

    public void InitializeForCurrentSave()
    {
        server = PlayerPrefs.GetString("SelectedSave", "default");
        state = TutorialState.Load(server);

        timePauseRequestCount = 0;
        VillageIntroAutoSequencePlayed = false;
        UpdateTimeFlow();

        if (state.tutorialDone)
        {
            currentStep = GlobalTutorialStep.Done;
        }
        else
        {
            currentStep = GlobalTutorialStep.DogamIntro;
        }

        Debug.Log($"[TutorialFlow] InitializeForCurrentSave : server={server}, tutorialDone={state.tutorialDone}, step={currentStep}");
    }

    public void LockScenePortal()
    {
        IsScenePortalLocked = true;
    }

    public void UnlockScenePortal()
    {
        IsScenePortalLocked = false;
    }

    public void RequestTutorialTimePause()
    {
        timePauseRequestCount++;
        UpdateTimeFlow();
    }

    public void ReleaseTutorialTimePause()
    {
        timePauseRequestCount--;
        if (timePauseRequestCount < 0)
            timePauseRequestCount = 0;

        UpdateTimeFlow();
    }

    private void UpdateTimeFlow()
    {
        bool shouldFlow = (timePauseRequestCount == 0);

        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.SetTimeFlow(shouldFlow);
        }
    }

    public void SetStep(GlobalTutorialStep step)
    {
        currentStep = step;
        //후에 기능 확장.... 안할지도?굳이.., 중간세이브..
    }

    public void FinishAllTutorial()
    {
        currentStep = GlobalTutorialStep.Done;
        state.tutorialDone = true;
        TutorialState.Save(server, state);

        UnlockScenePortal();

        while (timePauseRequestCount > 0)
        {
            ReleaseTutorialTimePause();
        }

        if (RecipeQuickViewUI.Instance != null && RecipeQuickViewUI.Instance.infoText != null)
        {
            RecipeQuickViewUI.Instance.infoText.SetActive(true);
        }
    }

}
