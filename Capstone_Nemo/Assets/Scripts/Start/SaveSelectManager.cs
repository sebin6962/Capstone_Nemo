using UnityEngine;
using UnityEngine.UI;
using TMPro; 
using System.IO;

public class SaveSelectManager : MonoBehaviour
{
    public GameObject saveButtonPrefab;   // 버튼 프리팹 (TextMeshProUGUI)
    public Transform contentParent;       // Scroll View > Content 오브젝트
    public Button btnBack;

    void Start()
    {
        btnBack.onClick.AddListener(() =>
        {
            FadeManager.Instance.FadeToScene("StartScene");
        });

        LoadSaveList();
    }

    void LoadSaveList()
    {
        string profilePath = Application.persistentDataPath + "/profile_myuser.json";
        if (!File.Exists(profilePath)) return;
        string json = File.ReadAllText(profilePath);
        Profile profile = JsonUtility.FromJson<Profile>(json);

        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        foreach (var save in profile.saves)
        {
            GameObject go = Instantiate(saveButtonPrefab, contentParent);

            // 각 세이브의 SaveData에서 플레이어 이름 가져오기
            string savePath = Application.persistentDataPath + $"/save_myuser_{save.serverName}.json";
            //string playerName = "(알 수 없음)";
            if (File.Exists(savePath))
            {
                SaveData saveData = JsonUtility.FromJson<SaveData>(File.ReadAllText(savePath));
                //playerName = saveData.playerName;
            }

            // TextMeshProUGUI로 서버이름+플레이어이름 표시
            var txt = go.GetComponentInChildren<TMP_Text>();
            if (txt != null)
                txt.text = $"{save.serverName}";

            var localSave = save;

            go.GetComponent<Button>().onClick.AddListener(() =>
            {
                PlayerPrefs.SetString("SelectedSave", localSave.serverName);
                // PlayerPrefs.SetString("SelectedPlayerName", playerName);
                SceneTransitionInfo.Instance.entranceID = "FromPlayerStore";
                FadeManager.Instance.FadeToScene("VillageScene");
            });
        }
    }
}
