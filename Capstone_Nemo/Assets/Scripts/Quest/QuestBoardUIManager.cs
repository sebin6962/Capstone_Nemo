using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class QuestBoardUIManager : MonoBehaviour
{
    public static QuestBoardUIManager Instance;

    [Header("전체 UI")]
    [SerializeField] private GameObject questListPopup;
    [SerializeField] private GameObject questDetailPopup;

    [Header("닫기 버튼")]
    [SerializeField] private Button questListCloseButton;
    [SerializeField] private Button questDetailCloseButton;

    [Header("목록")]
    [SerializeField] private Transform questListContent;
    [SerializeField] private GameObject questLineBackgroundPrefab;

    [Header("상세")]
    [SerializeField] private TMP_Text detailTitleText;
    [SerializeField] private TMP_Text detailDescriptionText;
    [SerializeField] private TMP_Text detailRewardText;
    [SerializeField] private Button acceptQuestButton;
    [SerializeField] private TMP_Text acceptQuestButtonText;

    private readonly List<GameObject> currentItems = new();
    private bool isOpen = false;
    private QuestData currentDetailQuestData;

    public bool IsOpen() => isOpen;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (questListPopup != null) questListPopup.SetActive(false);
        if (questDetailPopup != null) questDetailPopup.SetActive(false);

        if (questListCloseButton != null)
        {
            questListCloseButton.onClick.RemoveAllListeners();
            questListCloseButton.onClick.AddListener(CloseAll);
        }

        if (questDetailCloseButton != null)
        {
            questDetailCloseButton.onClick.RemoveAllListeners();
            questDetailCloseButton.onClick.AddListener(CloseQuestDetail);
        }

        if (acceptQuestButton != null)
        {
            acceptQuestButton.onClick.RemoveAllListeners();
            acceptQuestButton.onClick.AddListener(OnClickAcceptQuestButton);
        }
    }

    public void OpenQuestList()
    {
        RefreshQuestList();

        if (questListPopup != null) questListPopup.SetActive(true);
        if (questDetailPopup != null) questDetailPopup.SetActive(false);

        isOpen = true;
    }

    public void CloseAll()
    {
        if (questListPopup != null) questListPopup.SetActive(false);
        if (questDetailPopup != null) questDetailPopup.SetActive(false);

        isOpen = false;
        currentDetailQuestData = null;
    }

    public void CloseQuestDetail()
    {
        if (questDetailPopup != null)
            questDetailPopup.SetActive(false);

        currentDetailQuestData = null;
    }

    public void OpenQuestDetail(QuestData data)
    {
        if (data == null) return;

        currentDetailQuestData = data;

        if (detailTitleText != null)
            detailTitleText.text = data.title;

        if (detailDescriptionText != null)
            detailDescriptionText.text = data.description;

        if (detailRewardText != null)
            detailRewardText.text = $"{data.rewardId} {data.rewardAmount}개";

        RefreshAcceptButtonState();

        if (questDetailPopup != null)
            questDetailPopup.SetActive(true);
    }

    private void OnClickAcceptQuestButton()
    {
        if (currentDetailQuestData == null) return;
        if (QuestAcceptManager.Instance == null) return;

        bool success = QuestAcceptManager.Instance.TryAcceptQuest(currentDetailQuestData);

        if (!success)
        {
            Debug.Log("[QuestBoardUIManager] 퀘스트는 최대 3개까지만 수락할 수 있습니다.");
            RefreshAcceptButtonState();
            return;
        }

        RefreshAcceptButtonState();
        CloseQuestDetail();
        RefreshQuestList();
    }

    private void RefreshAcceptButtonState()
    {
        if (acceptQuestButton == null || acceptQuestButtonText == null)
            return;

        if (currentDetailQuestData == null)
        {
            acceptQuestButton.interactable = false;
            acceptQuestButtonText.text = "수락하기";
            return;
        }

        if (QuestAcceptManager.Instance != null &&
            QuestAcceptManager.Instance.IsAccepted(currentDetailQuestData.id))
        {
            acceptQuestButton.interactable = false;
            acceptQuestButtonText.text = "수락완료";
            return;
        }

        bool canAccept = QuestAcceptManager.Instance == null || QuestAcceptManager.Instance.CanAcceptMore();

        acceptQuestButton.interactable = canAccept;
        acceptQuestButtonText.text = canAccept ? "수락하기" : "수락불가";
    }

    private void RefreshQuestList()
    {
        ClearQuestList();

        if (QuestDatabase.Instance == null || QuestDatabase.Instance.QuestList == null)
            return;

        foreach (QuestData quest in QuestDatabase.Instance.QuestList)
        {
            GameObject lineGO = Instantiate(questLineBackgroundPrefab, questListContent, false);

            RectTransform rt = lineGO.GetComponent<RectTransform>();
            if (rt != null)
                rt.localScale = Vector3.one;

            QuestListItemUI item = lineGO.GetComponent<QuestListItemUI>();
            if (item != null)
                item.Setup(quest, this);

            currentItems.Add(lineGO);
        }
    }

    private void ClearQuestList()
    {
        for (int i = currentItems.Count - 1; i >= 0; i--)
        {
            if (currentItems[i] != null)
                Destroy(currentItems[i]);
        }

        currentItems.Clear();
    }
}