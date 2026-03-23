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

    private Vector2 baseSize;
    private bool baseSizeInitialized = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;

        if (heldItemImage != null)
        {
            RectTransform rt = heldItemImage.GetComponent<RectTransform>();
            if (rt != null)
            {
                baseSize = rt.sizeDelta;     // 처음 에디터에서 세팅한 크기를 기준으로 저장
                baseSizeInitialized = true;
            }
        }
    }

    private void LateUpdate()
    {
        if (!IsHoldingItem())
        {
            if (heldItemImage.enabled)
                heldItemImage.enabled = false;
            return;
        }

        if (heldItemImage.enabled)
        {
            Vector3 offset = new Vector3(0, 1.5f, 0);
            // 월드 스페이스 캔버스니까 그냥 월드 좌표를 씀
            heldItemImage.rectTransform.position = player.position + offset;

            //village2 튜토리얼 진행 트리거 3
            var tm = FindObjectOfType<TutorialManager>();
            if (tm != null &&
                tm.IsCurrentStep(VillageSecondStep.PickUpSeed) &&  
                HeldItemManager.Instance.GetHeldItemName() == "Danhobak_seedBag")
            {
                tm.GoToNextVillageSecondStep(); 
            }

            var st = StoreTutorialManager.Instance;
            if (st != null &&
                st.IsCurrentStep(StoreTutorialStep.OpenStorage) && HeldItemManager.Instance.GetHeldItemName() == "Mepssalgaru")
            {
                st.GoToNextStep();
            }

            var ss = SecondStoreTutorialManager.Instance;
            if (ss != null &&
                ss.IsCurrentStep(SecondStoreTutorialStep.OpenStorage) && HeldItemManager.Instance.GetHeldItemName() == "Danhobakgaru")
            {
                ss.GoToNextStep();
            }

            /*            //village2 튜토리얼 진행 트리거 6
                        if (tm != null &&
                            tm.IsCurrentStep(VillageSecondStep.PickUp_WateringCan) &&
                            HeldItemManager.Instance.GetHeldItemName() == "village_object_wateringcan")
                        {
                            tm.GoToNextVillageSecondStep();
                        }*/

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
            Debug.LogWarning("ShowHeldItem: 스프라이트가 null입니다.");
            return;
        }
        if (heldItemImage == null)
        {
            Debug.LogError("heldItemImage가 연결되지 않았습니다. Inspector에서 Image를 드래그해 연결하세요.");
            return;
        }

        currentHeldSprite = sprite;
        heldItemName = itemName;

        heldItemImage.sprite = sprite;
        heldItemImage.enabled = true;

        // 스프라이트 비율 유지하도록 이미지 크기 조정
        RectTransform rt = heldItemImage.GetComponent<RectTransform>();
        if (rt != null)
        {
            if (!baseSizeInitialized)
            {
                baseSize = rt.sizeDelta;
                baseSizeInitialized = true;
            }

            rt.sizeDelta = baseSize;
        }

        heldItemImage.preserveAspect = true;

        Debug.Log("ShowHeldItem: 아이템 표시됨 - " + sprite.name);
    }

    public void HideHeldItem()
    {
        //혹시 모르니까
        string prevItemName = heldItemName;

        heldItemImage.enabled = false;
        currentHeldSprite = null;
        heldItemName = null;

        //village2 튜토리얼 진행 트리거 5
        var tm = TutorialManager.Instance;
        if (tm != null &&
            tm.IsCurrentStep(VillageSecondStep.RestoreSeed) && prevItemName == "Danhobak_seedBag")
        {
            tm.GoToNextVillageSecondStep();
        }
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
