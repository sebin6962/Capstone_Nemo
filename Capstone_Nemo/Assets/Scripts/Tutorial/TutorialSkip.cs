 using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialSkip : MonoBehaviour
{
    public static TutorialSkip Instance;

    [SerializeField] private GameObject skipButton;
    [SerializeField] private GameObject confirmPopup;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (confirmPopup != null)
            confirmPopup.SetActive(false);
    }

    private void Start()
    {
        RefreshSkipButton();
    }

    private void Update()
    {
        RefreshSkipButton();
    }

    private void RefreshSkipButton()
    {
        if (TutorialFlowManager.Instance == null)
            return;

        bool tutorialRunning =
            TutorialFlowManager.Instance.currentStep != GlobalTutorialStep.None &&
            TutorialFlowManager.Instance.currentStep != GlobalTutorialStep.Done;

        if (skipButton != null &&
            skipButton.activeSelf != tutorialRunning)
        {
            skipButton.SetActive(tutorialRunning);
        }

        if (!tutorialRunning &&
            confirmPopup != null &&
            confirmPopup.activeSelf)
        {
            confirmPopup.SetActive(false);
        }
    }

    public void OpenSkipPopup()
    {
        if (confirmPopup != null)
            confirmPopup.SetActive(true);
    }

    public void CancelSkip()
    {
        if (confirmPopup != null)
            confirmPopup.SetActive(false);
    }

    public void ConfirmSkip()
    {
        if (confirmPopup != null)
            confirmPopup.SetActive(false);

        if (TutorialFlowManager.Instance == null)
        {
            Debug.LogError("[TutorialSkip] TutorialFlowManager가 없습니다.");
            return;
        }

        //전체 튜토리얼 완료+저장+시간정지/포털잠금 해제
        TutorialFlowManager.Instance.FinishAllTutorial();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
