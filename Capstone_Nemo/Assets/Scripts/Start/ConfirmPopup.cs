using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class ConfirmPopup : MonoBehaviour
{
    public static ConfirmPopup Instance;

    public TMP_Text messageText;
    public Button btnYes;
    public Button btnNo;

    private Action onConfirm;

    void Awake()
    {
        Instance = this;
        gameObject.SetActive(false);
    }

    public void Open(string message, Action confirmAction)
    {
        gameObject.SetActive(true);
        messageText.text = message;
        onConfirm = confirmAction;

        btnYes.onClick.RemoveAllListeners();
        btnNo.onClick.RemoveAllListeners();

        btnYes.onClick.AddListener(() =>
        {
            onConfirm?.Invoke();
            gameObject.SetActive(false);
        });

        btnNo.onClick.AddListener(() =>
        {
            gameObject.SetActive(false);
        });
    }
}
