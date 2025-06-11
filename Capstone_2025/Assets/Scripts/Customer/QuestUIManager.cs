using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class QuestUIManager : MonoBehaviour
{
    public GameObject questPanel;
    public Image customerPortrait;
    public TextMeshProUGUI questText;
    public Button acceptButton;

    private string[] currentLines;
    private int currentLineIndex;
    private QuestCustomer currentCustomer;

    public void StartQuestDialogue(string[] lines, Sprite portrait, QuestCustomer customer)
    {
        currentLines = lines;
        currentLineIndex = 0;
        currentCustomer = customer;

        questPanel.SetActive(true);
        customerPortrait.sprite = portrait;
        acceptButton.gameObject.SetActive(false);

        ShowCurrentLine();
    }

   
    void ShowCurrentLine()
    {
        if (currentLineIndex < currentLines.Length)
        {
            questText.text = currentLines[currentLineIndex];
        }
        else
        {
            acceptButton.gameObject.SetActive(true);
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
        currentCustomer.AcceptQuest(); 
    }
}