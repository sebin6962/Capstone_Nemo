using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class VillageSceneManager : MonoBehaviour
{
    public static VillageSceneManager Instance;
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        ResetData();
    }

    public void ResetData()
    {
        string serverName = PlayerPrefs.GetString("SelectedSave", "");

        SetupServerNameAllManagers(serverName);

        StarDataManager.Instance?.SetServerName(serverName);
        StarDataManager.Instance?.LoadStarData();

        PlayerLevelManager.Instance?.SetServerName(serverName);
        PlayerLevelManager.Instance?.Load();

        TreeLevelUnlocker.Instance?.SetServerName(serverName);
        TreeLevelUnlocker.Instance?.LoadUnlockData();

        StorageInventory.Instance?.SetServerName(serverName);
        StorageInventory.Instance?.LoadStorage();

        TimeManager.Instance?.SetServerName(serverName);
        TimeManager.Instance?.LoadDay();

        UnlockManager.Instance?.SetServerName(serverName);
        UnlockManager.Instance?.LoadUnlockData();

        if (PlayerPrefs.GetInt("StartTimeOnEnter", 0) == 1)
        {
            PlayerPrefs.SetInt("StartTimeOnEnter", 0);
            TimeManager.Instance?.SetTimeFlow(true);
        }
    }

    void SetupServerNameAllManagers(string serverName)
    {
        StarDataManager.Instance?.SetServerName(serverName);
        PlayerLevelManager.Instance?.SetServerName(serverName);
        TreeLevelUnlocker.Instance?.SetServerName(serverName);
        StorageInventory.Instance?.SetServerName(serverName);
        TimeManager.Instance?.SetServerName(serverName);
    }
}
