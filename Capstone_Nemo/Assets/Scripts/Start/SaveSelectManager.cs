using UnityEngine;
using UnityEngine.UI;
using TMPro; 
using System.IO;
using System.Collections.Generic;
using System;

[System.Serializable]
public class SlotUI
{
    public Button button;
    public TMP_Text txtServerName;
    public TMP_Text txtStarlight;
    public TMP_Text txtLevel;
    public TMP_Text Playtime;
    public TMP_Text txtPlaytime;
    public TMP_Text LastPlayed;
    public TMP_Text txtLastPlayed;
    public Image backgroundImage;
    public Button deleteButton;
}


public class SaveSelectManager : MonoBehaviour
{
    [Serializable] class PlaytimeData 
    { 
        public long seconds; 
        public string lastPlayed; 
    }

    public SlotUI[] saveSlots; // 슬롯 3개 연결
    public GameObject newGamePopup;

    public Color normalSlotColor = Color.white;
    public Color emptySlotColor = Color.gray;

    static void TryDelete(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log($"[Delete] {path}");
        }
    }

    // 세이브(서버) 하나를 지울 때 함께 지울 파일들
    void DeleteServerFiles(string serverName)
    {
        var p = Application.persistentDataPath;

        // 1) 이 서버의 개별 데이터 파일들
        TryDelete(Path.Combine(p, $"save_myuser_{serverName}.json"));
        TryDelete(Path.Combine(p, $"playerStarData_{serverName}.json"));
        TryDelete(Path.Combine(p, $"player_level_data_{serverName}.json"));
        TryDelete(Path.Combine(p, $"dayData_{serverName}.json"));
        TryDelete(Path.Combine(p, $"timeData_{serverName}.json"));   // 사용 중이면
        TryDelete(Path.Combine(p, $"unlock_{serverName}.json"));     // 퍼서버 해금 저장을 사용한다면
        TryDelete(Path.Combine(p, $"playtime_{serverName}.json"));
        TryDelete(Path.Combine(p, $"storage_{serverName}.json"));
        TryDelete(Path.Combine(p, $"treeUnlock_{serverName}.json"));
        TryDelete(Path.Combine(p, $"tutorial_{serverName}.json"));
        TryDelete(Path.Combine(p, $"ps_tableItem_{serverName}.json"));
        TryDelete(Path.Combine(p, $"playerSkin_{serverName}.json"));
        TryDelete(System.IO.Path.Combine(p, $"farm_{serverName}.json"));
        TryDelete(Path.Combine(p, $"grassLoot_{serverName}.json"));

        // 레거시 해금 파일 더 이상 쓰지 않는다면 지우기
        var legacy = Path.Combine(p, "unlock_state.json");
        if (File.Exists(legacy)) TryDelete(legacy);

        // 프로필 목록에서 엔트리 제거
        var profilePath = Path.Combine(p, "profile_myuser.json");
        if (File.Exists(profilePath))
        {
            var json = File.ReadAllText(profilePath);
            var profile = JsonUtility.FromJson<Profile>(json);
            if (profile != null && profile.saves != null)
            {
                profile.saves.RemoveAll(s => s.serverName == serverName);
                File.WriteAllText(profilePath, JsonUtility.ToJson(profile, true));
            }
        }

        // 4) 현재 선택된 세이브였다면 선택값 초기화
        if (PlayerPrefs.GetString("SelectedSave", "") == serverName)
            PlayerPrefs.DeleteKey("SelectedSave");
    }

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
        DeleteServerFiles(serverName);
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

                var pt = LoadPlaytimeData(serverName);
                slot.txtPlaytime.text = FormatHMS(pt.seconds);
                slot.txtLastPlayed.text = string.IsNullOrEmpty(pt.lastPlayed) ? "-" : pt.lastPlayed;
                slot.Playtime.gameObject.SetActive(true);
                slot.LastPlayed.gameObject.SetActive(true);
                slot.txtServerName.text = $"{saveData.serverName}";
                slot.txtStarlight.text = $"{starlight} 별빛";
                slot.txtLevel.text = $"{level}";
                
                //slot.txtPlaytime.text = $"플레이 타임 : 약 {(day - 1) * 20}분";
                //slot.txtLastPlayed.text = $"마지막 접속 : {saveInfo.lastPlayed}";
                slot.backgroundImage.color = normalSlotColor;

                btn.onClick.AddListener(() =>
                {
                    if (SFXManager.Instance != null)
                        SFXManager.Instance.PlayFileSelectSFX();

                    PlayerPrefs.SetString("SelectedSave", serverName);
                    PlayerPrefs.Save();

                    // 세이브 전환 시 데이터 매니저들도 새 세이브 기준으로 다시 로드
                    if (PlayerSkinManager.Instance != null)
                    {
                        PlayerSkinManager.Instance.SwitchToSave(serverName);
                    }

                    if (StarDataManager.Instance != null)
                    {
                        StarDataManager.Instance.InitFromSelectedSave();
                    }

                    TutorialFlowManager.ForceResetInstance();

                    if (QuestAcceptManager.Instance != null)
                    {
                        QuestAcceptManager.Instance.SwitchToSave(serverName);
                    }

                    if (DailyQuestManager.Instance != null)
                    {
                        DailyQuestManager.Instance.SwitchToSave(serverName);
                    }

                    // 해금 매니저에 이 서버로 전환하라고 알려줌
                    if (UnlockManager.Instance != null)
                    {
                        UnlockManager.Instance.SwitchToServer(serverName);
                    }

                    if (VillageSceneManager.Instance != null)
                    {
                        VillageSceneManager.Instance.ResetData();
                    }

                    SceneTransitionInfo.Instance.entranceID = "FromPlayerStore";
                    FadeManager.Instance.FadeToScene("VillageScene");
                });

                delBtn.onClick.AddListener(() =>
                {
                    if (SFXManager.Instance != null)
                        SFXManager.Instance.PlayBtnClickSFX();

                    ConfirmPopup.Instance.Open($"[{serverName}] 세이브 파일을 삭제할까요?", () =>
                    {
                        //DeleteSave(serverName);
                        DeleteServerFiles(serverName); 
                        RefreshSaveSlots();
                    });
                });
            }
            else
            {
                delBtn.gameObject.SetActive(false);
                slot.txtServerName.text = "";
                slot.txtStarlight.text = "";
                slot.txtLevel.text = "";
                slot.txtPlaytime.text = "";
                slot.txtLastPlayed.text = "";
                slot.Playtime.gameObject.SetActive(false);
                slot.LastPlayed.gameObject.SetActive(false);
                var emptySprite = Resources.Load<Sprite>("Sprites/UI/start_file_slot_plus");
                slot.backgroundImage.sprite = emptySprite;

                btn.onClick.AddListener(() =>
                {
                    if (SFXManager.Instance != null)
                        SFXManager.Instance.PlayFileSelectSFX();
                    newGamePopup.SetActive(true);
                });
            }
        }
    }

    PlaytimeData LoadPlaytimeData(string serverName)
    {
        string path = Application.persistentDataPath + $"/playtime_{serverName}.json";
        if (!File.Exists(path)) return new PlaytimeData { seconds = 0, lastPlayed = "" };
        try { return JsonUtility.FromJson<PlaytimeData>(File.ReadAllText(path)); }
        catch { return new PlaytimeData { seconds = 0, lastPlayed = "" }; }
    }

    string FormatHMS(long seconds)
    {
        var ts = TimeSpan.FromSeconds(Math.Max(0, seconds));
        // 총 시간은 HH 누적(24 넘으면 25:13:02 처럼 표기)
        int hh = (int)ts.TotalHours;
        return $"{hh:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
    }
}

