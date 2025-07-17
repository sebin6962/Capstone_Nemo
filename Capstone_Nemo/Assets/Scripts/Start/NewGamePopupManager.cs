using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.IO;

public class NewGamePopupManager : MonoBehaviour
{
    public TMP_Text slotText;
    public TMP_InputField inputServerName;
    public Button btnCreate, btnCancel;
    private string currentSlot;

    void Start()
    {
        btnCancel.onClick.AddListener(() =>
        {
            gameObject.SetActive(false);
        });

        btnCreate.onClick.AddListener(() =>
        {
            string serverName = inputServerName.text.Trim();
            if (string.IsNullOrEmpty(serverName)) return;

            string profilePath = Application.persistentDataPath + "/profile_myuser.json";
            Profile profile = File.Exists(profilePath) ?
                JsonUtility.FromJson<Profile>(File.ReadAllText(profilePath)) :
                new Profile { username = "myuser" };

            if (profile.saves.Exists(x => x.serverName == currentSlot)) return;

            var newInfo = new SaveInfo
            {
                serverName = currentSlot,
                created = DateTime.Now.ToString("s"),
                lastPlayed = DateTime.Now.ToString("s")
            };
            profile.saves.Add(newInfo);
            File.WriteAllText(profilePath, JsonUtility.ToJson(profile, true));

            string savePath = Application.persistentDataPath + $"/save_myuser_{currentSlot}.json";
            SaveData saveData = new SaveData { serverName = currentSlot };
            File.WriteAllText(savePath, JsonUtility.ToJson(saveData, true));

            PlayerPrefs.SetString("SelectedSave", currentSlot);
            FadeManager.Instance.FadeToScene("CutScene");
        });
    }

    public void SetSlot(string slotName)
    {
        currentSlot = slotName;
        slotText.text = $"½½·Ô: {slotName}";
        inputServerName.text = "";
    }
}