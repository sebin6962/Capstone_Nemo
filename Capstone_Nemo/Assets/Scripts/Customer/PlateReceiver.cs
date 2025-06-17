using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlateReceiver : MonoBehaviour
{
    private bool hasPlacedThisPress = false; // ������ �ִ� ���� �ߺ� ������

    public GameObject resultUIPrefab;

    public Transform worldCanvas;   
    public Transform plateAnchor;

    private void OnTriggerStay2D(Collider2D other)
    {
        if (Input.GetKey(KeyCode.E) && !hasPlacedThisPress && HeldItemManager.Instance.IsHoldingItem())
        {
            if (plateAnchor != null && plateAnchor.childCount > 0)
            {
                Debug.Log("��Ź ���� �̹� �ٰ��� �ֽ��ϴ�");
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
                plateCheck.TryServeDagwa();  //�� 
            }

            HeldItemManager.Instance.HideHeldItem();
            hasPlacedThisPress = true;
            Debug.Log($"{ itemName}�� ��Ź�� ��ġ");
        }

        if (!Input.GetKey(KeyCode.E))
        {
            hasPlacedThisPress = false;
        }
    }
}
        /* if (other.CompareTag("HeldDagwa"))
         {
             // E Ű�� ������ �ְ� ���� ���� �� ������ ����
             if (Input.GetKey(KeyCode.E) && !hasPlacedThisPress )
             {
                 Debug.Log("�ٰ��� ���ÿ� �������ϴ�!");

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

                 hasPlacedThisPress = true; // �ٽ� ������ �ʵ��� ����
             }

             // E Ű���� �� ���� �� �ٽ� ���
             if (!Input.GetKey(KeyCode.E))
             {
                 hasPlacedThisPress = false;
             }
         }
     }*/
    
