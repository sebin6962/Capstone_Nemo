using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class StartUIManager : MonoBehaviour
{
    public Button btnSelectSave;
    public Button btnNewGame;

    private void Start()
    {
        btnSelectSave.onClick.RemoveAllListeners();
        btnNewGame.onClick.RemoveAllListeners();

        btnSelectSave.onClick.AddListener(() =>
        {
            FadeManager.Instance.FadeToScene("SaveSelectScene");
        });
        btnNewGame.onClick.AddListener(() =>
        {
            FadeManager.Instance.FadeToScene("NewGameScene");
        });
    }
}
