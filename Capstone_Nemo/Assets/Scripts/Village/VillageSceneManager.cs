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
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    void Start()
    {
        // 1. 선택된 서버명 확보
        string serverName = PlayerPrefs.GetString("SelectedSave");

        // 2. 모든 매니저에 서버명 설정 (파일 분리)
        SetupServerNameAllManagers(serverName);

        // 3. 매니저별 파일에서 데이터 불러오기
        StarDataManager.Instance?.LoadStarData();
        PlayerLevelManager.Instance?.Load();
        TreeLevelUnlocker.Instance?.LoadUnlockData();
        StorageInventory.Instance?.LoadStorage();
        TimeManager.Instance?.LoadDay();

        // 4. (기존 시간 흐름 플래그)
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
        // 필요한 매니저 있으면 여기에 추가
    }
}
