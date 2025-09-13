using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.IO;

public class NewGamePopupManager : MonoBehaviour
{
    public TMP_InputField inputServerName;
    public Button btnCreate, btnCancel;

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

            if (profile.saves.Exists(x => x.serverName == serverName)) return;

            profile.saves.Add(new SaveInfo
            {
                serverName = serverName,
                created = DateTime.Now.ToString("s"),
                lastPlayed = DateTime.Now.ToString("s")
            });
            File.WriteAllText(profilePath, JsonUtility.ToJson(profile, true));

            File.WriteAllText(Application.persistentDataPath + $"/save_myuser_{serverName}.json", JsonUtility.ToJson(new SaveData { serverName = serverName }, true));
            File.WriteAllText(Application.persistentDataPath + $"/playerStarData_{serverName}.json", "{\"starlight\":0}");
            File.WriteAllText(Application.persistentDataPath + $"/player_level_data_{serverName}.json", "{\"Level\":1,\"Exp\":0}");
            File.WriteAllText(Application.persistentDataPath + $"/dayData_{serverName}.json", "{\"day\":1,\"hour\":9,\"minute\":0}");

            PlayerPrefs.SetString("SelectedSave", serverName);
            FadeManager.Instance.FadeToScene("CutScene");
        });
    }
}