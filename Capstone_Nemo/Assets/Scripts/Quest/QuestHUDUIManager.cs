using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestHUDUIManager : MonoBehaviour
{
    public static QuestHUDUIManager Instance;

    [Header("상단 HUD")]
    [SerializeField] private GameObject rootObject;
    [SerializeField] private Button toggleButton;
    [SerializeField] private TMP_Text questCountText;
    [SerializeField] private Image arrowImage;
    [SerializeField] private Sprite arrowDownSprite;
    [SerializeField] private Sprite arrowUpSprite;

    [Header("리스트")]
    [SerializeField] private GameObject questListPanel;
    [SerializeField] private Transform questSlotParent;
    [SerializeField] private GameObject acceptedQuestSlotPrefab;

    private readonly List<GameObject> spawnedSlots = new();
    private bool isExpanded = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        if (toggleButton != null)
        {
            toggleButton.onClick.RemoveAllListeners();
            toggleButton.onClick.AddListener(ToggleList);
        }

        SetExpanded(false);
    }

    private void OnEnable()
    {
        RefreshAcceptedQuestUI();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void ToggleList()
    {
        SetExpanded(!isExpanded);
    }

    public void SetExpanded(bool expanded)
    {
        isExpanded = expanded;

        if (questListPanel != null)
            questListPanel.SetActive(isExpanded);

        if (arrowImage != null)
            arrowImage.sprite = isExpanded ? arrowUpSprite : arrowDownSprite;
    }

    public void RefreshAcceptedQuestUI()
    {
        ClearSlots();

        if (QuestAcceptManager.Instance == null)
        {
            if (questCountText != null) questCountText.text = "0";
            if (rootObject != null) rootObject.SetActive(true);
            if (questListPanel != null) questListPanel.SetActive(false);
            return;
        }

        IReadOnlyList<QuestData> accepted = QuestAcceptManager.Instance.AcceptedQuests;

        if (questCountText != null)
            questCountText.text = accepted.Count.ToString();

        if (rootObject != null)
            rootObject.SetActive(true);

        if (accepted.Count == 0)
        {
            isExpanded = false;

            if (questListPanel != null)
                questListPanel.SetActive(false);

            if (arrowImage != null)
                arrowImage.sprite = arrowDownSprite;
        }

        for (int i = 0; i < accepted.Count; i++)
        {
            QuestData quest = accepted[i];
            if (quest == null) continue;

            GameObject slot = Instantiate(acceptedQuestSlotPrefab, questSlotParent, false);

            AcceptedQuestSlotUI slotUI = slot.GetComponent<AcceptedQuestSlotUI>();
            if (slotUI != null)
                slotUI.Setup(quest);

            spawnedSlots.Add(slot);
        }
    }

    private void ClearSlots()
    {
        for (int i = spawnedSlots.Count - 1; i >= 0; i--)
        {
            if (spawnedSlots[i] != null)
                Destroy(spawnedSlots[i]);
        }

        spawnedSlots.Clear();
    }
}
