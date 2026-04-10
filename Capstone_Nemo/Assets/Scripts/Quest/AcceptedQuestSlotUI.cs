using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AcceptedQuestSlotUI : MonoBehaviour
{
    [SerializeField] private Image npcImage;
    [SerializeField] private TMP_Text leftText;
    [SerializeField] private TMP_Text rewardText;

    public void Setup(QuestData data)
    {
        if (data == null) return;

        if (leftText != null)
            leftText.text = data.title;

        if (rewardText != null)
            rewardText.text = $"{data.rewardId} {data.rewardAmount}°³";

        if (npcImage != null)
        {
            if (!string.IsNullOrWhiteSpace(data.npcSprite))
            {
                Sprite sprite = Resources.Load<Sprite>($"Sprites/NPC/{data.npcSprite}");

                if (sprite != null)
                {
                    npcImage.sprite = sprite;
                    npcImage.gameObject.SetActive(true);
                }
                else
                {
                    npcImage.sprite = null;
                    npcImage.gameObject.SetActive(false);
                }
            }
            else
            {
                npcImage.sprite = null;
                npcImage.gameObject.SetActive(false);
            }
        }
    }
}