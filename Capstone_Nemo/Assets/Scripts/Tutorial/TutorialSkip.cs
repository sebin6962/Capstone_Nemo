 using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class TutorialSkip : MonoBehaviour
{
    public static TutorialSkip Instance;

    [SerializeField] private GameObject skipButton;
    [SerializeField] private GameObject confirmPopup;
    [SerializeField] private string[] tutorialScenes;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
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

        string currentScene = SceneManager.GetActiveScene().name;

        bool isTutorialScene = false;

        foreach (string sceneName in tutorialScenes)
        {
            if (currentScene == sceneName)
            {
                isTutorialScene = true;
                break;
            }
        }

        bool shouldShowSkipButton =
            tutorialRunning && isTutorialScene;

        if (skipButton != null &&
            skipButton.activeSelf != shouldShowSkipButton)
        {
            skipButton.SetActive(shouldShowSkipButton);
        }

        if (!shouldShowSkipButton &&
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
            return;

        StartCoroutine(SkipTutorialRoutine());
    }

    private IEnumerator SkipTutorialRoutine()
    {
        if (FadeManager.Instance == null)
        {
            TutorialFlowManager.Instance.FinishAllTutorial();

            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            yield break;
        }

        yield return StartCoroutine(FadeManager.Instance.FadeOut());

        TutorialFlowManager.Instance.FinishAllTutorial();

        string currentScene = SceneManager.GetActiveScene().name;

        SceneManager.LoadScene(currentScene);

        yield return null;

        yield return StartCoroutine(FadeManager.Instance.FadeIn());
    }
}
