using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class QuestUIManager : MonoBehaviour
{
    public GameObject questPanel;
    public GameObject questCompletePanel;
    public Image customerPortrait;
    public TextMeshProUGUI questText;
    public TextMeshProUGUI questCompleteText;
    public Button acceptButton;
    public Button completeButton;

    private string[] questLines;
    private string[] questCompleteLines;
    private int currentLineIndex;
    private QuestCustomer currentCustomer;

    private enum DialogueType 
    {
        None, 
        Accept, 
        Complete 
    }
    private DialogueType currentDialogueType = DialogueType.None;

    public void StartQuestDialogue(string[] lines, Sprite portrait, QuestCustomer customer)
    {
        questLines = lines;
        currentDialogueType = DialogueType.Accept;
        currentLineIndex = 0;
        currentCustomer = customer;

        questPanel.SetActive(true);
        customerPortrait.sprite = portrait;
        acceptButton.gameObject.SetActive(false);

        ShowCurrentLine();
    }

    public void StartQuestCompleteDialogue(string[] lines, Sprite portrait, QuestCustomer customer)
    {
        questCompleteLines = lines;
        currentDialogueType = DialogueType.Complete;
        currentLineIndex = 0;
        currentCustomer = customer;

        questCompletePanel.SetActive(true);
        customerPortrait.sprite = portrait;
        completeButton.gameObject.SetActive(false);

        ShowCurrentLine();
    }


    void ShowCurrentLine()
    {
        string[] lines = currentDialogueType == DialogueType.Accept ? questLines : questCompleteLines;

        {
            if (currentLineIndex < lines.Length)
            {
                if (questCompletePanel.activeSelf)
                {
                    questCompleteText.text = lines[currentLineIndex];
                }
                else
                {
                    questText.text = lines[currentLineIndex];
                }
            }
            else
            {
                if (questCompletePanel.activeSelf)
                {
                    completeButton.gameObject.SetActive(true);
                }
                else
                {
                    acceptButton.gameObject.SetActive(true);
                }
            }
        }
    }


    public void OnClickNextLine()
    {
        currentLineIndex++;
        ShowCurrentLine();
    }

    public void OnAcceptButtonClicked()
    {
        questPanel.SetActive(false);
        SFXManager.Instance.PlayBbyongSFX();
        currentCustomer.AcceptQuest(); 
    }

    public void OnCompleteButtonClicked()
    {
        questCompletePanel.SetActive(false);
        SFXManager.Instance.PlayBbyongSFX();
    }
}