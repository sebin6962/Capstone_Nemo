using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.SceneManagement;

public class TutorialDialogueManager : MonoBehaviour
{
    public static TutorialDialogueManager Instance;

    [Header("UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private Image portraitImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Button nextButton;

    private List<TutorialDialogueLine> currentLines;
    private int currentIndex;
    private Action onDialogueFinished;
    public static bool IsDialogueOpen { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(ShowNextLine);
        }
    }

    public void SetDialogueScale(float scale)
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.transform.localScale = Vector3.one * scale;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "PlayerStoreScene")
        {
            SetDialogueScale(0.85f);
        }
        else if (scene.name == "VillageScene")
        {
            SetDialogueScale(1f);
        }
        else
        {
            SetDialogueScale(1f); //±âº»°ª
        }
    }

    public void StartDialogue(List<TutorialDialogueLine> lines, Action onFinished = null)
    {
        if (lines == null || lines.Count == 0)
        {
            onFinished?.Invoke();
            return;
        }

        if (dialoguePanel == null)
        {
            onFinished?.Invoke();
            return;
        }

        currentLines = lines;
        currentIndex = 0;
        onDialogueFinished = onFinished;


        IsDialogueOpen = true;
        dialoguePanel.SetActive(true);
        ShowCurrentLine();
    }

    private void ShowCurrentLine()
    {
        if (currentLines == null || currentLines.Count == 0)
            return;

        TutorialDialogueLine line = currentLines[currentIndex];

        if (nameText != null)
            nameText.text = line.speakerName;

        if (dialogueText != null)
            dialogueText.text = line.dialogue;

        if (portraitImage != null)
        {
            portraitImage.sprite = line.portrait;
            portraitImage.gameObject.SetActive(line.portrait != null);
        }
    }

    private void ShowNextLine()
    {
        if (currentLines == null || currentLines.Count == 0)
            return;

        currentIndex++;

        if (currentIndex >= currentLines.Count)
        {
            EndDialogue();
            return;
        }

        ShowCurrentLine();
    }

    private void EndDialogue()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
        IsDialogueOpen = false;

        Action finishedCallback = onDialogueFinished;

        currentLines = null;
        currentIndex = 0;
        onDialogueFinished = null;

        finishedCallback?.Invoke();
    }

    public bool IsDialoguePlaying()
    {
        return dialoguePanel != null && dialoguePanel.activeSelf;
    }

    public void ForceCloseDialogue()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        currentLines = null;
        currentIndex = 0;
        onDialogueFinished = null;
    }
}