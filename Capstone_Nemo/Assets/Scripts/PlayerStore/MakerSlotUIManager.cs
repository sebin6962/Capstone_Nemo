using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MakerSlotUIManager : MonoBehaviour
{
    public List<Image> slotImages; // UI > Image ≈∏¿‘∏∏!

    public void UpdateSlots(List<Sprite> sprites)
    {
        for (int i = 0; i < slotImages.Count; i++)
        {
            if (i < sprites.Count && sprites[i] != null)
            {
                slotImages[i].sprite = sprites[i];
                slotImages[i].gameObject.SetActive(true);
            }
            else
            {
                slotImages[i].sprite = null;
                slotImages[i].gameObject.SetActive(false);
            }
        }
    }

    public void ClearSlots()
    {
        foreach (var img in slotImages)
        {
            img.sprite = null;
            img.gameObject.SetActive(false);
        }
    }
}
