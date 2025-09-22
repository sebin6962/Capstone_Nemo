using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using TMPro;
using System.Collections.Generic;

public class NewGameManager : MonoBehaviour
{
    public TMP_InputField inputServerName;
    //public TMP_InputField inputPlayerName;
    public Button btnCreate;
    public Button btnCancel;

    void Start()
    {
        btnCancel.onClick.AddListener(() =>
        {
            FadeManager.Instance.FadeToScene("StartScene");
        });

        btnCreate.onClick.AddListener(OnCreateClicked);
    }

    void OnCreateClicked()
    {
        string serverName = inputServerName.text.Trim();
        //string playerName = inputPlayerName.text.Trim();

        // 서버 이름 입력해야만 생성
        if (string.IsNullOrEmpty(serverName))
            return;

        //if (string.IsNullOrEmpty(serverName) || string.IsNullOrEmpty(playerName))
        //return;

        string profilePath = Application.persistentDataPath + "/profile_myuser.json";
        Profile profile;
        if (File.Exists(profilePath))
            profile = JsonUtility.FromJson<Profile>(File.ReadAllText(profilePath));
        else
            profile = new Profile { username = "myuser" };

        if (profile.saves.Exists(x => x.serverName == serverName)) return;

        SaveInfo info = new SaveInfo
        {
            serverName = serverName,
            created = DateTime.Now.ToString("s"),
            lastPlayed = DateTime.Now.ToString("s")
        };
        profile.saves.Add(info);
        File.WriteAllText(profilePath, JsonUtility.ToJson(profile, true));

        string savePath = Application.persistentDataPath + $"/save_myuser_{serverName}.json";
        SaveData saveData = new SaveData
        {
            serverName = serverName,

            //playerName = playerName // 플레이어 이름 저장
        };
        File.WriteAllText(savePath, JsonUtility.ToJson(saveData, true));

        File.WriteAllText(Application.persistentDataPath + $"/playerStarData_{serverName}.json", "{\"starlight\":0}");
        File.WriteAllText(Application.persistentDataPath + $"/player_level_data_{serverName}.json", "{\"Level\":1,\"Exp\":0}");
        File.WriteAllText(Application.persistentDataPath + $"/dayData_{serverName}.json", "{\"day\":1}");
        //File.WriteAllText(Application.persistentDataPath + $"/timeData_{serverName}.json", "{\"hour\":9, \"minute\":0}");

        PlayerPrefs.SetString("SelectedSave", serverName);
        FadeManager.Instance.FadeToScene("CutScene");
    }
}