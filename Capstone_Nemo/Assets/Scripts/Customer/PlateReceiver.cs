using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlateReceiver : MonoBehaviour
{
   private bool hasPlacedThisPress = false; // ������ �ִ� ���� �ߺ� ������

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("HeldDagwa"))
        {
            // E Ű�� ������ �ְ� ���� ���� �� ������ ����
            if (Input.GetKey(KeyCode.E) && !hasPlacedThisPress)
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
    }
}
