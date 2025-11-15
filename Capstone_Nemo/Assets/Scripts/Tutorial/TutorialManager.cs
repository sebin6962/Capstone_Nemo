using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum TutorialStep
{
    None,
    DogamIntro,
    Village,
    Shop
}

public class TutorialManager : MonoBehaviour
{
    public TutorialStep currentStep = TutorialStep.None;

    [SerializeField] private Button dogamButton;
    [SerializeField] private Button dogamCloseButton;
    [SerializeField] private ParticleSystem dogamHighlightFX;   // FX의 루트 PS
    [SerializeField] private CanvasGroup overlayBlocker;
    [SerializeField] private PlayerManager player;
    [SerializeField] private DoGamUIManager dogamUI;
    [SerializeField] private GameObject tutorialText;

    [SerializeField] private GameObject villageTutorialPanel;

    private string server;
    private TutorialStateData state;

    void Start()
    {
        server = PlayerPrefs.GetString("SelectedSave", "default");
        state = TutorialState.Load(server);

        if (!state.tutorialDone)
        {
            currentStep = TutorialStep.DogamIntro;
            StartTutorial_DogamIntro();
        }
        else
        {
            CleanupTutorialVisuals();
        }
    }

    void StartTutorial_DogamIntro()
    {
        //시간 정지, 이동X
        SetPlayerInput(false);
        Time.timeScale = 0f;

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

    void OnDogamClicked()
    {
        if (currentStep != TutorialStep.DogamIntro)
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

        if (villageTutorialPanel)
        {
            villageTutorialPanel.SetActive(true);
        }

        currentStep = TutorialStep.Village;

        if (dogamCloseButton != null)
            dogamCloseButton.onClick.RemoveListener(OnDogamClicked);

        //지금 시간만 멈추고 플레이어만 움직이게 할 수 없는듯? 추후 수정 예정
        //우선 시간 흐르게...
        Time.timeScale = 1f;
    }

    void ResumeAndComplete()
    {
        SetPlayerInput(true);
        Time.timeScale = 1f;

        CleanupTutorialVisuals();

        state.tutorialDone = true;
        TutorialState.Save(server, state);

        if (dogamCloseButton != null)
            dogamCloseButton.onClick.RemoveListener(OnDogamClicked);
    }

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
}
