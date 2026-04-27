using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestListItemUI : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Image npcImage;
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text buttonText;

    private QuestData questData;
    private QuestBoardUIManager uiManager;

    public void Setup(QuestData data, QuestBoardUIManager manager)
    {
        questData = data;
        uiManager = manager;

        if (titleText != null)
            titleText.text = data.title;

        SetupNpcImage(data);
        RefreshButtonState();

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClickItem);
        }
    }

    public void RefreshButtonState()
    {
        if (button == null || buttonText == null || questData == null)
            return;

        bool isAccepted = QuestAcceptManager.Instance != null &&
                          QuestAcceptManager.Instance.IsAccepted(questData.id);

        buttonText.text = isAccepted ? "수락완료" : "확인하기";
        button.interactable = !isAccepted;
    }

    private void SetupNpcImage(QuestData data)
    {
        if (npcImage == null)
            return;

        if (data == null || string.IsNullOrWhiteSpace(data.npcSprite))
        {
            npcImage.sprite = null;
            npcImage.gameObject.SetActive(false);
            return;
        }

        Sprite sprite = Resources.Load<Sprite>($"Sprites/NPC/{data.npcSprite}");

        if (sprite != null)
        {
            npcImage.sprite = sprite;
            npcImage.preserveAspect = true;
            npcImage.gameObject.SetActive(true);
        }
        else
        {
            npcImage.sprite = null;
            npcImage.gameObject.SetActive(false);
        }
    }

    private void OnClickItem()
    {
        if (uiManager != null)
            uiManager.OpenQuestDetail(questData);
    }
}