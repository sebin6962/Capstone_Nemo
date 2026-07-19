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
    public TMP_Text txtPlayerName;

    public TMP_Text txtStarlight;
    public TMP_Text txtLevel;

    public TMP_Text Playtime;
    public TMP_Text txtPlaytime;

    public TMP_Text LastPlayed;
    public TMP_Text txtLastPlayed;

    public Image backgroundImage;
    public Button deleteButton;

    public GameObject fixedTextObject;
}

public class SaveSelectManager : MonoBehaviour
{
    [Serializable]
    class PlaytimeData
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

    // 세이브 하나를 지울 때 함께 지울 파일들
    void DeleteServerFiles(string serverName)
    {
        var p = Application.persistentDataPath;

        TryDelete(Path.Combine(p, $"save_myuser_{serverName}.json"));
        TryDelete(Path.Combine(p, $"playerStarData_{serverName}.json"));
        TryDelete(Path.Combine(p, $"player_level_data_{serverName}.json"));
        TryDelete(Path.Combine(p, $"dayData_{serverName}.json"));
        TryDelete(Path.Combine(p, $"timeData_{serverName}.json"));
        TryDelete(Path.Combine(p, $"unlock_{serverName}.json"));
        TryDelete(Path.Combine(p, $"playtime_{serverName}.json"));
        TryDelete(Path.Combine(p, $"storage_{serverName}.json"));
        TryDelete(Path.Combine(p, $"treeUnlock_{serverName}.json"));
        TryDelete(Path.Combine(p, $"tutorial_{serverName}.json"));
        TryDelete(Path.Combine(p, $"ps_tableItem_{serverName}.json"));
        TryDelete(Path.Combine(p, $"playerSkin_{serverName}.json"));
        TryDelete(Path.Combine(p, $"farm_{serverName}.json"));
        TryDelete(Path.Combine(p, $"grassLoot_{serverName}.json"));

        // 레거시 해금 파일
        var legacy = Path.Combine(p, "unlock_state.json");
        if (File.Exists(legacy))
            TryDelete(legacy);

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

        // 현재 선택된 세이브였다면 선택값 초기화
        if (PlayerPrefs.GetString("SelectedSave", "") == serverName)
        {
            PlayerPrefs.DeleteKey("SelectedSave");
            PlayerPrefs.Save();
        }
    }

    void OnEnable()
    {
        RefreshSaveSlots();
    }

    [System.Serializable]
    public class StarData
    {
        public int starlight;
    }

    [System.Serializable]
    public class LevelData
    {
        public int Level;
        public int Exp;
    }

    [System.Serializable]
    public class DayData
    {
        public int day;
    }

    int LoadStarlight(string serverName)
    {
        string path = Path.Combine(
            Application.persistentDataPath,
            $"playerStarData_{serverName}.json"
        );

        if (File.Exists(path))
        {
            var data = JsonUtility.FromJson<StarData>(File.ReadAllText(path));
            if (data != null)
                return data.starlight;
        }

        return 0;
    }

    int LoadLevel(string serverName)
    {
        string path = Path.Combine(
            Application.persistentDataPath,
            $"player_level_data_{serverName}.json"
        );

        if (File.Exists(path))
        {
            var data = JsonUtility.FromJson<LevelData>(File.ReadAllText(path));
            if (data != null)
                return data.Level;
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
        string profilePath = Path.Combine(
            Application.persistentDataPath,
            "profile_myuser.json"
        );

        Profile profile = File.Exists(profilePath)
            ? JsonUtility.FromJson<Profile>(File.ReadAllText(profilePath))
            : new Profile { username = "myuser" };

        if (profile == null)
            profile = new Profile { username = "myuser" };

        if (profile.saves == null)
            profile.saves = new List<SaveInfo>();

        List<SaveInfo> validSaves = new List<SaveInfo>();

        foreach (var save in profile.saves)
        {
            string savePath = Path.Combine(
                Application.persistentDataPath,
                $"save_myuser_{save.serverName}.json"
            );

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

                string savePath = Path.Combine(
                    Application.persistentDataPath,
                    $"save_myuser_{serverName}.json"
                );

                SaveData saveData = JsonUtility.FromJson<SaveData>(
                    File.ReadAllText(savePath)
                );

                if (saveData == null)
                    saveData = new SaveData { serverName = serverName };

                string displayServerName = string.IsNullOrWhiteSpace(saveData.serverName)
                    ? serverName
                    : saveData.serverName;

                // 캐릭터 이름이 없던 기존 세이브도 오류 없이 표시
                string displayPlayerName = string.IsNullOrWhiteSpace(saveData.playerName)
                    ? "이름 미설정"
                    : saveData.playerName;

                int starlight = LoadStarlight(serverName);
                int level = LoadLevel(serverName);

                var pt = LoadPlaytimeData(serverName);

                if (slot.txtServerName != null)
                    slot.txtServerName.text = displayServerName;

                if (slot.txtPlayerName != null)
                {
                    slot.txtPlayerName.gameObject.SetActive(true);
                    slot.txtPlayerName.text = displayPlayerName;
                }

                if (slot.txtStarlight != null)
                    slot.txtStarlight.text = $"{starlight} 별빛";

                if (slot.txtLevel != null)
                    slot.txtLevel.text = $"{level}";

                if (slot.txtPlaytime != null)
                    slot.txtPlaytime.text = FormatHMS(pt.seconds);

                if (slot.txtLastPlayed != null)
                {
                    slot.txtLastPlayed.text = string.IsNullOrEmpty(pt.lastPlayed)
                        ? "-"
                        : pt.lastPlayed;
                }

                if (slot.Playtime != null)
                    slot.Playtime.gameObject.SetActive(true);

                if (slot.LastPlayed != null)
                    slot.LastPlayed.gameObject.SetActive(true);

                if (slot.deleteButton != null)
                    slot.deleteButton.gameObject.SetActive(true);

                if (slot.fixedTextObject != null)
                    slot.fixedTextObject.SetActive(true);

                if (slot.backgroundImage != null)
                    slot.backgroundImage.color = normalSlotColor;

                btn.onClick.AddListener(() =>
                {
                    if (SFXManager.Instance != null)
                        SFXManager.Instance.PlayFileSelectSFX();

                    PlayerPrefs.SetString("SelectedSave", serverName);
                    PlayerPrefs.Save();

                    if (PlayerSkinManager.Instance != null)
                        PlayerSkinManager.Instance.SwitchToSave(serverName);

                    if (StarDataManager.Instance != null)
                        StarDataManager.Instance.InitFromSelectedSave();

                    TutorialFlowManager.ForceResetInstance();

                    if (QuestAcceptManager.Instance != null)
                        QuestAcceptManager.Instance.SwitchToSave(serverName);

                    if (DailyQuestManager.Instance != null)
                        DailyQuestManager.Instance.SwitchToSave(serverName);

                    if (UnlockManager.Instance != null)
                        UnlockManager.Instance.SwitchToServer(serverName);

                    if (VillageSceneManager.Instance != null)
                        VillageSceneManager.Instance.ResetData();

                    SceneTransitionInfo.Instance.entranceID = "FromPlayerStore";
                    FadeManager.Instance.FadeToScene("VillageScene");
                });

                delBtn.onClick.AddListener(() =>
                {
                    if (SFXManager.Instance != null)
                        SFXManager.Instance.PlayBtnClickSFX();

                    ConfirmPopup.Instance.Open(
                        $"[{serverName}] 세이브 파일을 삭제할까요?",
                        () =>
                        {
                            DeleteServerFiles(serverName);
                            RefreshSaveSlots();
                        }
                    );
                });
            }
            else
            {
                if (delBtn != null)
                    delBtn.gameObject.SetActive(false);

                if (slot.fixedTextObject != null)
                    slot.fixedTextObject.SetActive(false);

                if (slot.txtServerName != null)
                    slot.txtServerName.text = "";

                if (slot.txtPlayerName != null)
                {
                    slot.txtPlayerName.text = "";
                    slot.txtPlayerName.gameObject.SetActive(false);
                }

                if (slot.txtStarlight != null)
                    slot.txtStarlight.text = "";

                if (slot.txtLevel != null)
                    slot.txtLevel.text = "";

                if (slot.txtPlaytime != null)
                    slot.txtPlaytime.text = "";

                if (slot.txtLastPlayed != null)
                    slot.txtLastPlayed.text = "";

                if (slot.Playtime != null)
                    slot.Playtime.gameObject.SetActive(false);

                if (slot.LastPlayed != null)
                    slot.LastPlayed.gameObject.SetActive(false);

                if (slot.backgroundImage != null)
                {
                    var emptySprite = Resources.Load<Sprite>(
                        "Sprites/UI/start_file_slot_plus"
                    );

                    slot.backgroundImage.sprite = emptySprite;
                    slot.backgroundImage.color = emptySlotColor;
                }

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
        string path = Path.Combine(
            Application.persistentDataPath,
            $"playtime_{serverName}.json"
        );

        if (!File.Exists(path))
            return new PlaytimeData { seconds = 0, lastPlayed = "" };

        try
        {
            return JsonUtility.FromJson<PlaytimeData>(File.ReadAllText(path))
                   ?? new PlaytimeData { seconds = 0, lastPlayed = "" };
        }
        catch
        {
            return new PlaytimeData { seconds = 0, lastPlayed = "" };
        }
    }

    string FormatHMS(long seconds)
    {
        var ts = TimeSpan.FromSeconds(Math.Max(0, seconds));
        int hh = (int)ts.TotalHours;

        return $"{hh:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
    }
}
