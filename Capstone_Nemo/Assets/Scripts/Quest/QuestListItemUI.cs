using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestListItemUI : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Image npcImage;
    [SerializeField] private Button button;

    private QuestData questData;
    private QuestBoardUIManager uiManager;

    public void Setup(QuestData data, QuestBoardUIManager manager)
    {
        questData = data;
        uiManager = manager;

        if (titleText != null)
            titleText.text = data.title;

        SetupNpcImage(data);

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClickItem);
        }
    }

    private void SetupNpcImage(QuestData data)
    {
        if (npcImage == null)
        {
            Debug.LogWarning("[QuestListItemUI] npcImage 연결이 안 되어 있습니다.");
            return;
        }

        if (data == null)
        {
            npcImage.gameObject.SetActive(false);
            return;
        }

        Debug.Log($"[QuestListItemUI] quest={data.title}, npcSprite={data.npcSprite}");

        if (string.IsNullOrWhiteSpace(data.npcSprite))
        {
            Debug.LogWarning($"[QuestListItemUI] npcSprite 값이 비어 있습니다. quest={data.title}");
            npcImage.sprite = null;
            npcImage.gameObject.SetActive(false);
            return;
        }

        Sprite sprite = Resources.Load<Sprite>($"Sprites/NPC/{data.npcSprite}");

        if (sprite == null)
        {
            Debug.LogWarning($"[QuestListItemUI] 스프라이트 로드 실패: Resources/Sprites/NPC/{data.npcSprite}");
            npcImage.sprite = null;
            npcImage.gameObject.SetActive(false);
            return;
        }

        npcImage.sprite = sprite;
        npcImage.preserveAspect = true;
        npcImage.gameObject.SetActive(true);

        Debug.Log($"[QuestListItemUI] 스프라이트 로드 성공: {data.npcSprite}");
    }

    private void OnClickItem()
    {
        if (uiManager != null)
            uiManager.OpenQuestDetail(questData);
    }
}