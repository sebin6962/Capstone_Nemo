using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HeldItemManager : MonoBehaviour
{
    public static HeldItemManager Instance;

    public Image heldItemImage; // UI Image 오브젝트 (플레이어 머리 위)
    public Transform player;    // 플레이어 Transform

    private Sprite currentHeldSprite;
    private string heldItemName;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void LateUpdate()
    {// 들고 있는 아이템이 없으면 이미지 끄기
        if (!IsHoldingItem())
        {
            if (heldItemImage.enabled)
            {
                heldItemImage.enabled = false;
            }
            return;
        }
        // 플레이어 위에 따라다니게 위치 업데이트
        if (heldItemImage.enabled)
        {
            Vector3 offset = new Vector3(0, 1.5f, 0); // 머리 위 위치
            Vector3 screenPos = Camera.main.WorldToScreenPoint(player.position + offset); // 선언과 초기화 동시에
            heldItemImage.transform.position = screenPos;

            //Debug.Log("HeldItemImage 위치: " + screenPos);
        }
    }
    public string GetHeldItemName()
    {
        return heldItemName;
    }

    public void ShowHeldItem(Sprite sprite, string itemName = null)
    {
        if (sprite == null)
        {
            Debug.LogWarning("ShowHeldItem: 스프라이트가 null입니다!");
            return;
        }
        if (heldItemImage == null)
        {
            Debug.LogError("heldItemImage가 연결되지 않았습니다! Inspector에서 Image를 드래그해 연결하세요.");
            return;
        }

        currentHeldSprite = sprite;
        heldItemName = itemName;

        heldItemImage.sprite = sprite;
        heldItemImage.enabled = true;

        Debug.Log("ShowHeldItem: 아이템 표시됨 - " + sprite.name);
    }

    public void HideHeldItem()
    {
        heldItemImage.enabled = false;
        currentHeldSprite = null;
        heldItemName = null;
    }

    public bool IsHoldingItem()
    {
        return currentHeldSprite != null;
    }

    public Sprite GetHeldItemSprite()
    {
        return currentHeldSprite;
    }

}
