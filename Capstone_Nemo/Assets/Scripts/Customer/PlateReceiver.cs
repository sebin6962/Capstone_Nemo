using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlateReceiver : MonoBehaviour
{
    private bool hasPlacedThisPress = false; // 누르고 있는 동안 중복 방지용

    public GameObject resultUIPrefab;

    public Transform worldCanvas;   
    public Transform plateAnchor;

    private void OnTriggerStay2D(Collider2D other)
    {
        if (Input.GetKey(KeyCode.E) && !hasPlacedThisPress && HeldItemManager.Instance.IsHoldingItem())
        {
            if (plateAnchor != null && plateAnchor.childCount > 0)
            {
                Debug.Log("식탁 위에 이미 다과가 있습니다");
                return;
            }

            Sprite sprite = HeldItemManager.Instance.GetHeldItemSprite();
            string itemName = HeldItemManager.Instance.GetHeldItemName();

            if (!itemName.EndsWith("finish") || string.IsNullOrEmpty(itemName)) 
                return;

            GameObject uiItem = Instantiate(resultUIPrefab);
            uiItem.transform.localScale = Vector3.one * 0.02f;
            uiItem.transform.SetParent(worldCanvas.transform, false); 
            Vector3 basePos = plateAnchor != null ? plateAnchor.position : transform.position;
            uiItem.transform.position = basePos + new Vector3(0, 0.3f, 0.01f); 
            uiItem.transform.forward = Camera.main.transform.forward;


            ResultItemUI uiComp = uiItem.GetComponent<ResultItemUI>();
            if (uiComp != null)
            {
                uiComp.Initialize(sprite, itemName);
            }

            PlateCheck plateCheck = GetComponent<PlateCheck>();
            if (plateCheck != null)
            {
                plateCheck.TryServeDagwa();  //비교 
            }

            HeldItemManager.Instance.HideHeldItem();
            hasPlacedThisPress = true;
            Debug.Log($"{ itemName}을 식탁에 배치");
        }

        if (!Input.GetKey(KeyCode.E))
        {
            hasPlacedThisPress = false;
        }
    }
}
        /* if (other.CompareTag("HeldDagwa"))
         {
             // E 키를 누르고 있고 아직 실행 안 했으면 수행
             if (Input.GetKey(KeyCode.E) && !hasPlacedThisPress )
             {
                 Debug.Log("다과가 접시에 놓였습니다!");

                 HeldItemManager.Instance.HideHeldItem();

                 PlateCheck plate = GetComponent<PlateCheck>();
                 if (plate != null)
                 {
                     other.transform.position = transform.position + new Vector3(0, 0.3f, 0);
                     other.transform.SetParent(plate.transform);
                     other.tag = "Dagwa";

                     DagwaItem dagwa = other.GetComponent<DagwaItem>();
                     if (dagwa != null)
                     {
                         plate.SendDagwaToCustomer(dagwa.dagwaName);
                     }
                 }

                 hasPlacedThisPress = true; // 다시 누르지 않도록 설정
             }

             // E 키에서 손 뗐을 때 다시 허용
             if (!Input.GetKey(KeyCode.E))
             {
                 hasPlacedThisPress = false;
             }
         }
     }*/
    
