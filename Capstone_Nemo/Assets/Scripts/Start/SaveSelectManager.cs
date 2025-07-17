using UnityEngine;
using UnityEngine.UI;
using TMPro; 
using System.IO;
using System.Collections.Generic;

[System.Serializable]
public class SlotUI
{
    public Button button;
    public TMP_Text txtServerName;
    public TMP_Text txtStarlight;
    public TMP_Text txtLevel;
    public TMP_Text txtPlaytime;
    public TMP_Text txtLastPlayed;
    public Image backgroundImage;
    public Button deleteButton;
}

public class SaveSelectManager : MonoBehaviour
{


    public SlotUI[] saveSlots; // 슬롯 3개 연결
    public GameObject newGamePopup;

    public Color normalSlotColor = Color.white;
    public Color emptySlotColor = Color.gray;

    void OnEnable()
    {
        RefreshSaveSlots();  // 탭 전환 시 다시 로드 가능
    }
    int LoadIntFromFile(string filename, int defaultValue = 0)
    {
        string path = Application.persistentDataPath + "/" + filename;
        if (File.Exists(path))
            return int.Parse(File.ReadAllText(path));
        else
            return defaultValue;
    }

    [System.Serializable]
    public class StarData { public int starlight; }
    [System.Serializable]
    public class LevelData { public int Level; public int Exp; }
    [System.Serializable]
    public class DayData { public int day; }

    int LoadStarlight(string serverName)
    {
        string path = Application.persistentDataPath + $"/playerStarData_{serverName}.json";
        if (File.Exists(path))
        {
            var data = JsonUtility.FromJson<StarData>(File.ReadAllText(path));
            return data.starlight;
        }
        return 0;
    }

    int LoadLevel(string serverName)
    {
        string path = Application.persistentDataPath + $"/player_level_data_{serverName}.json";
        if (File.Exists(path))
        {
            var data = JsonUtility.FromJson<LevelData>(File.ReadAllText(path));
            return data.Level;
        }
        return 1;
    }

    int LoadDay(string serverName)
    {
        string path = Application.persistentDataPath + $"/dayData_{serverName}.json";
        if (File.Exists(path))
        {
            var data = JsonUtility.FromJson<DayData>(File.ReadAllText(path));
            return data.day;
        }
        return 1;
    }
    public void DeleteSave(string serverName)
    {
        Debug.Log($"세이브 삭제: {serverName}");

        // profile 업데이트
        string profilePath = Application.persistentDataPath + "/profile_myuser.json";
        Profile profile = JsonUtility.FromJson<Profile>(File.ReadAllText(profilePath));
        profile.saves.RemoveAll(x => x.serverName == serverName);
        File.WriteAllText(profilePath, JsonUtility.ToJson(profile, true));

        // 관련 파일 삭제
        string[] files = {
        $"save_myuser_{serverName}.json",
        $"playerStarData_{serverName}.json",
        $"player_level_data_{serverName}.json",
        $"dayData_{serverName}.json"
    };
        foreach (var file in files)
        {
            string path = Application.persistentDataPath + "/" + file;
            if (File.Exists(path))
                File.Delete(path);
        }

        RefreshSaveSlots();
    }
    public void RefreshSaveSlots()
    {
        string profilePath = Application.persistentDataPath + "/profile_myuser.json";
        Profile profile = File.Exists(profilePath) ?
            JsonUtility.FromJson<Profile>(File.ReadAllText(profilePath)) :
            new Profile { username = "myuser" };

        List<SaveInfo> validSaves = new List<SaveInfo>();

        foreach (var save in profile.saves)
        {
            string savePath = Application.persistentDataPath + $"/save_myuser_{save.serverName}.json";
            if (File.Exists(savePath))
                validSaves.Add(save);
        }

        for (int i = 0; i < saveSlots.Length; i++)
        {
            var slot = saveSlots[i];
            var btn = slot.button;
            var delBtn = slot.deleteButton;
            btn.onClick.RemoveAllListeners();
            delBtn.onClick.RemoveAllListeners();

            if (i < validSaves.Count)
            {
                var saveInfo = validSaves[i];
                string serverName = saveInfo.serverName;

                var saveData = JsonUtility.FromJson<SaveData>(File.ReadAllText(Application.persistentDataPath + $"/save_myuser_{serverName}.json"));

                int starlight = LoadStarlight(serverName);
                int level = LoadLevel(serverName);
                int day = LoadDay(serverName);

                slot.txtServerName.text = $"‘{saveData.serverName}’";
                slot.txtStarlight.text = $"{starlight} 별빛";
                slot.txtLevel.text = $"{level} Lv";
                slot.txtPlaytime.text = $"플레이 타임 : 약 {(day - 1) * 20}분";
                slot.txtLastPlayed.text = $"마지막 접속 : {saveInfo.lastPlayed}";
                slot.backgroundImage.color = normalSlotColor;

                btn.onClick.AddListener(() =>
                {
                    PlayerPrefs.SetString("SelectedSave", serverName);
                    FadeManager.Instance.FadeToScene("VillageScene");
                });

                delBtn.onClick.AddListener(() =>
                {
                    ConfirmPopup.Instance.Open($"[{serverName}] 세이브 파일을 삭제할까요?", () =>
                    {
                        DeleteSave(serverName);
                    });
                });
            }
            else
            {
                delBtn.gameObject.SetActive(false);
                slot.txtServerName.text = "새 가게 만들기";
                slot.txtStarlight.text = "";
                slot.txtLevel.text = "";
                slot.txtPlaytime.text = "";
                slot.txtLastPlayed.text = "";
                slot.backgroundImage.color = emptySlotColor;

                btn.onClick.AddListener(() =>
                {
                    newGamePopup.SetActive(true);
                });
            }
        }


    }
}

